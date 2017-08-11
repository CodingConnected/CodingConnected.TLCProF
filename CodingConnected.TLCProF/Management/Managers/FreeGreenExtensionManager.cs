using System;
using System.Linq;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class FreeGreenExtensionManager : ManagerBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateFreeExtendGreen()
        {
            foreach (var sg in Controller.SignalGroups)
            {
                if (sg.ExtendGreenFree && sg.InternalState == InternalSignalGroupStateEnum.FreeExtendGreen)
                {
                    var extend = Controller.SignalGroups.Any(sg2 => sg2.CyclicGreen && sg2.InterGreenTimes.All(x => x.ConflictingSignalGroup.Name != sg.Name)) &&
#warning Check for efficiency; this is almost the same as code used by ModuleMill; store in a property?
                                 Controller.ModuleMill.CurrentModule.SignalGroups
                                     .Any(x => !x.SignalGroup.CyclicGreen &&
                                               !(x.HadPrimaryRealisation || x.SkippedPrimaryRealisation ||
                                                 x.AheadPrimaryRealisation || !x.SignalGroup.HasGreenRequest) ||
                                               x.SignalGroup.IsInWaitingGreen);
                    if (!extend) continue;

                    foreach (var igt in sg.InterGreenTimes)
                    {
                        var mlcsg = Controller.ModuleMill.AllModuleSignalGroups.First(
                            x => x.SignalGroupName == igt.ConflictingSignalGroup.Name);
                        if (mlcsg.MayRealisePrimaryAhead)
                        {
                            extend = false;
                        }
                    }
                    if (extend)
                    {
                        sg.AddStateRequest(SignalGroupStateRequestEnum.FreeExtendGreen, 0, this);
                    }
                }
            }
        }

        #endregion // Private Methods

        #region Constructor

        public FreeGreenExtensionManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateFreeExtendGreen, ControllerFunctionalityEnum.FreeExtension, 0);
        }

        #endregion // Constructor
    }
}
