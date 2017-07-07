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
    [DataContract(Name = "InterGreenTime", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class InterGreenTimeModel
    {
        #region Properties

        [DataMember]
        public string SignalGroupFrom { get; set; }
        [DataMember]
        public string SignalGroupTo { get; set; }

        [DataMember]
        public TimerModel Timer { get; set; }

        [IgnoreDataMember]
        public SignalGroupModel ConflictingSignalGroup { get; set; }

        #endregion // Properties

        #region Constructor

        public InterGreenTimeModel(string from, string to, int max)
        {
            SignalGroupFrom = from;
            SignalGroupTo = to;
            Timer = new TimerModel("igt" + from + to, max);
        }

        public InterGreenTimeModel()
        {
            
        }

        #endregion // Constructor
    }
}
