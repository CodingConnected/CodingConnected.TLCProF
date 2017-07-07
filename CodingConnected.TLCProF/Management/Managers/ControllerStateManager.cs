using System;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class ControllerStateManager : ManagerBase
    {
        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateControllerState()
        {
            switch (Controller.ControllerState)
            {
                case ControllerStateEnum.Control:
                    foreach (var signalgroup in Controller.SignalGroups)
                    {
                        signalgroup.HandleStateRequests();
                    }
                    break;
                case ControllerStateEnum.AllRed:
                    foreach (var signalgroup in Controller.SignalGroups)
                    {
                        signalgroup.AddStateRequest(SignalGroupStateRequestEnum.AbortGreen, 999, this);
                        signalgroup.AddStateRequest(SignalGroupStateRequestEnum.HoldRed, 999, this);
                        signalgroup.HandleStateRequests();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion // Private Methods

        #region Constructor

        public ControllerStateManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateControllerState, ControllerFunctionalityEnum.Conclusion, 0);
        }

        #endregion // Constructor
    }
}
