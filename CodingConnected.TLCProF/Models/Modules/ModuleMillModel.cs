using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "ModuleMill", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ModuleMillModel : ITLCProFModelBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        // State
        public bool ModuleStart { get; private set; }

        // References
        [IgnoreDataMember]
        public ControllerModel Controller { get; set; }
        [IgnoreDataMember]
        public ModuleModel CurrentModule { get; set; }
        [IgnoreDataMember]
        public ModuleModel WaitingModule { get; set; }
        [IgnoreDataMember]
        public List<SignalGroupModuleDataModel> AllModuleSignalGroups { get; set; }

        // Settings
        [DataMember]
        public List<ModuleModel> Modules { get; private set; }
        [DataMember(IsRequired = true)]
        public string WaitingModuleName { get; set; }

        #endregion // Properties

        #region Public Methods

        public void UpdatePrimaryRealisations()
        {
            foreach (var sg in CurrentModule.SignalGroups)
            {
                if(sg.HadPrimaryRealisation || sg.SkippedPrimaryRealisation || sg.AheadPrimaryRealisation)
                {
                    continue;
                }

                if(sg.SignalGroup.State == SignalGroupStateEnum.Green)
                {
                    sg.HadPrimaryRealisation = true;
                    continue;
                }

                if (sg.SignalGroup.GreenRequests.Any())
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this);
                }
            }

            foreach (var sg in AllModuleSignalGroups)
            {
                sg.UpdateState(Controller);
            }

            foreach (var sg in AllModuleSignalGroups)
            {
                if (!sg.SignalGroup.GreenRequests.Any() ||
                    sg.ModulesAheadAllowed == 0 ||
                    sg.SignalGroup.HasConflict ||
                    CurrentModule.SignalGroups.Any(x => x.SignalGroupName == sg.SignalGroupName)) continue;
                if (sg.AheadPrimaryRealisation)
                {
                    if (sg.SignalGroup.State != SignalGroupStateEnum.Green)
                    {
                        sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this);
                    }
                    else
                    {
                        sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.FreeExtendGreen, 0, this);
                    }
                }
                else if (sg.SignalGroup.GreenRequests.Any() && sg.MayRealisePrimaryAhead)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this);
                    sg.AheadPrimaryRealisation = true;
                    foreach (var sgc in CurrentModule.SignalGroups)
                    {
                        if (sgc.HadPrimaryRealisation || sgc.SignalGroup.State != SignalGroupStateEnum.Red)
                            continue;
                        if (sgc.SignalGroup.HasConflictWith(sg.SignalGroupName))
                        {
                            sgc.SkippedPrimaryRealisation = true;
                        }
                    }
                }
            }
        }

        public void MoveTheMill()
        {
            // Don't move if the waiting module is active, and there are no unhandled requests
            if (CurrentModule == WaitingModule && 
                Controller.SignalGroups.All(x => x.State == SignalGroupStateEnum.Green || x.GreenRequests.Count == 0))
            {
                return;
            }

            // Move on if all phases are done with cyclic green
            ModuleStart = false;
            if (CurrentModule.SignalGroups.All(x => !x.SignalGroup.CyclicGreen && (x.HadPrimaryRealisation || x.SkippedPrimaryRealisation || x.AheadPrimaryRealisation || x.SignalGroup.GreenRequests.Count == 0)))
            {
                foreach(var sg in CurrentModule.SignalGroups)
                {
                    sg.HadPrimaryRealisation = false;
                    sg.AheadPrimaryRealisation = false;
                    sg.SkippedPrimaryRealisation = false;
                }
                var i = Modules.IndexOf(CurrentModule);
                i++;
                CurrentModule = i >= Modules.Count ? Modules[0] : Modules[i];
                ModuleStart = true;
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

        #region ITLCProFModelBase

        public void Reset()
        {
            CurrentModule = WaitingModule;
            Modules.ForEach(x => x.Reset());
        }

        #endregion // ITLCProFModelBase

        #region Contructors

        public ModuleMillModel(ControllerModel controller)
        {
            Controller = controller;
            OnCreated();
        }

        #endregion // Contructors
    }
}
