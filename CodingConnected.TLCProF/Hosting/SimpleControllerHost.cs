using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly int _stepsize;
        private readonly ControllerManager _manager;

        private bool _stepdelay;
        private DateTime _previousTime;
        private bool _running;
        private Thread _runThread;

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
            _runThread = new Thread(RunController);
            _runThread.Start();
        }

        #endregion // Public methods

        #region Private methods

        [UsedImplicitly]
        private void RunController()
        {
            _logger.Info("RunController started.");
            try
            {
                _previousTime = DateTime.Now;
                double elapsed, del;
                while (true)
                {
                    if (_stepdelay)
                    {
                        elapsed = DateTime.Now.Subtract(_previousTime).TotalMilliseconds;
                        _previousTime = DateTime.Now;
                        if (elapsed > _stepsize * 2)
                        {
                            _logger.Warn("Control loop cycle took longer than twice the desired step size: {0} ms", elapsed);
                        }

                        Simulator?.SimulationStep(elapsed);
                        _manager.ExecuteStep(elapsed);
                        StepTaken?.Invoke(this, EventArgs.Empty);

                        del = _stepsize - elapsed;
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

        [UsedImplicitly]
        public void StopController()
        {
            if (_runThread == null)
            {
                _logger.Error("StopController was called without a prior call to StartController.");
                return;
            }

            _runThread.Interrupt();

            // Bring controller to allred state
            Task.Run(() =>
            {
                while (
                    _manager.Controller.ControllerState == ControllerStateEnum.AllRed &&
                    _manager.Controller.SignalGroups.Any(x => x.State != SignalGroupStateEnum.Red))
                    {
                        _manager.ExecuteStep(_stepsize);
                    }
            });

            _running = false;
        }

        #endregion // Private methods

        #region Constructor

        public SimpleControllerHost(ControllerManager manager, SimpleControllerSim simulator, int stepsize, bool stepdelay = true)
        {
            _manager = manager;
            Simulator = simulator;
            _stepsize = stepsize;
            _stepdelay = stepdelay;
        }

        #endregion // Constructor
    }
}