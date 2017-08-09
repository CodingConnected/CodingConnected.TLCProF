using System;
using System.Linq;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class WaitGreenManager : ManagerBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateWaitGreen()
        {
            foreach (var sg in Controller.SignalGroups)
            {
                if (sg.WaitGreen && sg.InternalState == InternalSignalGroupStateEnum.WaitGreen)
                {
                    if (!sg.InterGreenTimes.Any(x => x.ConflictingSignalGroup.HasGreenRequest))
                    {
                        sg.AddStateRequest(SignalGroupStateRequestEnum.WaitGreen, 0, this);
                    }
                }
            }
        }

        #endregion // Private Methods

        #region Constructor

        public WaitGreenManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateWaitGreen, ControllerFunctionalityEnum.Extension, 2);
        }

        #endregion // Constructor
    }
}
