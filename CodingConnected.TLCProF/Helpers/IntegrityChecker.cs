using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Helpers
{
    public static class IntegrityChecker
    {
        public static bool IsControllerDataOK(ControllerModel c)
        {
            if(!IsInterGreenMatrixOK(c))
            {
                return false;
            }

            if (c.ModuleMill == null ||
                c.ModuleMill.Modules.Count == 0 ||
                 string.IsNullOrWhiteSpace(c.ModuleMill.WaitingModuleName))
            {
                return false;
            }
            return true;
        }

#warning works ok?? if not syummetric???
        public static bool IsInterGreenMatrixOK(ControllerModel c)
        {
            foreach (var sg in c.SignalGroups)
            {
                foreach (var igt in sg.InterGreenTimes)
                {
                    bool found = false;
                    foreach (var sg2 in c.SignalGroups)
                    {
                        foreach (var igt2 in sg2.InterGreenTimes)
                        {
                            if (igt.SignalGroupFrom == igt2.SignalGroupTo &&
                                igt.SignalGroupTo == igt2.SignalGroupFrom)
                            {
                                found = true;
                            }
                        }
                    }
                    if(!found)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool IsInternalStateOK(ControllerModel c)
        {
            foreach(var sg in c.SignalGroups)
            {
                if(sg.HasConflict && sg.State == SignalGroupStateEnum.Green)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
