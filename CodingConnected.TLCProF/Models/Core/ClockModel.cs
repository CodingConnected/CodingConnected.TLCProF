using System;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "Clock", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ClockModel
    {
        #region Properties

        [IgnoreDataMember]
        public DateTime CurrentTime { get; set; }

        #endregion // Properties

        #region Public Methods

        public void Update(int timeAmount)
        {
            CurrentTime = CurrentTime.AddMilliseconds(timeAmount);
        }

        #endregion // Public Methods

        #region Constructor

        public ClockModel()
        {
        }

        #endregion // Constructor
    }
}
