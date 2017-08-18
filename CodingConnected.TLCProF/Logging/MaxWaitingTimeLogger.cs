using System.IO;
using System.Text;
using System.Windows;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Models.Attributes;

namespace CodingConnected.TLCProF.Logging
{
    public class MaxWaitingTimeLogger
    {
        #region Fields

        private ControllerModel _Controller;

        #endregion // Fields

        #region Public Methods

        public void LogMaxWaitingTimeOccured()
        {
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), _Controller.Clock.CurrentTime.ToString("yyyyMMdd") +
                _Controller.Clock.CurrentTime.ToString("hhmmss") + _Controller.Data.Name + ".log.csv");

            var sb = new StringBuilder();

            sb.AppendLine("signal groups;");
            var props = typeof(SignalGroupModel).GetProperties();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttributes(typeof(LogWithDumpAttribute), true);
                if (attr.Length == 1)
                {
                    sb.Append($"{prop.Name};");
                }
            }
            sb.AppendLine();
            foreach (var sg in _Controller.SignalGroups)
            {
                foreach (var prop in props)
                {
                    var attr = prop.GetCustomAttributes(typeof(LogWithDumpAttribute), true);
                    if (attr.Length == 1)
                    {
                        sb.Append($"{prop.GetValue(sg)};");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("block structure;");
            props = typeof(BlockSignalGroupDataModel).GetProperties();
            sb.AppendLine($"waiting block;{_Controller.BlockStructure.WaitingBlockName};");
            sb.AppendLine($"current block;{_Controller.BlockStructure.CurrentBlock.Name};");
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttributes(typeof(LogWithDumpAttribute), true);
                if (attr.Length == 1)
                {
                    sb.Append($"{prop.Name};");
                }
            }
            sb.AppendLine();
            foreach (var sg in _Controller.BlockStructure.AllBlocksSignalGroups)
            {
                foreach (var prop in props)
                {
                    var attr = prop.GetCustomAttributes(typeof(LogWithDumpAttribute), true);
                    if (attr.Length == 1)
                    {
                        sb.Append($"{prop.GetValue(sg)};");
                    }
                }
                sb.AppendLine();
            }
            
            File.WriteAllText(fileName, sb.ToString());
        }

        #endregion // Public Methods

        #region Constructor

        public MaxWaitingTimeLogger(ControllerModel controller)
        {
            _Controller = controller;
            _Controller.MaximumWaitingTimeExceeded += (o, e) => { LogMaxWaitingTimeOccured(); };
        }

        #endregion // Constructor
    }
}
