using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CodingConnected.TLCProF.Generic;
using CodingConnected.TLCProF.Logging;
using CodingConnected.TLCProF.Management;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Simulation;
using JetBrains.Annotations;
using NLog;

namespace CodingConnected.TLCProF.Hosting
{
    public class SimpleControllerHost
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly bool IsPosixEnvironment = Path.DirectorySeparatorChar == '/';

        private int _stepSize;
        private int _stepDelaySize;
        private readonly int _stepsizemargin;
        private readonly ControllerManager _manager;

        private bool _stepDelay;
        private bool _halted;
        private bool _haltStepDelay;
        private bool _running;
        private readonly bool _realTime;

        private WinHiPrecTimer _winCycleTimer;
        private PosixHiPrecTimer _linCycleTimer;
        private Stopwatch watch;
        private CancellationTokenSource _fastTokenSource;

        private MaxWaitingTimeLogger _controllerLogger;

#if DEBUG
        private readonly Stopwatch _performanceWatch = new Stopwatch();
        private readonly long[] _performanceMeasurements = new long[100];
        private int _performanceMeasurementsIndex;
        private long _meanPerformanceMeasurement;
#endif

        #endregion // Fields

        #region Properties

        [UsedImplicitly]
        public SimpleControllerSim Simulator { private get; set; }

        public int StepSize
        {
            get => _stepSize;
            set => _stepSize = value;
        }

        public bool StepDelay
        {
            [UsedImplicitly] get => _stepDelay;
            set
            {
                _stepDelay = value;
                if (!_halted)
                {
                    if (!value)
                    {
                        _fastTokenSource = new CancellationTokenSource();
                        if (IsPosixEnvironment)
                        {
                            if (_linCycleTimer != null) _linCycleTimer.Enabled = false;
                        }
                        else
                        {
                            if (_winCycleTimer != null && _winCycleTimer.IsRunning) _winCycleTimer.Stop();
                        }
                        Task.Run(() =>
                        {
                            while (!_fastTokenSource.IsCancellationRequested)
                            {
                                Simulator?.SimulationStep(_stepSize);
                                _manager.ExecuteStep(_stepSize);
                                StepTaken?.Invoke(this, EventArgs.Empty);
                            }
                        });
                    }
                    else if (_fastTokenSource != null)
                    {
                        _fastTokenSource.Cancel();
                        if (IsPosixEnvironment)
                        {
                            if (_linCycleTimer != null) _linCycleTimer.Enabled = true;
                        }
                        else
                        {
                            if (_winCycleTimer != null && !_winCycleTimer.IsRunning) _winCycleTimer.Start();
                        }
                    }
                }
            }
        }

        public int StepDelaySize
        {
            get => _stepDelaySize;
            set
            {
                _stepDelaySize = value;
                if (!_halted)
                {
                    if (IsPosixEnvironment)
                    {
                        _linCycleTimer.Interval = _stepDelaySize;
                    }
                    else
                    {
                        if (_winCycleTimer != null && _winCycleTimer.IsRunning) _winCycleTimer.Stop();
                        _winCycleTimer = new WinHiPrecTimer();
                        _winCycleTimer.Elapsed += WinCycleTimerElapsed;
                        _winCycleTimer.Interval = _stepDelaySize;
                        _winCycleTimer.Resolution = 5;
                        _winCycleTimer.Start();
                    }
                }
            }
        }

        public bool Running => _running;

        #endregion // Properties

        #region Events

        public event EventHandler StepTaken;

        #endregion // Events

        #region Public methods

        [UsedImplicitly]
        public void TakeSingleStep()
        {
            Simulator?.SimulationStep(_stepSize);
            _manager.ExecuteStep(_stepSize);
        }

        [UsedImplicitly]
        public void StartController()
        {
            _running = true;
            watch = new Stopwatch();
            if (IsPosixEnvironment)
            {
                _linCycleTimer = new PosixHiPrecTimer();
                _linCycleTimer.Tick += LinCycleTimerElapsed;
                _linCycleTimer.Interval = _stepDelaySize;
                _linCycleTimer.Enabled = true;
            }
            else
            {
                _winCycleTimer = new WinHiPrecTimer();
                _winCycleTimer.Elapsed += WinCycleTimerElapsed;
                _winCycleTimer.Interval = _stepDelaySize;
                _winCycleTimer.Resolution = 5;
                _winCycleTimer.Start();
            }

        }

