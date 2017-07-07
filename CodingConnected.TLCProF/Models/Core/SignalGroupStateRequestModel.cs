using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    public class SignalGroupStateRequestModel
    {
        #region Properties
        
        public SignalGroupStateRequestEnum RequestedState { get; private set; }
        public int Priority { get; private set; }
        public object RequestingObject { get; private set; }

        #endregion // Properties

        #region Constructor

        public SignalGroupStateRequestModel(SignalGroupStateRequestEnum requestedstate, int priority, object requestingobject)
        {
            RequestedState = requestedstate;
            Priority = priority;
            RequestingObject = requestingobject;
        }

        #endregion // Constructor
    }
}
