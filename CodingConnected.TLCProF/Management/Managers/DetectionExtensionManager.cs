using System;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class DetectionExtensionManager : ManagerBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateExtension()
        {
            foreach (var sg in Controller.SignalGroups)
            {
                if (sg.State != SignalGroupStateEnum.Green)
                    continue;

                foreach (var d in sg.Detectors)
                {
                    if (d.Occupied && d.Extend != DetectorExtendingTypeEnum.None)
                    {
                        switch(d.Extend)
                        {
                            case DetectorExtendingTypeEnum.HeadMax:
                                if(sg.HeadMax.Running)
                                {
                                    sg.AddStateRequest(SignalGroupStateRequestEnum.ExtendGreen, 0, this);
                                }
                                break;

                            case DetectorExtendingTypeEnum.Measure:
                                if (sg.GreenExtend.Running)
                                {
                                    sg.AddStateRequest(SignalGroupStateRequestEnum.ExtendGreen, 0, this);
                                }
                                break;
                        }
                    }
                }
            }
        }

        #endregion // Private Methods

        #region Constructor

        public DetectionExtensionManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateExtension, ControllerFunctionalityEnum.Extension, 0);
        }

        #endregion // Constructor
    }
}
