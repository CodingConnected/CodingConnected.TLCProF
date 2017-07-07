using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Management
{
    public abstract class ManagerBase
    {
        protected ControllerModel Controller;
        protected ControllerManager MainManager;

        protected ManagerBase(ControllerManager mainmanager, ControllerModel controller)
        {
            MainManager = mainmanager;
            Controller = controller;
        }
    }
}
