using System;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "Clock", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ClockModel : ITLCProFModelBase
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

        #region Private Methods

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            Reset();
        }

        #endregion // Private Methods

        #region ITLCProFModelBase

        public void Reset()
        {
            CurrentTime = new DateTime(2000, 1, 1, 12, 0, 0, 0);
        }

        #endregion

        #region Constructor

        public ClockModel()
        {
            Reset();
        }

        #endregion // Constructor
    }
}
