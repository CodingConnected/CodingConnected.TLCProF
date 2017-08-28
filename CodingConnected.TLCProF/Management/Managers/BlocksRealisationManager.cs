using System;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class BlocksRealisationManager : ManagerBase
    {
        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateRealisations()
        {
            Controller.BlockStructure.UpdatePrimaryRealisations();
            Controller.BlockStructure.UpdatePrimaryAOTRealisations();
            Controller.BlockStructure.UpdateAtlernativeRealisations();
            Controller.BlockStructure.MoveBlockStructure();
        }

        #endregion // Private Methods

        #region Constructor

        public BlocksRealisationManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateRealisations, ControllerFunctionalityEnum.Realisation, 0);
        }

        #endregion // Constructor
    }
}
