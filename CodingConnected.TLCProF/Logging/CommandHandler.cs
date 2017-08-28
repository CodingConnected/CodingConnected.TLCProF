using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Models.Attributes;

namespace CodingConnected.TLCProF.Logging
{
    public class CommandHandler
    {
        private ControllerModel _controller;

        public CommandHandler(ControllerModel controller)
        {
            _controller = controller;
        }

        public string HandleCommand(string command)
        {
            var sb = new StringBuilder();

            if (command.ToLower().StartsWith("gd"))
            {
                foreach (var s in _controller.GreenLog)
                {
                    sb.AppendLine(s);
                }
            }
            else if (command.ToLower().StartsWith("sg"))
            {
                var sgname = command.ToLower().Replace(" ", "").Replace("sg", "");
                var sg = _controller.SignalGroups.FirstOrDefault(x => x.Name == sgname);
                if (sg != null)
                {
                    var props = typeof(SignalGroupModel).GetProperties();
                    sb.AppendLine($"signal group: {sg.Name}");
                    foreach (var prop in props)
                    {
                        var attr = prop.GetCustomAttributes(typeof(LogWithDumpAttribute), true);
                        if (attr.Length == 1)
                        {
                            sb.AppendLine($"   - {prop.Name}:{prop.GetValue(sg)}");
                        }
                    }
                }
            }
            else if (command.ToLower().StartsWith("gr"))
            {
                var sgname = command.ToLower().Replace(" ", "").Replace("gr", "");
                var sg = _controller.SignalGroups.FirstOrDefault(x => x.Name == sgname);
                if (sg != null)
                {
                    var reqs = sg.CurrentGreenRequests;
                    if (reqs.Count == 0)
                    {
                        sb.AppendLine($"signal group {sg.Name} has 0 green requests");
                    }
                    else
                    {
                        sb.AppendLine($"signal group {sg.Name} has {reqs.Count} green requests:");
                        foreach (var req in reqs)
                        {
                            sb.AppendLine($"   - {req}");
                        }
                    }
                }
            }
            else if (command.ToLower().StartsWith("sr"))
            {
                var sgname = command.ToLower().Replace(" ", "").Replace("sr", "");
                var sg = _controller.SignalGroups.FirstOrDefault(x => x.Name == sgname);
                if (sg != null)
                {
                    var reqs = sg.CurrentStateRequests;
                    if (reqs.Count == 0)
                    {
                        sb.AppendLine($"signal group {sg.Name} has 0 state requests");
                    }
                    else
                    {
                        sb.AppendLine($"signal group {sg.Name} has {reqs.Count} state requests:");
                        foreach (var req in reqs)
                        {
                            sb.AppendLine($"   - {req}");
                        }
                    }
                }
            }
            else if (command.ToLower().StartsWith("blsg"))
            {
                var sgname = command.ToLower().Replace(" ", "").Replace("blsg", "");
                var sg = _controller.BlockStructure.AllBlocksSignalGroups.FirstOrDefault(x => x.SignalGroupName == sgname);
                if (sg != null)
                {
                    var props = typeof(BlockSignalGroupDataModel).GetProperties();
                    sb.AppendLine($"signal group: {sg.SignalGroupName}");
                    foreach (var prop in props)
                    {
                        var attr = prop.GetCustomAttributes(typeof(LogWithDumpAttribute), true);
                        if (attr.Length == 1)
                        {
                            sb.AppendLine($"   - {prop.Name}:{prop.GetValue(sg)}");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("Unknown command");
            }
            return sb.ToString();
        }
    }
}
