using System;
using System.Linq;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class FixedRequestsManager : ManagerBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Private Methods
        
        private void UpdateRequests()
        {
            foreach (var sg in Controller.SignalGroups)
            {
                if (sg.FixedRequest == FixedRequestTypeEnum.None) continue;
                switch (sg.FixedRequest)
                {
                    case FixedRequestTypeEnum.Red:
                        if (sg.State == SignalGroupStateEnum.Red && !sg.FixedRequestDelay.Running)
                        {
                            sg.AddGreenRequest("fixed");
                        }
                        break;

                    case FixedRequestTypeEnum.RedNoConflictingRequests:
                        if (sg.State == SignalGroupStateEnum.Red && !sg.FixedRequestDelay.Running &&
                            !sg.InterGreenTimes.Any(x => x.ConflictingSignalGroup.HasGreenRequest))
                        {
                            sg.AddGreenRequest("fixed_noconflicts");
                        }
                        break;

                    case FixedRequestTypeEnum.None:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion // Private Methods

        #region Constructor

        public FixedRequestsManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateRequests, ControllerFunctionalityEnum.Requests, 1);
        }

        #endregion // Constructor
    }
}
