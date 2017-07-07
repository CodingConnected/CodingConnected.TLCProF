using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "ExtraData", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ExtraDataModel
    {
        #region Properties

        [DataMember]
        public List<SignalGroupSafetyGreenModel> SafetyGreenSignalGroups { get; private set; }

        #endregion // Properties

        #region Constructor

        public ExtraDataModel()
        {
            SafetyGreenSignalGroups = new List<SignalGroupSafetyGreenModel>();
        }

        #endregion // Constructor
    }
}
