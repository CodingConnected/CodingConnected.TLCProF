using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using NLog;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "Controller", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ControllerModel : ITLCProFModelBase
    {
        #region Fields

        [field: NonSerialized]
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Queue<string> _greenLog;
        private int _greenLogIdx;
        private string _lastLogString;

        #endregion // Fields

        #region Properties

        [DataMember(Name = "ControllerData")]
        public ControllerDataModel Data { get; private set; }

        [DataMember]
        public List<SignalGroupModel> SignalGroups { get; private set; }

        [DataMember]
        public BlockStructureModel BlockStructure { get; private set; }

        [DataMember]
        public ExtraDataModel Extras { get; private set; }

        [DataMember]
        public ClockModel Clock { get; private set; }

        [IgnoreDataMember]
        public ControllerStateEnum ControllerState { get; set; }

        [IgnoreDataMember]
        public Queue<string> GreenLog => _greenLog;

        #endregion // Properties
        
        #region Private Methods

        private void OnCreated()
        {
            ControllerState = ControllerStateEnum.Control;
            Data.Controller = this;
            _greenLog = new Queue<string>();
            _greenLogIdx = 25;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        internal void OnMaximumWaitingTimeExceeded(object sender, EventArgs e)
        {
            MaximumWaitingTimeExceeded?.Invoke(this, e);
        }

        #endregion // Private Methods

        #region Public Methods

        public void UpdateGreenLog()
        {
            StringBuilder sb = new StringBuilder();
            if (_greenLogIdx == 25)
            {
                _greenLogIdx = 0;
                sb.Append(";");
                foreach (var sg in SignalGroups)
                {
                    sb.Append(sg.Name + ";");
                }
                sb.Append("block;");
                _greenLog.Enqueue(sb.ToString());
                sb.Clear();
            }

            sb.Append($"{Clock.CurrentTime.ToLongTimeString()};");
            foreach (var sg in SignalGroups)
            {
                switch (sg.InternalState)
                {
                    case InternalSignalGroupStateEnum.FixedRed:
                        sb.Append("R;");
                        break;
                    case InternalSignalGroupStateEnum.Red:
                        sb.Append("r;");
                        break;
                    case InternalSignalGroupStateEnum.NilRed:
                        sb.Append(".;");
                        break;
                    case InternalSignalGroupStateEnum.FixedGreen:
                        sb.Append("G;");
                        break;
                    case InternalSignalGroupStateEnum.WaitGreen:
                        sb.Append("W;");
                        break;
                    case InternalSignalGroupStateEnum.ExtendGreen:
                        sb.Append("X;");
                        break;
                    case InternalSignalGroupStateEnum.FreeExtendGreen:
                        sb.Append("F;");
                        break;
                    case InternalSignalGroupStateEnum.Amber:
                        sb.Append("A;");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            sb.Append(BlockStructure.CurrentBlock.Name +  ";");
            if (_lastLogString != null)
            {
                if (_lastLogString.Substring(8) != sb.ToString().Substring(8))
                {
                    _greenLog.Enqueue(sb.ToString());
                    if (_greenLog.Count > 250) _greenLog.Dequeue();
                    _lastLogString = sb.ToString();
                    _greenLogIdx++;
                }
            }
            else
            {
                _greenLog.Enqueue(sb.ToString());
                _lastLogString = sb.ToString();
                _greenLogIdx++;
            }
        }


        #endregion // Public Methods

        #region Events

        [field: NonSerialized]
        public event EventHandler<SignalGroupModel> SignalGroupStateChanged;

        [field: NonSerialized]
        public event EventHandler MaximumWaitingTimeExceeded;

        #endregion // Events

        #region Internal Methods

        internal void OnSignalGroupStateChanged(object sender, EventArgs e)
        {
            SignalGroupStateChanged?.Invoke(this, sender as SignalGroupModel);
        }

        #endregion // Internal Methods

        #region ITLCProFModelBase

        public void Reset()
        {
            ControllerState = ControllerStateEnum.Control;
            Clock.Reset();
            SignalGroups.ForEach(x => x.Reset());
            BlockStructure.Reset();
            Extras.Reset();
        }

        #endregion // ITLCProFModelBase

        #region Constructor

        public ControllerModel()
        {
            Data = new ControllerDataModel();
            SignalGroups = new List<SignalGroupModel>();
            BlockStructure = new BlockStructureModel(this);
            Extras = new ExtraDataModel();
            Clock = new ClockModel();

            OnCreated();
        }

        #endregion // Constructor
    }
}
