using System;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "SignalGroupSafetyGreen", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class SignalGroupSafetyGreenModel
    {
        #region Properties

        [DataMember]
        public string SignalGroupName { get; private set; }
        [DataMember]
        public string DetectorName { get; private set; }
        [DataMember]
        public TimerModel GapTimer { get; private set; }
        [DataMember]
        public TimerModel ExtendTimer { get; private set; }

        [IgnoreDataMember]
        public bool NeedsExtending { get; private set; }

        private SignalGroupModel _signalGroup;
        [IgnoreDataMember]
        public SignalGroupModel SignalGroup
        {
            get => _signalGroup;
            set => _signalGroup = value;
        }

        private DetectorModel _detector;
        [IgnoreDataMember]
        public DetectorModel Detector
        {
            get => _detector;
            set
            {
                _detector = value;
                if(value != null)
                {
                    value.PresenceChanged += (o, e) =>
                    {
                        if (!e) return;
                        if(GapTimer.Running)
                        {
                            NeedsExtending = true;
                            GapTimer.Start();
                            ExtendTimer.Start();
                        }
                        else
                        {
                            GapTimer.Start();
                        }
                    };
                }
            }
        }

        #endregion // Properties

        #region Constructor

        public SignalGroupSafetyGreenModel(string sgname, string dname, int gap, int extend)
        {
            SignalGroupName = sgname;
            DetectorName = dname;
            GapTimer = new TimerModel("safetygreen" + sgname + "d" + dname + "gap", gap);
            ExtendTimer = new TimerModel("safetygreen" + sgname + "d" + dname + "extend", extend);
            ExtendTimer.Ended += (o, e) => 
            {
                NeedsExtending = false;
            };
        }

        #endregion // Constructor
    }
}
