using System;
using System.Collections.Generic;
using System.Linq;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Simulation
{
    public class SimpleControllerSim
    {
        #region Fields

        private readonly Random _random;
        private readonly ControllerModel _model;
        private readonly List<SimpleDetectorSim> _detectorSims = new List<SimpleDetectorSim>(1);

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Public methods

        public void SimulationInit(DateTime starttime)
        {
            foreach (var d in _model.SignalGroups.SelectMany(x => x.Detectors))
            {
                _detectorSims.Add(new SimpleDetectorSim(d){ NextChange = starttime.AddSeconds(_random.Next(1, 30))});
            }
        }

        public void SimulationStep(double size)
        {
            foreach (var d in _detectorSims)
            {
                if (_model.Clock.CurrentTime < d.NextChange) continue;
                d.Model.Presence = !d.Model.Presence;
                d.NextChange = _model.Clock.CurrentTime.AddSeconds(_random.Next(1, 30));
            }
        }

        #endregion // Public methods

        #region Constructor

        public SimpleControllerSim(ControllerModel model, int randomseed)
        {
            _model = model;
            _random = new Random(randomseed);
        }

        #endregion // Constructor
    }
}
