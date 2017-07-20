using System;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class DetectionRequestsManager : ManagerBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Private Methods
        
        private void UpdateRequests()
        {
            foreach(var sg in Controller.SignalGroups)
            {
                foreach(var d in sg.Detectors)
                {
                    if (!d.Occupied || d.Request == DetectorRequestTypeEnum.None) continue;
                    switch (d.Request)
                    {
                        case DetectorRequestTypeEnum.Red:
                            if (sg.State == SignalGroupStateEnum.Red)
                            {
                                sg.AddGreenRequest(new SignalGroupGreenRequestModel(this));
                            }
                            break;

                        case DetectorRequestTypeEnum.RedNonGuaranteed:
                            if (sg.State == SignalGroupStateEnum.Red &&
                                !sg.RedGuaranteed.Running)
                            {
                                sg.AddGreenRequest(new SignalGroupGreenRequestModel(this));
                            }
                            break;

                        case DetectorRequestTypeEnum.Amber:
                            if (sg.InternalState == InternalSignalGroupStateEnum.Amber ||
                                sg.State == SignalGroupStateEnum.Red)
                            {
                                sg.AddGreenRequest(new SignalGroupGreenRequestModel(this));
                            }
                            break;

                        case DetectorRequestTypeEnum.None:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        #endregion // Private Methods

        #region Constructor

        public DetectionRequestsManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateRequests, ControllerFunctionalityEnum.Requests, 0);
        }

        #endregion // Constructor
    }
}
