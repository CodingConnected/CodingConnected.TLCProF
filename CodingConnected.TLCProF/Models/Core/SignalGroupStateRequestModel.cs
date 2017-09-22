using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    internal struct SignalGroupStateRequestModel
    {
        #region Properties

        public readonly SignalGroupStateRequestEnum RequestedState;
        public readonly int Priority;
	    public readonly string Reason;
        public object RequestingObject;
        public bool HasValue;

        #endregion // Properties

        #region Overrides

        public override string ToString()
        {
            return RequestingObject.GetType().Name + (Reason != null ? $"({Reason})" : "") + $": {RequestedState} [{Priority}]";
        }

        #endregion // Overrides

        #region Constructor

        public SignalGroupStateRequestModel(SignalGroupStateRequestEnum requestedstate, int priority, object requestingobject, string reason = null)
        {
            RequestedState = requestedstate;
            Priority = priority;
            RequestingObject = requestingobject;
            HasValue = false;
	        Reason = reason;
        }

        #endregion // Constructor
    }
}
