using System;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Simulation
{
    public class SimpleDetectorSim
    {
        #region Fields


        #endregion // Fields

        #region Properties

        public DateTime NextChange { get; set; }
        public DetectorModel Model { get; }

        #endregion // Properties

        #region Constructor

        public SimpleDetectorSim(DetectorModel model)
        {
            Model = model;
        }

        #endregion // Constructor
    }
}