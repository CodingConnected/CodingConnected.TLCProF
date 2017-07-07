using System;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Sim
{
    public class DetectorSim
    {
        #region Fields


        #endregion // Fields

        #region Properties

        public DateTime NextChange { get; set; }
        public DetectorModel Model { get; }

        #endregion // Properties

        #region Constructor

        public DetectorSim(DetectorModel model)
        {
            Model = model;
        }

        #endregion // Constructor
    }
}