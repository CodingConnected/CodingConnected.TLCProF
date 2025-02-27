using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    public struct SignalGroupGreenRequestModel
    {
        #region Properties

        public object RequestingObject { get; }

        #endregion // Properties

        #region Constructor

        public SignalGroupGreenRequestModel(object requestingobject)
        {
            RequestingObject = requestingobject;
        }

        #endregion // Constructor
    }
}
