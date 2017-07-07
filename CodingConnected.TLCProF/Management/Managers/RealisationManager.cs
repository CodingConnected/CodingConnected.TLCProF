using System;
using CodingConnected.TLCProF.Models;
using JetBrains.Annotations;

namespace CodingConnected.TLCProF.Management.Managers
{
    [ControllerManager]
    [UsedImplicitly]
    public class RealisationManager : ManagerBase
    {
        #region Properties

        #endregion // Properties

        #region Private Methods

        private void UpdateRealisations()
        {
            Controller.ModuleMill.UpdatePrimaryRealisations();
            Controller.ModuleMill.MoveTheMill();
        }

        #endregion // Private Methods

        #region Constructor

        public RealisationManager(ControllerManager mainmanager, ControllerModel controller) : base(mainmanager, controller)
        {
            mainmanager.InsertFunctionality(UpdateRealisations, ControllerFunctionalityEnum.Realisation, 0);
        }

        #endregion // Constructor
    }
}
