﻿using System;
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
                    var extend = Controller.SignalGroups.Any(sg2 => sg2.CyclicGreen);
                    if (!extend) continue;
                    foreach (var igt in sg.InterGreenTimes)
                    {
                        var mlcsg = Controller.ModuleMill.AllModuleSignalGroups.First(
                            x => x.SignalGroupName == igt.ConflictingSignalGroup.Name);
                        if (mlcsg.MayRealisePrimaryAhead) extend = false;
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
            mainmanager.InsertFunctionality(UpdateFreeExtendGreen, ControllerFunctionalityEnum.Extension, 2);
        }

        #endregion // Constructor
    }
}