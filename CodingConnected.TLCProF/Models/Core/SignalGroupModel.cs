using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NLog;
using System.Drawing;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "SignalGroup", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class SignalGroupModel
    {
        #region Fields

        [field: NonSerialized]
        private List<SignalGroupStateRequestModel> _stateRequests;
        [field: NonSerialized]
        private List<SignalGroupGreenRequestModel> _greenRequests;
        private InternalSignalGroupStateEnum _internalState;
        [field: NonSerialized]
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #endregion // Fields

        #region Properties

        [ModelName]
        [DataMember(IsRequired = true)]
        public string Name { get; private set; }

        [DataMember(IsRequired = true)]
        public TimerModel GreenGuaranteed { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel GreenFixed { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel GreenExtend { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel Amber { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel RedGuaranteed { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel RedFixed { get; private set; }
        [DataMember(IsRequired = true)]
        public TimerModel HeadMax { get; private set; }
        [DataMember(IsRequired = true)]
        public bool ExtendGreenFree { get; set; }

        [DataMember]
        public bool Permissive { get; set; }

        [DataMember]
        public Point Coordinates { get; set; } 

        [DataMember]
        public List<DetectorModel> Detectors { get; private set; }

        [DataMember]
        public List<InterGreenTimeModel> InterGreenTimes { get; private set; }
        
        [IgnoreDataMember]
        public ReadOnlyCollection<SignalGroupStateRequestModel> StateRequests { get; private set; }

        [IgnoreDataMember]
        public ReadOnlyCollection<SignalGroupGreenRequestModel> GreenRequests { get; private set; }
        
        [IgnoreDataMember]
        public InternalSignalGroupStateEnum InternalState
        {
            get => _internalState;
            private set
            {
                var oldstate = State;
                _internalState = value;
                _logger.Trace("Signal group {0} internal state changed: {1}", Name, value);
                InternalStateChanged?.Invoke(this, EventArgs.Empty);

                if (State == oldstate) return;
                _logger.Trace("Signal group {0} external state changed: {1}", Name, State);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [IgnoreDataMember]
        public SignalGroupStateEnum State
        {
            get
            {
                switch (InternalState)
                {
                    case InternalSignalGroupStateEnum.FixedGreen:
                    case InternalSignalGroupStateEnum.WaitGreen:
                    case InternalSignalGroupStateEnum.ExtendGreen:
                    case InternalSignalGroupStateEnum.FreeExtendGreen:
                        return SignalGroupStateEnum.Green;
                    case InternalSignalGroupStateEnum.Amber:
                        return SignalGroupStateEnum.Amber;
                    case InternalSignalGroupStateEnum.FixedRed:
                    case InternalSignalGroupStateEnum.Red:
                    case InternalSignalGroupStateEnum.NilRed:
                        return SignalGroupStateEnum.Red;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [IgnoreDataMember]
        public bool HasConflict
        {
            get
            {
                foreach(var igt in InterGreenTimes)
                {
                    if(igt.ConflictingSignalGroup.State == SignalGroupStateEnum.Green)
                    {
                        return true;
                    }
                    var igt2 = igt.ConflictingSignalGroup.InterGreenTimes.Where(x => x.SignalGroupTo == igt.SignalGroupFrom);
                    if(igt2.First().Timer.Running)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [IgnoreDataMember]
        public bool CyclicGreen => InternalState == InternalSignalGroupStateEnum.NilRed ||
                                   InternalState == InternalSignalGroupStateEnum.FixedGreen ||
                                   InternalState == InternalSignalGroupStateEnum.WaitGreen ||
                                   InternalState == InternalSignalGroupStateEnum.ExtendGreen;

        #endregion // Properties

        #region Events

        [field: NonSerialized]
        public event EventHandler InternalStateChanged;
        [field: NonSerialized]
        public event EventHandler StateChanged;

        #endregion // Events

        #region Public Methods

        public void AddGreenRequest(object requestingobject)
        {
            var present = GreenRequests.Any(x => x.RequestingObject != requestingobject);
            if (!present)
            {
                _greenRequests.Add(new SignalGroupGreenRequestModel(requestingobject));
            }
        }

        public void AddStateRequest(SignalGroupStateRequestEnum state, int priority, object requestingobject)
        {
            _stateRequests.Add(new SignalGroupStateRequestModel(state, priority, requestingobject));
        }

        public void ClearStateRequests()
        {
            _stateRequests.Clear();
        }

        public void HandleStateRequests()
        {
            if (GreenGuaranteed.Running ||
                Amber.Running ||
                RedGuaranteed.Running)
            {
                return;
            }

            var greenreq = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.Green).OrderBy(x => x.Priority).FirstOrDefault();
            var holdredreq = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.HoldRed).OrderBy(x => x.Priority).FirstOrDefault();
            var blockgreenreq = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.BlockGreen).OrderBy(x => x.Priority).FirstOrDefault();
            var holdgreenreq = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.HoldGreen).OrderBy(x => x.Priority).FirstOrDefault();
            var extendgreenreq = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.ExtendGreen).OrderBy(x => x.Priority).FirstOrDefault();
            var abortgreenreq = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.AbortGreen).OrderBy(x => x.Priority).FirstOrDefault();
            var waitgreenreqs = StateRequests.Where(x => x.RequestedState == SignalGroupStateRequestEnum.WaitGreen).OrderBy(x => x.Priority).FirstOrDefault();

            switch (InternalState)
            {
                case InternalSignalGroupStateEnum.FixedRed:
                    if(!RedFixed.Running)
                    {
                        InternalState = InternalSignalGroupStateEnum.Red;
                    }
                    break;

                case InternalSignalGroupStateEnum.Red:
                    if (!RedGuaranteed.Running &&
                        (greenreq != null && (holdredreq == null || holdredreq.Priority < greenreq.Priority)))
                    {
                        InternalState = InternalSignalGroupStateEnum.NilRed;
                    }
                    break;

                case InternalSignalGroupStateEnum.NilRed:
                    if (!HasConflict && greenreq != null && (blockgreenreq == null || blockgreenreq.Priority < greenreq.Priority))
                    {
                        InternalState = InternalSignalGroupStateEnum.FixedGreen;
                        GreenFixed.Start();
                        GreenGuaranteed.Start();
                        HeadMax.Start();
                    }
                    break;

                case InternalSignalGroupStateEnum.FixedGreen:
                    if(!GreenFixed.Running || abortgreenreq != null)
                    {
                        InternalState = InternalSignalGroupStateEnum.WaitGreen;
                    }
                    break;

                case InternalSignalGroupStateEnum.WaitGreen:
                    // Skip this for now
                    InternalState = InternalSignalGroupStateEnum.ExtendGreen;
                    GreenExtend.Start();
                    break;

                case InternalSignalGroupStateEnum.ExtendGreen:
                    if (extendgreenreq == null && holdgreenreq == null || 
                        (abortgreenreq != null && 
                         ((extendgreenreq == null || extendgreenreq.Priority < abortgreenreq.Priority) ||
                          (holdgreenreq == null || holdgreenreq.Priority < abortgreenreq.Priority))))
                    {
                        InternalState = InternalSignalGroupStateEnum.FreeExtendGreen;
                    }
                    break;

                case InternalSignalGroupStateEnum.FreeExtendGreen:
                    // Skip this for now
                    InternalState = InternalSignalGroupStateEnum.Amber;
                    Amber.Start();
                    foreach(var igt in InterGreenTimes)
                    {
                        igt.Timer.Start();
                    }
                    break;

                case InternalSignalGroupStateEnum.Amber:
                    if (!Amber.Running)
                    {
                        InternalState = InternalSignalGroupStateEnum.FixedRed;
                        RedGuaranteed.Start();
                        RedFixed.Start();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion // Public Methods
        
        #region Private Methods

        private void OnCreated()
        {
            if (Detectors == null)
            {
                Detectors = new List<DetectorModel>();
            }
            if (InterGreenTimes == null)
            {
                InterGreenTimes = new List<InterGreenTimeModel>();
            }
            _stateRequests = new List<SignalGroupStateRequestModel>();
            StateRequests = _stateRequests.AsReadOnly();
            _greenRequests = new List<SignalGroupGreenRequestModel>();
            GreenRequests = _greenRequests.AsReadOnly();
            InternalState = InternalSignalGroupStateEnum.Red;

            Amber.Started += (o, e) => { _greenRequests.Clear(); };

            RedGuaranteed.Start();
            RedFixed.Start();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        #endregion // Private Methods

        #region Constructors

        public SignalGroupModel(string name, int greengar, int greenfix, int greenex, int amber, int redgar, int redfix, int headmax)
        {
            Name = name;

            GreenGuaranteed = new TimerModel("greengar" + name, greengar);
            GreenFixed = new TimerModel("greenfix" + name, greenfix);
            GreenExtend = new TimerModel("greenex" + name, greenex);
            Amber = new TimerModel("amber" + name, amber);
            RedGuaranteed = new TimerModel("redgar" + name, redgar);
            RedFixed = new TimerModel("redfix" + name, redfix);
            HeadMax = new TimerModel("headmax" + name, headmax);

            OnCreated();
        }

        #endregion // Constructors
    }
}
