using System;
using System.Collections.Generic;
using System.Linq;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Simulation
{
    [UsedImplicitly]
    public class SimpleControllerSim
    {
        #region Fields

        private readonly Random _random;
        private readonly ControllerModel _model;
        private readonly List<SimpleDetectorSim> _detectorSims = new List<SimpleDetectorSim>();

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Public methods

        public void SimulationInit(DateTime starttime)
        {
            foreach (var d in _model.SignalGroups.SelectMany(x => x.Detectors))
            {
                _detectorSims.Add(new SimpleDetectorSim(d, starttime, _random));
            }
        }

        public void SimulationStep(double size)
        {
            foreach (var d in _detectorSims)
            {
                d.UpdateSimulationState(_model.Clock.CurrentTime);
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