        [UsedImplicitly]
        public void HaltController(bool halt)
        {
            _halted = halt;
            _running = !_halted;
            if (IsPosixEnvironment)
            {
                if (_linCycleTimer != null && halt)
                {
                    _fastTokenSource?.Cancel();
                    if(_linCycleTimer.Enabled) _linCycleTimer.Enabled = false;
                }
                else if (_linCycleTimer != null)
                {
                    _linCycleTimer.Enabled = true;
                    StepDelay = _stepDelay;
                }
            }
            else
            {
                if (_winCycleTimer != null && halt)
                {
                    _fastTokenSource?.Cancel();
                    if(_winCycleTimer.IsRunning) _winCycleTimer.Stop();
                }
                else if (_winCycleTimer != null && StepDelay)
                {
                    if (!_winCycleTimer.IsRunning) _winCycleTimer.Start();
                    StepDelay = _stepDelay;
                }
            }
        }

        private void WinCycleTimerElapsed(object sender, EventArgs eventArgs)
        {
            long elapsed;
            if (!_realTime)
            {
                elapsed = _stepSize;
            }
            else
            {
                elapsed = watch.ElapsedMilliseconds;
                if (elapsed > _stepSize * 2)
                {
                    _logger.Warn("Control loop cycle took longer than twice the desired step size: {0} ms", elapsed);
                }
            }
            watch.Reset();
            watch.Start();

            Simulator?.SimulationStep(elapsed);
#if DEBUG
            _performanceWatch.Start();
#endif
            _manager.ExecuteStep(elapsed);
#if DEBUG
            _performanceMeasurements[_performanceMeasurementsIndex] = _performanceWatch.ElapsedTicks;
            if (_performanceMeasurementsIndex < 99)
            {
                _performanceMeasurementsIndex++;
            }
            else
            {
                _meanPerformanceMeasurement = 0;
                for (var i = 0; i < _performanceMeasurementsIndex; i++)
                {
                    _meanPerformanceMeasurement += _performanceMeasurements[i];
                }
                _performanceMeasurementsIndex = 0;
                _meanPerformanceMeasurement /= 100;
                _logger.Debug("Ticks per step over the last 100 iterations: {0}", _meanPerformanceMeasurement);
            }
            _performanceWatch.Reset();
#endif
            StepTaken?.Invoke(this, EventArgs.Empty);

        }

        private void LinCycleTimerElapsed(object sender, EventArgs eventArgs)
        {
            var elapsed = watch.ElapsedMilliseconds;
            watch.Reset();
            watch.Start();

            if (elapsed > _stepsizemargin)
            {
                _logger.Warn("Control loop cycle took than 1.5 x the desired step size: {0} ms", elapsed);
            }

            Simulator?.SimulationStep(elapsed);
            _manager.ExecuteStep(elapsed);
            StepTaken?.Invoke(this, EventArgs.Empty);
        }

        [UsedImplicitly]
        public void StopController()
        {
            _fastTokenSource?.Cancel();
            if (IsPosixEnvironment)
            {
                if (_linCycleTimer == null)
                {
                    _logger.Error("StopController was called without a prior call to StartController.");
                    return;
                }
                if(_stepDelay && _linCycleTimer.Enabled) _linCycleTimer.Enabled = false;
                _linCycleTimer = null;
            }
            else
            {
                if (_winCycleTimer == null)
                {
                    _logger.Error("StopController was called without a prior call to StartController.");
                    return;
                }
                if (_stepDelay && _winCycleTimer.IsRunning) _winCycleTimer.Stop();
                _winCycleTimer.Dispose();
                _winCycleTimer = null;
            }
            
            _running = false;
        }

        #endregion // Public methods

        #region Private methods
        
        #endregion // Private methods

        #region Constructor

        public SimpleControllerHost(ControllerManager manager, SimpleControllerSim simulator, int stepSize, int stepDelaySize, bool stepDelay = true, bool realTime = true)
        {
            _manager = manager;

            _controllerLogger = new MaxWaitingTimeLogger(manager.Controller);
            manager.Controller.MaximumWaitingTimeExceeded += (o, e) =>
            {
                if (IsPosixEnvironment)
                {
                    _linCycleTimer.Enabled = false;
                }
                else
                {
                    _winCycleTimer.Stop();
                }
                _controllerLogger.LogMaxWaitingTimeOccured();
                manager.Controller.Reset();
                if (IsPosixEnvironment)
                {
                    _linCycleTimer.Enabled = true;
                }
                else
                {
                    _winCycleTimer.Start();
                }
#warning stop running if # times...
            };
            Simulator = simulator;
            _stepSize = stepSize;
            _stepsizemargin = (int) (stepSize * 1.5);
            _stepDelay = stepDelay;
            _stepDelaySize = stepDelaySize;
            _realTime = realTime;
        }

        #endregion // Constructor
    }
}