using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "SignalGroupModuleData", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class SignalGroupModuleDataModel : ITLCProFModelBase
    {
        #region Properties

        // State
        [IgnoreDataMember]
        public bool HadPrimaryRealisation { get; set; }
        [IgnoreDataMember]
        public bool SkippedPrimaryRealisation { get; set; }
        [IgnoreDataMember]
        public bool AheadPrimaryRealisation { get; set; }
        [IgnoreDataMember]
        public bool MayRealisePrimaryAhead { get; private set; }

        // Settings
        [DataMember]
        public string SignalGroupName { get; private set; }
        [DataMember]
        public int ModulesAheadAllowed { get; set; }

        // References
        [IgnoreDataMember]
        public SignalGroupModel SignalGroup { get; set; }

        #endregion // Properties

        #region ITLCProFModelBase

        public void Reset()
        {
            HadPrimaryRealisation = false;
            SkippedPrimaryRealisation = false;
            AheadPrimaryRealisation = false;
        }

        #endregion // ITLCProFModelBase

        #region Public Methods

        public void UpdateState(ControllerModel controller)
        {
            if (SignalGroup.State != SignalGroupStateEnum.Green && SignalGroup.GreenRequests.Any())
            {
                UpdateMayRealisePrimaryAhead(controller);
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private void UpdateMayRealisePrimaryAhead(ControllerModel controller)
        {
            var currentModule = controller.ModuleMill.CurrentModule;
            if (currentModule.SignalGroups.Any(x => x.SignalGroup.CyclicGreen))
            {
                if (currentModule.SignalGroups.All(x => !x.SignalGroup.HasConflictWith(this.SignalGroupName) ||
                                                        x.AheadPrimaryRealisation || x.HadPrimaryRealisation ||
                                                        x.SkippedPrimaryRealisation || !x.SignalGroup.GreenRequests.Any()))
                {
                    var nextModule = currentModule;
                    for (var i = 0; i < this.ModulesAheadAllowed && i < controller.ModuleMill.Modules.Count; ++i)
                    {
                        var j = controller.ModuleMill.Modules.IndexOf(nextModule);
                        j++;
                        nextModule = j >= controller.ModuleMill.Modules.Count ? controller.ModuleMill.Modules[0] : controller.ModuleMill.Modules[j];
                        if (nextModule.SignalGroups.Any(x => x.SignalGroupName == this.SignalGroupName))
                        {
                            MayRealisePrimaryAhead = true;
                            return;
                        }
                    }
                }
            }
            MayRealisePrimaryAhead = false;
        }

        #endregion // Private Methods

        #region Constructor

        public SignalGroupModuleDataModel(string sgname)
        {
            SignalGroupName = sgname;
        }

        #endregion // Constructor
    }
}
