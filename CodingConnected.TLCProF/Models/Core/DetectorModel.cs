using System;
using System.Drawing;
using System.Runtime.Serialization;
using NLog;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "Detector", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class DetectorModel : ITLCProFModelBase
    {
        #region Fields

        [field: NonSerialized]
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #endregion // Fields

        #region Properties

        // State
        private bool _presence;
        [IgnoreDataMember]
        public bool Presence
        {
            get => _presence;
            set
            {
                if (value != _presence)
                {
                    _logger.Trace("Detector {0} presence changed: {1}", Name, value);
                    PresenceChanged?.Invoke(this, value);
                }
                if (value && !_presence)
                {
                    OccupiedTimer.Start();
                    GapTimer.Stop();
                }
                if (!value && _presence)
                {
                    OccupiedTimer.Stop();
                    GapTimer.Start();
                }
                _presence = value;
            }
        }
        [IgnoreDataMember]
        public bool Occupied { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel OccupiedTimer { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel GapTimer { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel ErrorHiTimer { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel ErrorLoTimer { get; private set; }

        // Settings
        [ModelName]
        [DataMember(IsRequired = true)]
        public string Name { get; set; }
        [DataMember(IsRequired = true)]
        public DetectorTypeEnum Type { get; set; }
        [DataMember(IsRequired = true)]
        public DetectorRequestTypeEnum Request { get; set; }
        [DataMember(IsRequired = true)]
        public DetectorExtendingTypeEnum Extend { get; set; }
        [DataMember]
        public Point Coordinates { get; set; }

        #endregion // Properties

        #region Events

        [field: NonSerialized]
        public event EventHandler<bool> PresenceChanged;

        #endregion // Events

        #region Private Methods

        private void OnCreated()
        {
            OccupiedTimer.Ended += (o, e) => { Occupied = true; };
            GapTimer.Ended += (o, e) => { Occupied = false; };
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        #endregion // Private Methods

        #region ITLCProFModelBase

        public void Reset()
        {
            Occupied = false;
            OccupiedTimer.Reset();
            GapTimer.Reset();
            ErrorHiTimer.Reset();
            ErrorLoTimer.Reset();
        }

        #endregion // ITLCProFModelBase

        #region Constructors

        public DetectorModel(string name, DetectorRequestTypeEnum request, DetectorExtendingTypeEnum extend, int occupied, int gap, int errorhi, int errorlo)
        {
            Name = name;
            Request = request;
            Extend = extend;
            OccupiedTimer = new TimerModel("occ" + name, occupied);
            GapTimer = new TimerModel("gap" + name, gap);
            ErrorHiTimer = new TimerModel("errhi" + name, errorhi, TimerTypeEnum.Minutes);
            ErrorLoTimer = new TimerModel("errlo" + name, errorlo, TimerTypeEnum.Minutes);

            OnCreated();
        }

        #endregion // Constructors
    }
}
