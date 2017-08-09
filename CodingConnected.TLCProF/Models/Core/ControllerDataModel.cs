using System;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "ControllerData", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ControllerDataModel
    {
        #region Fields

        private int _maximumWaitingTime;
        internal ControllerModel Controller;

        #endregion // Fields

        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int MaximumWaitingTime
        {
            get => _maximumWaitingTime;
            set
            {
                _maximumWaitingTime = value;
                Controller?.SignalGroups.ForEach(x => x.CurrentWaitingTime.SetMaximum(value, TimerTypeEnum.Seconds));
            }
        }

        #endregion // Properties
    }
}