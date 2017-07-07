using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "SignalGroupModuleData", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class SignalGroupModuleDataModel
    {
        #region Properties

        [DataMember]
        public string SignalGroupName { get; private set; }

        [IgnoreDataMember]
        public bool HadPrimaryRealisation { get; set; }
        [IgnoreDataMember]
        public bool SkippedPrimaryRealisation { get; set; }
        
        [IgnoreDataMember]
        public SignalGroupModel SignalGroup { get; set; }

        #endregion // Properties

        #region Constructor

        public SignalGroupModuleDataModel(string sgname)
        {
            SignalGroupName = sgname;
        }

        #endregion // Constructor
    }
}
