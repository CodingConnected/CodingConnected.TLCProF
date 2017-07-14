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

        private readonly int _stepsize;
        private readonly int _stepsizemargin;
        private readonly ControllerManager _manager;

        private bool _stepdelay;
        private DateTime _previousTime;
        private bool _running;
        private Thread _runThread;

        private WinHiPrecTimer _winCycleTimer;
        private PosixHiPrecTimer _linCycleTimer;
        private Stopwatch watch;

        #endregion // Fields

        #region Properties

        [UsedImplicitly]
        public SimpleControllerSim Simulator { private get; set; }

        public bool Stepdelay
        {
            [UsedImplicitly] get => _stepdelay;
            set
            { 
                _stepdelay = value;
                _previousTime = DateTime.Now;
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
            //_runThread = new Thread(RunController);
            //_runThread.Start();
            watch = new Stopwatch();
            if (IsPosixEnvironment)
            {
                _linCycleTimer = new PosixHiPrecTimer();
                _linCycleTimer.Tick += LinCycleTimerElapsed;
                _linCycleTimer.Interval = _stepsize;
                _linCycleTimer.Enabled = true;
            }
            else
            {
                _winCycleTimer = new WinHiPrecTimer();
                _winCycleTimer.Elapsed += WinCycleTimerElapsed;
                _winCycleTimer.Interval = _stepsize;
                _winCycleTimer.Resolution = 25;
                _winCycleTimer.Start();
            }

        }

        private void WinCycleTimerElapsed(object sender, EventArgs eventArgs)
        {
            var elapsed = watch.ElapsedMilliseconds;
            watch.Reset();
            watch.Start();

            if (elapsed > _stepsize * 2)
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
            //if (_runThread == null)
            //{
            //    _logger.Error("StopController was called without a prior call to StartController.");
            //    return;
            //}
            if (IsPosixEnvironment)
            {
                if (_linCycleTimer == null)
                {
                    _logger.Error("StopController was called without a prior call to StartController.");
                    return;
                }
                _linCycleTimer.Enabled = false;
                _linCycleTimer = null;
            }
            else
            {
                if (_winCycleTimer == null)
                {
                    _logger.Error("StopController was called without a prior call to StartController.");
                    return;
                }  
                _winCycleTimer.Stop();
                _winCycleTimer.Dispose();
                _winCycleTimer = null;
            }

            //_runThread.Interrupt();
            _running = false;
        }

        #endregion // Public methods

        #region Private methods

        [UsedImplicitly]
        private void RunController()
        {
            _logger.Info("RunController started.");
            try
            {
                //_previousTime = DateTime.Now;
                //double elapsed, del;
                //while (true)
                //{
                //    if (_stepdelay)
                //    {
                //        elapsed = DateTime.Now.Subtract(_previousTime).TotalMilliseconds;
                //        _previousTime = DateTime.Now;
                //        if (elapsed > _stepsize * 2)
                //        {
                //            _logger.Warn("Control loop cycle took longer than twice the desired step size: {0} ms", elapsed);
                //        }
                //
                //        Simulator?.SimulationStep(elapsed);
                //        _manager.ExecuteStep(elapsed);
                //        StepTaken?.Invoke(this, EventArgs.Empty);
                //
                //        del = _stepsize - elapsed;
                //        if (del > 0)
                //        {
                //            Thread.Sleep((int)del);
                //        }
                //    }
                //    else
                //    {
                //        Simulator?.SimulationStep(_stepsize);
                //        _manager.ExecuteStep(_stepsize);
                //        StepTaken?.Invoke(this, EventArgs.Empty);
                //    }
                //}
                long elapsed = 0;
                var watch = new Stopwatch();
                while (true)
                {
                    if (_stepdelay)
                    {
                        elapsed = watch.ElapsedMilliseconds;
                        watch.Reset();
                        watch.Start();

                        if (elapsed > _stepsize * 2)
                        {
                            _logger.Warn("Control loop cycle took longer than twice the desired step size: {0} ms", elapsed);
                        }

                        Simulator?.SimulationStep(elapsed);
                        _manager.ExecuteStep(elapsed);
                        StepTaken?.Invoke(this, EventArgs.Empty);

                        var del = _stepsize - watch.ElapsedMilliseconds;
                        if (del > 0)
                        {
                            Thread.Sleep((int) del);
                        }
                    }
                    else
                    {
                        Simulator?.SimulationStep(_stepsize);
                        _manager.ExecuteStep(_stepsize);
                        StepTaken?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                _logger.Info("RunController aborted.");
            }
            catch (ThreadInterruptedException)
            {
                _logger.Info("RunController interrupted.");
            }
        }

        #endregion // Private methods

        #region Constructor

        public SimpleControllerHost(ControllerManager manager, SimpleControllerSim simulator, int stepsize, bool stepdelay = true)
        {
            _manager = manager;
            Simulator = simulator;
            _stepsize = stepsize;
            _stepsizemargin = (int) (stepsize * 1.5);
            _stepdelay = stepdelay;
        }

        #endregion // Constructor
    }
}