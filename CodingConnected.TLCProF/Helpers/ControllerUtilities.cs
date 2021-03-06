﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodingConnected.TLCProF.Models;

namespace CodingConnected.TLCProF.Helpers
{
    public static class ControllerUtilities
    {
        public static void SetAllReferences(ControllerModel controller)
        {
#warning This would be better with reflection and attributes
            foreach (var sg in controller.SignalGroups)
            {
                foreach (var igt in sg.InterGreenTimes)
                {
                    foreach (var sg2 in controller.SignalGroups)
                    {
                        if (igt.SignalGroupTo == sg2.Name)
                        {
                            igt.ConflictingSignalGroup = sg2;
                            break;
                        }
                    }
                }
            }

            foreach (var m in controller.BlockStructure.Blocks)
            {
                if (m.Name == controller.BlockStructure.WaitingBlockName)
                {
                    controller.BlockStructure.CurrentBlock = controller.BlockStructure.WaitingBlock = m;
                }
                foreach (var sgn in m.SignalGroups)
                {
                    foreach (var sg in controller.SignalGroups)
                    {
                        if (sg.Name == sgn.SignalGroupName)
                        {
                            sgn.SignalGroup = sg;
                            break;
                        }
                    }
                }
            }

            controller.BlockStructure.Controller = controller;
            controller.BlockStructure.AllBlocksSignalGroups = controller.BlockStructure.Blocks.SelectMany(x => x.SignalGroups)
                .ToList();

            foreach(var sgn in controller.Extras.SafetyGreenSignalGroups)
            {
                foreach (var sg in controller.SignalGroups)
                {
                    if (sg.Name == sgn.SignalGroupName)
                    {
                        sgn.SignalGroup = sg;
                        foreach(var d in sg.Detectors)
                        {
                            if(d.Name == sgn.DetectorName)
                            {
                                sgn.Detector = d;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
