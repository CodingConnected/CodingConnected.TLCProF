using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CodingConnected.TLCProF.Generic;
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
        private bool _running;

        private WinHiPrecTimer _winCycleTimer;
        private PosixHiPrecTimer _linCycleTimer;
        private Stopwatch watch;
        private CancellationTokenSource _fastTokenSource;

        #endregion // Fields

        #region Properties

        [UsedImplicitly]
        public SimpleControllerSim Simulator { private get; set; }

        public bool StepDelay
        {
            [UsedImplicitly] get => _stepDelay;
            set
            { 
                _stepDelay = value;
                if (!value)
                {
                    _fastTokenSource = new CancellationTokenSource();
                    if (IsPosixEnvironment)
                    {
                        _linCycleTimer.Enabled = false;
                    }
                    else
                    {
                        _winCycleTimer.Stop();
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
                else
                {
                    _fastTokenSource.Cancel();
                    if (IsPosixEnvironment)
                    {
                        _linCycleTimer.Enabled = true;
                    }
                    else
                    {
                        _winCycleTimer.Start();
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
                if (IsPosixEnvironment)
                {
                    _linCycleTimer.Interval = _stepDelaySize;
                }
                else
                {
                    _winCycleTimer.Interval = _stepDelaySize;
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
                _winCycleTimer.Resolution = 25;
                _winCycleTimer.Start();
            }

        }

        private void WinCycleTimerElapsed(object sender, EventArgs eventArgs)
        {
            var elapsed = watch.ElapsedMilliseconds;
            watch.Reset();
            watch.Start();

            if (elapsed > _stepSize * 2)
            {
                _logger.Warn("Control loop cycle took longer than twice the desired step size: {0} ms", elapsed);
            }

            Simulator?.SimulationStep(elapsed);
            _manager.ExecuteStep(elapsed);
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
                if(_stepDelay) _linCycleTimer.Enabled = false;
                _linCycleTimer = null;
            }
            else
            {
                if (_winCycleTimer == null)
                {
                    _logger.Error("StopController was called without a prior call to StartController.");
                    return;
                }
                if (_stepDelay) _winCycleTimer.Stop();
                _winCycleTimer.Dispose();
                _winCycleTimer = null;
            }

            //_runThread.Interrupt();
            _running = false;
        }

        #endregion // Public methods

        #region Private methods
        
        #endregion // Private methods

        #region Constructor

        public SimpleControllerHost(ControllerManager manager, SimpleControllerSim simulator, int stepSize, int stepDelaySize, bool stepDelay = true)
        {
            _manager = manager;
            Simulator = simulator;
            _stepSize = stepSize;
            _stepsizemargin = (int) (stepSize * 1.5);
            _stepDelay = stepDelay;
            _stepDelaySize = stepDelaySize;
        }

        #endregion // Constructor
    }
}