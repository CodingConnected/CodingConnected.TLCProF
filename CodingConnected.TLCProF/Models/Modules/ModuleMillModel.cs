using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "ModuleMill", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ModuleMillModel
    {
        #region Fields

        #endregion // Fields

        #region Properties

        [DataMember]
        public List<ModuleModel> Modules { get; private set; }
        [DataMember(IsRequired = true)]
        public string WaitingModuleName { get; set; }
        
        [IgnoreDataMember]
        public ControllerModel Controller { get; set; }
        [IgnoreDataMember]
        public ModuleModel CurrentModule { get; set; }
        [IgnoreDataMember]
        public ModuleModel WaitingModule { get; set; }

        #endregion // Properties

        #region Public Methods

        public void UpdatePrimaryRealisations()
        {
            foreach (var sg in CurrentModule.SignalGroups)
            {
                if(sg.HadPrimaryRealisation || sg.SkippedPrimaryRealisation)
                {
                    continue;
                }

                if(sg.SignalGroup.State == SignalGroupStateEnum.Green || !sg.SignalGroup.GreenRequests.Any())
                {
                    sg.HadPrimaryRealisation = true;
                    continue;
                }

                if (sg.SignalGroup.GreenRequests.Any())
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this);
                }
                else
                {
                    sg.SkippedPrimaryRealisation = true;
                }
            }
        }

        public void MoveTheMill()
        {
            if(CurrentModule == WaitingModule && 
               !Controller.SignalGroups.Any(x => x.State != SignalGroupStateEnum.Green && x.GreenRequests.Any()))
            {
            }
            else if (!CurrentModule.SignalGroups.Any(x => x.SignalGroup.CyclicGreen || !x.HadPrimaryRealisation && !x.SkippedPrimaryRealisation))
            {
                foreach(var sg in CurrentModule.SignalGroups)
                {
                    sg.HadPrimaryRealisation = false;
                    sg.SkippedPrimaryRealisation = false;
                }
                var i = Modules.IndexOf(CurrentModule);
                i++;
                CurrentModule = i >= Modules.Count ? Modules[0] : Modules[i];
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private void OnCreated()
        {
            if (Modules == null)
            {
                Modules = new List<ModuleModel>();
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        #endregion // Private Methods

        #region Contructors

        public ModuleMillModel(ControllerModel controller)
        {
            Controller = controller;
            OnCreated();
        }

        #endregion // Contructors
    }
}
