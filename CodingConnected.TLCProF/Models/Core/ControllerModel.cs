using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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

        #endregion // Fields

        #region Properties

        [DataMember(Name = "ControllerData")]
        public ControllerDataModel Data { get; private set; }

        [DataMember]
        public List<SignalGroupModel> SignalGroups { get; private set; }

        [DataMember]
        public ModuleMillModel ModuleMill { get; private set; }

        [DataMember]
        public ExtraDataModel Extras { get; private set; }

        [DataMember]
        public ClockModel Clock { get; private set; }

        [IgnoreDataMember]
        public ControllerStateEnum ControllerState { get; set; }

        #endregion // Properties
        
        #region Private Methods

        private void OnCreated()
        {
            ControllerState = ControllerStateEnum.Control;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        #endregion // Private Methods

        #region Events

        [field: NonSerialized]
        public event EventHandler<SignalGroupModel> SignalGroupStateChanged;

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
            ModuleMill.Reset();
            Extras.Reset();
        }

        #endregion // ITLCProFModelBase

        #region Constructor

        public ControllerModel()
        {
            Data = new ControllerDataModel();
            SignalGroups = new List<SignalGroupModel>();
            ModuleMill = new ModuleMillModel(this);
            Extras = new ExtraDataModel();
            Clock = new ClockModel();

            OnCreated();
        }

        #endregion // Constructor
    }
}
