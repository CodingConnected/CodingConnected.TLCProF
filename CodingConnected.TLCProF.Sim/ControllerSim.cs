using System;
using System.Collections.Generic;
using System.Linq;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Sim
{
    public class ControllerSim
    {
        #region Fields

        private readonly Random _random;
        private readonly ControllerModel _model;
        private readonly List<DetectorSim> _detectorSims = new List<DetectorSim>(1);

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Public methods

        public void SimulationInit(DateTime starttime)
        {
            foreach (var d in _model.SignalGroups.SelectMany(x => x.Detectors))
            {
                _detectorSims.Add(new DetectorSim(d){ NextChange = starttime.AddSeconds(_random.Next(1, 180))});
            }
        }

        public void SimulationStep(int size)
        {
            foreach (var d in _detectorSims)
            {
                if (_model.Clock.CurrentTime < d.NextChange) continue;
                d.Model.Presence = !d.Model.Presence;
                d.NextChange = _model.Clock.CurrentTime.AddSeconds(_random.Next(1, 180));
            }
        }

        #endregion // Public methods

        #region Constructor

        public ControllerSim(ControllerModel model, int randomseed)
        {
            _model = model;
            _random = new Random(randomseed);
        }

        #endregion // Constructor
    }
}
