using System;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class SafetyGreenManager : ManagerBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateSafetyGreen()
        {
            foreach (var sg in Controller.Extras.SafetyGreenSignalGroups)
            {
                if (sg.NeedsExtending)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.HoldGreen, 1, this);
                }
            }
        }

        #endregion // Private Methods

        #region Constructor

        public SafetyGreenManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateSafetyGreen, ControllerFunctionalityEnum.Extension, 1);
        }

        #endregion // Constructor
    }
}
