using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NLog;
using System.Drawing;
using CodingConnected.TLCProF.Models.Attributes;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "SignalGroup", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class SignalGroupModel : ITLCProFModelBase
    {
        #region Fields

        [field: NonSerialized] private int _stateReqIndex;
        [field: NonSerialized]
        private SignalGroupStateRequestModel [] _stateRequests;
        [field: NonSerialized] private int _greenReqIndex;
        [field: NonSerialized]
        private string [] _greenRequests;
        private InternalSignalGroupStateEnum _internalState;
        [field: NonSerialized]
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #endregion // Fields

        #region Properties

        // State
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel GreenGuaranteed { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel GreenFixed { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel GreenExtend { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel Amber { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel RedGuaranteed { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel RedFixed { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel HeadMax { get; private set; }
        [LogWithDump]
        [DataMember(IsRequired = true)]
        public TimerModel FixedRequestDelay { get; private set; }
        
        [LogWithDump]
        [IgnoreDataMember]
        public InternalSignalGroupStateEnum InternalState
        {
            get => _internalState;
            private set
            {
                var oldstate = State;
                _internalState = value;
                _logger.Trace("Signal group {0} internal state changed: {1}", Name, value);
                InternalStateChanged?.Invoke(this, value);

                if (State == oldstate) return;
                _logger.Trace("Signal group {0} external state changed: {1}", Name, State);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        [LogWithDump]
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
        [LogWithDump]
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
                    if (igt2.First().Timer.Running)
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

        [LogWithDump]
        [IgnoreDataMember]
        public bool HasGreenRequest => _greenReqIndex > 0;

        [IgnoreDataMember]
        public bool HasValidGreenStateRequest
        {
            get
            {
                if (State == SignalGroupStateEnum.Red && _stateReqIndex > 0)
                {
                    var greenReq = new SignalGroupStateRequestModel();
                    var holdRedReq = new SignalGroupStateRequestModel();
                    var blockGreenReq = new SignalGroupStateRequestModel();

                    for (var i = 0; i < _stateReqIndex; ++i)
                    {
                        switch (_stateRequests[i].RequestedState)
                        {
                            case SignalGroupStateRequestEnum.HoldRed:
                                if (!holdRedReq.HasValue || holdRedReq.Priority < _stateRequests[i].Priority)
                                    holdRedReq = _stateRequests[i];
                                break;
                            case SignalGroupStateRequestEnum.Green:
                                if (!greenReq.HasValue || greenReq.Priority < _stateRequests[i].Priority)
                                    greenReq = _stateRequests[i];
                                break;
                            case SignalGroupStateRequestEnum.BlockGreen:
                                if (!blockGreenReq.HasValue || blockGreenReq.Priority < _stateRequests[i].Priority)
                                    blockGreenReq = _stateRequests[i];
                                break;
                        }
                    }
                    if (greenReq.HasValue && 
                        (!holdRedReq.HasValue || holdRedReq.Priority < greenReq.Priority) &&
                        (!blockGreenReq.HasValue || blockGreenReq.Priority < greenReq.Priority))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [IgnoreDataMember]
        public bool IsInWaitingGreen
        {
            get
            {
                for (var i = 0; i < _stateReqIndex; ++i)
                {
                    if (_stateRequests[i].RequestedState == SignalGroupStateRequestEnum.WaitGreen)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [LogWithDump]
        [IgnoreDataMember]
        [Timer]
        public TimerModel CurrentWaitingTime { get; private set; }

        [IgnoreDataMember]
        public List<string> CurrentGreenRequests
        {
            get
            {
                var reqs = new List<string>();
                for (int i = 0; i < _greenReqIndex; i++)
                {
                    reqs.Add(_greenRequests[i]);
                }
                return reqs;
            }
        }

        [IgnoreDataMember]
        internal List<SignalGroupStateRequestModel> CurrentStateRequests
        {
            get
            {
                var reqs = new List<SignalGroupStateRequestModel>();
                for (int i = 0; i < _greenReqIndex; i++)
                {
                    reqs.Add(_stateRequests[i]);
                }
                return reqs;
            }
        }

        // Settings
        [LogWithDump]
        [ModelName]
        [DataMember(IsRequired = true)]
        public string Name { get; private set; }
        [DataMember]
        public bool Permissive { get; set; }
        [DataMember]
        public Point Coordinates { get; set; } 
        [DataMember]
        public List<DetectorModel> Detectors { get; private set; }
        [DataMember]
        public List<InterGreenTimeModel> InterGreenTimes { get; private set; }
        [DataMember]
        public bool ExtendGreenFree { get; set; }
        [DataMember]
        public bool WaitGreen { get; set; }
        [DataMember]
        public FixedRequestTypeEnum FixedRequest { get; set; }

        #endregion // Properties

        #region Events

        [field: NonSerialized]
        public event EventHandler<InternalSignalGroupStateEnum> InternalStateChanged;
        [field: NonSerialized]
        public event EventHandler StateChanged;

        #endregion // Events

        #region Public Methods

        public void AddGreenRequest(string reason)
        {
            for (var i = 0; i < _greenReqIndex; ++i)
            {
                if (!string.Equals(_greenRequests[i], reason)) continue;
                return;
            }
            if (!CurrentWaitingTime.Running && State != SignalGroupStateEnum.Green)
            {
                CurrentWaitingTime.Start();
            }
            if (_greenReqIndex < 10) _greenRequests[_greenReqIndex] = reason;
            _greenReqIndex++;
        }

        public void AddStateRequest(SignalGroupStateRequestEnum state, int priority, object requestingobject, string reason = null)
        {
            if (_stateReqIndex < 50)
            {
                _stateRequests[_stateReqIndex] =
                    new SignalGroupStateRequestModel(state, priority, requestingobject, reason)
                    {
                        HasValue = true
                    };
            }
            _stateReqIndex++;
        }

        public void ClearStateRequests()
        {
            _stateReqIndex = 0;
        }

        public void HandleStateRequests()
        {
            if (GreenGuaranteed.Running ||
                Amber.Running ||
                RedGuaranteed.Running)
            {
                return;
            }

            var greenReq = new SignalGroupStateRequestModel();
            var holdRedReq = new SignalGroupStateRequestModel();
            var blockGreenReq = new SignalGroupStateRequestModel();
            var holdGreenReq = new SignalGroupStateRequestModel();
            var waitGreenReq = new SignalGroupStateRequestModel();
            var extendGreenReq = new SignalGroupStateRequestModel();
            var freeExtendGreenReq = new SignalGroupStateRequestModel();
            var abortGreenReq = new SignalGroupStateRequestModel();

            if (_stateReqIndex > 0)
            {
                for (var i = 0; i < _stateReqIndex; ++i)
                {
                    switch (_stateRequests[i].RequestedState)
                    {
                        case SignalGroupStateRequestEnum.HoldRed:
                            if (!holdRedReq.HasValue || holdRedReq.Priority < _stateRequests[i].Priority)
                                holdRedReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.Green:
                            if (!greenReq.HasValue || greenReq.Priority < _stateRequests[i].Priority)
                                greenReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.BlockGreen:
                            if (!blockGreenReq.HasValue || blockGreenReq.Priority < _stateRequests[i].Priority)
                                blockGreenReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.WaitGreen:
                            if (!waitGreenReq.HasValue || waitGreenReq.Priority < _stateRequests[i].Priority)
                                waitGreenReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.ExtendGreen:
                            if (!extendGreenReq.HasValue || extendGreenReq.Priority < _stateRequests[i].Priority)
                                extendGreenReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.FreeExtendGreen:
                            if (!freeExtendGreenReq.HasValue || freeExtendGreenReq.Priority < _stateRequests[i].Priority)
                                freeExtendGreenReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.AbortGreen:
                            if (!abortGreenReq.HasValue || abortGreenReq.Priority < _stateRequests[i].Priority)
                                abortGreenReq = _stateRequests[i];
                            break;
                        case SignalGroupStateRequestEnum.HoldGreen:
                            if (holdRedReq.HasValue || holdRedReq.Priority < _stateRequests[i].Priority)
                                holdGreenReq = _stateRequests[i];
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            switch (InternalState)
            {
                case InternalSignalGroupStateEnum.FixedRed:
                    if(!RedFixed.Running)
                    {
                        InternalState = InternalSignalGroupStateEnum.Red;
                    }
                    break;

                case InternalSignalGroupStateEnum.Red:
                    if (!RedGuaranteed.Running && greenReq.HasValue && 
                        (!holdRedReq.HasValue || holdRedReq.Priority < greenReq.Priority))
                    {
                        InternalState = InternalSignalGroupStateEnum.NilRed;
                    }
                    break;

                case InternalSignalGroupStateEnum.NilRed:
                    if (!HasConflict && greenReq.HasValue && 
                        (!blockGreenReq.HasValue || blockGreenReq.Priority < greenReq.Priority))
                    {
                        InternalState = InternalSignalGroupStateEnum.FixedGreen;
                        GreenFixed.Start();
                        GreenGuaranteed.Start();
                        HeadMax.Start();
                    }
                    break;

                case InternalSignalGroupStateEnum.FixedGreen:
                    if(!GreenFixed.Running || !GreenGuaranteed.Running && abortGreenReq.HasValue)
                    {
                        InternalState = InternalSignalGroupStateEnum.WaitGreen;
                    }
                    break;

                case InternalSignalGroupStateEnum.WaitGreen:
                    
                    if (!waitGreenReq.HasValue && !holdGreenReq.HasValue ||
                        abortGreenReq.HasValue && (!waitGreenReq.HasValue || waitGreenReq.Priority < abortGreenReq.Priority) &&
                                                  (!holdGreenReq.HasValue || holdGreenReq.Priority < abortGreenReq.Priority))
                    {
                        InternalState = InternalSignalGroupStateEnum.ExtendGreen;
                        GreenExtend.Start();
                    }
                    break;

                case InternalSignalGroupStateEnum.ExtendGreen:
                    if (!extendGreenReq.HasValue && !holdRedReq.HasValue || 
                        abortGreenReq.HasValue && (!extendGreenReq.HasValue || extendGreenReq.Priority < abortGreenReq.Priority) &&
                                                  (!holdGreenReq.HasValue || holdGreenReq.Priority < abortGreenReq.Priority))
                    {
                        GreenExtend.Stop();
                        InternalState = InternalSignalGroupStateEnum.FreeExtendGreen;
                    }
                    break;

                case InternalSignalGroupStateEnum.FreeExtendGreen:
#warning abortgreen cannot override holdgreen with this code...
                    if (!freeExtendGreenReq.HasValue && !holdGreenReq.HasValue ||
                        abortGreenReq.HasValue && (!freeExtendGreenReq.HasValue || freeExtendGreenReq.Priority < abortGreenReq.Priority) &&
                                                  (!holdGreenReq.HasValue || holdGreenReq.Priority < abortGreenReq.Priority))
                    {
                        InternalState = InternalSignalGroupStateEnum.Amber;
                        Amber.Start();
                        foreach(var igt in InterGreenTimes)
                        {
                            igt.Timer.Start();
                        }
                    }
                    break;

                case InternalSignalGroupStateEnum.Amber:
                    if (!Amber.Running)
                    {
                        InternalState = InternalSignalGroupStateEnum.FixedRed;
                        RedGuaranteed.Start();
                        RedFixed.Start();
                        if (FixedRequest != FixedRequestTypeEnum.None) FixedRequestDelay.Start();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public bool HasConflictWith(string sgname)
        {
            return InterGreenTimes.Any(igt => igt.ConflictingSignalGroup.Name == sgname);
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
            InternalState = InternalSignalGroupStateEnum.Red;

            Amber.Started += (o, e) =>
            {
                _greenReqIndex = 0;
            };

            GreenFixed.Started += (o, e) =>
            {
                CurrentWaitingTime.Reset();
            };

            RedGuaranteed.Start();
            RedFixed.Start();

            _stateRequests = new SignalGroupStateRequestModel[50];
            _greenRequests = new string[10];
            _stateReqIndex = 0;
            _greenReqIndex = 0;

            CurrentWaitingTime = new TimerModel("curWait" + Name, 300, TimerTypeEnum.Seconds);
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
            InternalState = InternalSignalGroupStateEnum.Red;
            GreenGuaranteed.Reset();
            GreenFixed.Reset();
            GreenExtend.Reset();
            Amber.Reset();
            RedGuaranteed.Reset();
            RedFixed.Reset();
            HeadMax.Reset();
            _stateReqIndex = 0;
            _greenReqIndex = 0;
        }

        #endregion // ITLCProFModelBase

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
            FixedRequestDelay = new TimerModel("fixreqdelay" + name, 0);
            
            OnCreated();

        }

        #endregion // Constructors
    }
}
