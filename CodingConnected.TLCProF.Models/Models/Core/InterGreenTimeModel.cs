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
    public class InterGreenTimeModel : ITLCProFModelBase
    {
        #region Properties

        // Settings
        [DataMember]
        public string SignalGroupFrom { get; set; }
        [DataMember]
        public string SignalGroupTo { get; set; }

        // State
        [DataMember]
        public TimerModel Timer { get; set; }

        // References
        [IgnoreDataMember]
        public SignalGroupModel ConflictingSignalGroup { get; set; }

        #endregion // Properties

        #region ITLCProFModelBase

        public void Reset()
        {
            Timer.Reset();
        }

        #endregion // ITLCProFModelBase

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
