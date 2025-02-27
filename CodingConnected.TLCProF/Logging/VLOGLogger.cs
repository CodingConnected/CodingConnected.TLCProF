using CodingConnected.TLCProF.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodingConnected.TLCProF.Logging
{
    public class VLOGLogger
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
     
        public DateTime LastTimeStamp;
        public byte[] logData = new byte[1024];
        private ControllerModel _controller;
        private List<VLOGSignalGroup> SignalGroups { get; set; } = [];
        private List<VLOGDetector> Detectors { get; set; } = [];

        private StreamingVLOGServer _streamingVLOGServer;

        private readonly bool _fileLogging;
        private StringBuilder _fileStringBuilder;
        private string _fileNextFileName;
        private readonly bool _streamingLogging;

        private class VLOGSignalGroup
        {
            public int Index;
            public string Name;
            public int GUS;
            public int WUS;
        }

        private class VLOGDetector
        {
            public int Index;
            public string Name;
            public int State;
        }

        public VLOGLogger(bool fileLogging, bool streamingLogging, int streamingPort = 7001)
        {
            _fileLogging = fileLogging;
            _streamingLogging = streamingLogging;

            if (_fileLogging) 
            {
                if (!Directory.Exists("Log"))
                {
                    Directory.CreateDirectory("Log");
                }
                _fileStringBuilder = new StringBuilder();
            }

            if (_streamingLogging)
            {
                _streamingVLOGServer = new StreamingVLOGServer(streamingPort, false);
                _streamingVLOGServer.Start();
            }
        }

        private int InternalSignalGroupStateEnumToVlogValue(InternalSignalGroupStateEnum state)
        {
            return state switch
            {
                InternalSignalGroupStateEnum.FixedRed => 0x07,
                InternalSignalGroupStateEnum.Red => 0x07,
                InternalSignalGroupStateEnum.NilRed => 0x00,
                InternalSignalGroupStateEnum.FixedGreen => 0x02,
                InternalSignalGroupStateEnum.WaitGreen => 0x03,
                InternalSignalGroupStateEnum.ExtendGreen => 0x04,
                InternalSignalGroupStateEnum.FreeExtendGreen => 0x05,
                InternalSignalGroupStateEnum.Amber => 0x06,
                _ => 0xFF
            };
        }

        private class VlogConstants
        {
            public const int StatusDp = 0x05;
            public const int StatusSgInt = 0x09;
            public const int StatusSgExt = 0x0D;
            public const int StatusGpsInt = 0x11;
            public const int StatusGpsExt = 0x13;

            public const int UpdateDp = 0x06;
            public const int UpdateSgInt = 0x0A;
            public const int UpdateSgExt = 0x0E;
        }

        private List<string> _VLOGConfiguration;
        private bool _firstHeader = true;

        public void InitVLOG(ControllerModel c)
        {
            _controller = c;
            _controller.SignalGroups.Sort((x, y) => x.Name.CompareTo(y.Name));
            for (int id = 0; id < _controller.SignalGroups.Count; id++)
            {
                var vsg = new VLOGSignalGroup
                {
                    Index = id,
                    Name = _controller.SignalGroups[id].Name,
                    GUS = (int)_controller.SignalGroups[id].InternalState,
                    WUS = (int)_controller.SignalGroups[id].State
                };
                SignalGroups.Add(vsg);
            }
            _controller.AllDetectors.Sort((x, y) => x.Name.CompareTo(y.Name));
            for (int id = 0; id < _controller.AllDetectors.Count; id++)
            {
                var vsg = new VLOGDetector
                {
                    Index = id,
                    Name = _controller.AllDetectors[id].Name,
                    State = _controller.AllDetectors[id].Presence ? 1 : 0
                };
                Detectors.Add(vsg);
            }

            CreateVLOGConfiguration(c);

            WriteHeader(_controller);
            _fileNextFileName = (_controller.Data.Name ?? "UNKNOWN") + "_" + $"{DateTime.Now:yyyyMMdd}_{DateTime.Now:HHmmss}" + ".vlg";
        }

        private void CreateVLOGConfiguration(ControllerModel c)
        {
            _VLOGConfiguration = new List<string>
            {
                $"**** VLOGCFG / versie 3.0.0 / {_controller.Data.Name ?? "UNKNOWN"} ****",
                "",
                "//SYS",
                $"SYS,\"{_controller.Data.Name ?? "UNKNOWN"}\"",
                "",
                "//DP"
            };
            var index = 0;
            foreach (var d in c.AllDetectors)
            {
                var dt = d.Type switch
                {
                    DetectorTypeEnum.Head => 0x0001 | 0x0100,
                    DetectorTypeEnum.Long => 0x0001 | 0x0200,
                    DetectorTypeEnum.Away => 0x0001 | 0x0400,
                    DetectorTypeEnum.Button => 0x0002,
                    DetectorTypeEnum.Jam => 0,
                    DetectorTypeEnum.Other => 0,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _VLOGConfiguration.Add($"DP,{index},\"{d.Name}\",{dt}");
                ++index;
            }
            _VLOGConfiguration.Add("");
            index = 0;
            _VLOGConfiguration.Add("//IS");
            //foreach (var ip in config.Inputs)
            //{
            //    if (tranlateIs[ip.Index] == -1) continue;
            //    result.Add($"IS,{tranlateIs[ip.Index]},\"{ip.Name}\",0");
            _VLOGConfiguration.Add($"IS,{index},\"dummy\"");

            //}
            _VLOGConfiguration.Add("");
            _VLOGConfiguration.Add("//FC");
            index = 0;
            foreach (var sg in c.SignalGroups)
            {
                var sgt = sg.Type switch
                {
                    SignalGroupType.Motorized => 0x0001,
                    SignalGroupType.Pedestrian => 0x0002,
                    SignalGroupType.Cyclist => 0x0004,
                    SignalGroupType.PublicTransport => 0x0008,
                    SignalGroupType.MotorizedAndPT => 0x0001 | 0x0008,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                _VLOGConfiguration.Add($"FC,{index},\"{sg.Name}\",{sgt}");
                ++index;
            }
            _VLOGConfiguration.Add("");
            _VLOGConfiguration.Add("//US");
            index = 0;
            _VLOGConfiguration.Add($"US,{index},\"dummy\"");

            //foreach (var op in config.Outputs)
            //{
            //    if (tranlateUs[op.Index] == -1) continue;
            //    result.Add($"US,{tranlateUs[op.Index]},\"{op.Name}\",0");
            //}
            _VLOGConfiguration.Add("");
            _VLOGConfiguration.Add("**** EINDE VLOGCFG ****");
        }

        private void BroadcastVLOGMessage(byte[] buffer, int byteCount)
        {
            var ascii = ByteArrayToHexViaLookup32(buffer, byteCount).Replace("-", "") + "\r\n";
            MessageBroadcast?.Invoke(this, ascii);

            if (_fileLogging)
            {
                // in case of file logging, write the buffer to disk on new timestamp
                if (buffer[0] == 0x01 && !string.IsNullOrWhiteSpace(_fileNextFileName))
                {
                    var fileName = Path.Combine("Log", $"{_fileNextFileName}");
                    try 
                    { 
                        if (File.Exists(fileName)) File.Delete(fileName);
                        File.WriteAllText(fileName, _fileStringBuilder.ToString());
                    }
                    catch (Exception ex) 
                    {
                        _logger.Error(ex, $"Could not write VLOG file {fileName}");
                    }
                    _fileStringBuilder.Clear();
                }
                _fileStringBuilder.Append(ascii);
            }

            if (_streamingLogging)
            {
                _streamingVLOGServer.ForwardString(ascii);
            }
        }

        private static readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        private static string ByteArrayToHexViaLookup32(byte[] bytes, int byteCount)
        {
            var lookup32 = _lookup32;
            var result = new char[byteCount * 2];
            for (int i = 0; i < byteCount; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public event EventHandler<string> MessageBroadcast;

        public void Update()
        {
            if ((DateTime.Now.Minute % 5) == 0 &&
                DateTime.Now.Second == 0 &&
                DateTime.Now.Millisecond < 100)
            {
                WriteHeader(_controller);
                _fileNextFileName = (_controller.Data.Name ?? "UNKNOWN") + "_" + $"{DateTime.Now:yyyyMMdd}_{DateTime.Now:HHmmss}" + ".vlg";
            }

            var offset = (int)((DateTime.Now - LastTimeStamp).TotalMilliseconds / 100);

            // DP
            var id = 0;
            var currentLength = 0;
            var itemCount = 0;
            while (id < _controller.AllDetectors.Count)
            {
                for (; id < _controller.AllDetectors.Count; id++)
                {
                    if (Detectors[id].State != (_controller.AllDetectors[id].Presence ? 1 : 0))
                    {
                        if (currentLength == 0)
                        {
                            logData[currentLength++] = VlogConstants.UpdateDp;
                            logData[currentLength++] = (byte)(offset >> 4);
                            logData[currentLength] = 0;
                            logData[currentLength++] |= (byte)(offset << 4);
                        }
                        logData[currentLength++] = (byte)id; // index
                        logData[currentLength++] = (byte)((_controller.AllDetectors[id].Presence ? 1 : 0) & 0x0F); // state
                        ++itemCount;
                        Detectors[id].State = (_controller.AllDetectors[id].Presence ? 1 : 0);
                    }
                    if (itemCount >= 0x0F) break;
                }
                if (itemCount > 0)
                {
                    logData[2] |= (byte)(itemCount & 0x0F); // Set number of items                                                                                                                                                                                                
                    BroadcastVLOGMessage(logData, currentLength);
                    currentLength = 0;
                    itemCount = 0;
                }
            }

            // SG GUS
            id = 0;
            currentLength = 0;
            itemCount = 0;
            while (id < _controller.SignalGroups.Count)
            {
                for (; id < _controller.SignalGroups.Count; id++)
                {
                    if (SignalGroups[id].GUS != (int)_controller.SignalGroups[id].InternalState)
                    {
                        if (currentLength == 0)
                        {
                            logData[currentLength++] = VlogConstants.UpdateSgInt;
                            logData[currentLength++] = (byte)(offset >> 4);
                            logData[currentLength] = 0;
                            logData[currentLength++] |= (byte)(offset << 4);
                        }
                        logData[currentLength++] = (byte)id; // index
                        var st = InternalSignalGroupStateEnumToVlogValue(_controller.SignalGroups[id].InternalState);
                        logData[currentLength++] |= (byte)((st >> 8) & 0x0F); // state
                        logData[currentLength++] = (byte)st;
                        ++itemCount;
                        SignalGroups[id].GUS = (int)_controller.SignalGroups[id].InternalState;
                    }
                    if (itemCount >= 0x0F) break;
                }
                if (itemCount > 0)
                {
                    logData[2] |= (byte)(itemCount & 0x0F); // Set number of items                                                                                                                                                                                                
                    BroadcastVLOGMessage(logData, currentLength);
                    currentLength = 0;
                    itemCount = 0;
                }
            }

            // SG WUS
            id = 0;
            currentLength = 0;
            itemCount = 0;
            while (id < _controller.SignalGroups.Count)
            {
                for (; id < _controller.SignalGroups.Count; id++)
                {
                    if (SignalGroups[id].WUS != (int)_controller.SignalGroups[id].State)
                    {
                        if (currentLength == 0)
                        {
                            logData[currentLength++] = VlogConstants.UpdateSgExt;
                            logData[currentLength++] = (byte)(offset >> 4);
                            logData[currentLength] = 0;
                            logData[currentLength++] |= (byte)(offset << 4);
                        }
                        logData[currentLength++] = (byte)id; // index
                        logData[currentLength++] = (byte)((int)_controller.SignalGroups[id].State & 0x0F); // state
                        ++itemCount;
                        SignalGroups[id].WUS = (int)_controller.SignalGroups[id].State;
                    }
                    if (itemCount >= 0x0F) break;
                }
                if (itemCount > 0)
                {
                    logData[2] |= (byte)(itemCount & 0x0F); // Set number of items                                                                                                                                                                                                
                    BroadcastVLOGMessage(logData, currentLength);
                    currentLength = 0;
                    itemCount = 0;
                }
            }
        }

        public void WriteHeader(ControllerModel c)
        {
            LastTimeStamp = DateTime.Now;

            var currentLength = 0;

            // Timestamp
            logData[currentLength++] = 0x01;
            logData[currentLength++] = (byte)((LastTimeStamp.Year / 1000) << 4 | (LastTimeStamp.Year % 1000 / 100));
            logData[currentLength++] = (byte)((LastTimeStamp.Year % 100 / 10) << 4 | (LastTimeStamp.Year % 10));
            logData[currentLength++] = (byte)((LastTimeStamp.Month / 10) << 4 | (LastTimeStamp.Month % 10));
            logData[currentLength++] = (byte)((LastTimeStamp.Day / 10) << 4 | (LastTimeStamp.Day % 10));
            logData[currentLength++] = (byte)((LastTimeStamp.Hour / 10) << 4 | (LastTimeStamp.Hour % 10));
            logData[currentLength++] = (byte)((LastTimeStamp.Minute / 10) << 4 | (LastTimeStamp.Minute % 10));
            logData[currentLength++] = (byte)((LastTimeStamp.Second / 10) << 4 | (LastTimeStamp.Second % 10));
            logData[currentLength++] = (byte)((LastTimeStamp.Millisecond / 100 / 10) << 4 | ((LastTimeStamp.Millisecond / 100) % 10));
            BroadcastVLOGMessage(logData, currentLength);

            // Info
            currentLength = 0;
            logData[currentLength++] = 0x04; // Type
            logData[currentLength++] = 0x03; // VLOG version
            logData[currentLength++] = 0x00;
            logData[currentLength++] = 0x00;
            var charCount = 0;
            if (!string.IsNullOrEmpty(c.Data.Name))
            {
                foreach (var ch in Encoding.ASCII.GetBytes(c.Data.Name))
                {
                    logData[currentLength++] = ch;
                    ++charCount;
                    if (charCount >= 20) break;
                }
            }
            while (charCount < 20)
            {
                logData[currentLength++] = 0x20;
                ++charCount;
            }
            BroadcastVLOGMessage(logData, currentLength);

            // Detector STATUS
            currentLength = 0;
            logData[currentLength++] = VlogConstants.StatusDp;
            logData[currentLength++] = 0; // offset == 0 ; (byte)((int)0 >> 4);
            logData[currentLength] = 0;
            logData[currentLength++] |= (byte)(((int)c.AllDetectors.Count >> 8) & 0xFF);
            logData[currentLength++] = (byte)((int)c.AllDetectors.Count);
            for (int id = 0; id < c.AllDetectors.Count; id++)
            {
                if ((id % 2) == 0)
                {
                    logData[currentLength] = 0;
                    logData[currentLength] |= (byte)((c.AllDetectors[id].Presence ? 1 : 0) << 4);
                }
                else
                {
                    logData[currentLength++] |= (byte)((c.AllDetectors[id].Presence ? 1 : 0) & 0x0F);
                }
            }
            if (c.SignalGroups.Count % 2 != 0)
            {
                currentLength++;
            }
            BroadcastVLOGMessage(logData, currentLength);

            // Signalgroup intternal STATUS
            currentLength = 0;
            logData[currentLength++] = VlogConstants.StatusSgInt;
            logData[currentLength++] = 0; // offset == 0 ; (byte)((int)0 >> 4);
            logData[currentLength] = 0;
            logData[currentLength++] |= (byte)(((int)c.SignalGroups.Count >> 8) & 0xFF);
            logData[currentLength++] = (byte)((int)c.SignalGroups.Count);
            for (int id = 0; id < c.SignalGroups.Count; id++)
            {
                var st = InternalSignalGroupStateEnumToVlogValue(_controller.SignalGroups[id].InternalState);
                if ((id % 2) == 0)
                {
                    logData[currentLength++] = (byte)(st >> 4);
                    logData[currentLength] = 0;
                    logData[currentLength] |= (byte)(st << 4);
                }
                else
                {
                    logData[currentLength++] |= (byte)((st >> 8) & 0x0F);
                    logData[currentLength++] = (byte)st;
                }
            }
            if (c.SignalGroups.Count % 2 != 0)
            {
                currentLength++;
            }
            BroadcastVLOGMessage(logData, currentLength);

            // Signalgroup external STATUS
            currentLength = 0;
            logData[currentLength++] = VlogConstants.StatusSgExt;
            logData[currentLength++] = 0; // offset == 0 ; (byte)((int)0 >> 4);
            logData[currentLength] = 0;
            logData[currentLength++] |= (byte)(((int)c.SignalGroups.Count >> 8) & 0xFF);
            logData[currentLength++] = (byte)((int)c.SignalGroups.Count);
            for (int id = 0; id < c.SignalGroups.Count; id++)
            {
                if ((id % 2) == 0)
                {
                    logData[currentLength] = 0;
                    logData[currentLength] |= (byte)((int)c.SignalGroups[id].State << 4);
                }
                else
                {
                    logData[currentLength++] |= (byte)((int)c.SignalGroups[id].State & 0x0F);
                }
            }
            if (c.SignalGroups.Count % 2 != 0)
            {
                currentLength++;
            }
            BroadcastVLOGMessage(logData, currentLength);
            
            currentLength = 0;
            logData[currentLength++] = VlogConstants.StatusGpsInt;
            logData[currentLength++] = 0; // offset == 0 ; (byte)((int)0 >> 4);
            logData[currentLength] = 0;
            logData[currentLength++] |= 0;
            logData[currentLength++] = 1; // set count to 1 fixed
            logData[currentLength] = 0;
            logData[currentLength] |= 5 << 4;
            currentLength++; // 
            BroadcastVLOGMessage(logData, currentLength);

            currentLength = 0;
            logData[currentLength++] = VlogConstants.StatusGpsExt;
            logData[currentLength++] = 0; // offset == 0 ; (byte)((int)0 >> 4);
            logData[currentLength] = 0;
            logData[currentLength++] |= 0;
            logData[currentLength++] = 1; // set count to 1 fixed
            logData[currentLength] = 0;
            logData[currentLength] |= 5 << 4;
            currentLength++; // 
            BroadcastVLOGMessage(logData, currentLength);

            if (_firstHeader || DateTime.Now.Minute == 0)
            {
                _firstHeader = false;
                for (int i = 0; i < _VLOGConfiguration.Count; i++)
                {
                    currentLength = 0;
                    var t = i switch
                    {
                        0 => 1,
                        var x when x == (_VLOGConfiguration.Count - 1) => 3,
                        _ => 2
                    };
                    logData[currentLength++] = 0x7D;
                    var tr = ((t & 0x3) << 14) | ((i + 1) & 0x3FFF);
                    logData[currentLength++] = (byte)(tr >> 8);
                    logData[currentLength++] = (byte)tr;
                    foreach (var ch in Encoding.ASCII.GetBytes(_VLOGConfiguration[i]))
                    {
                        logData[currentLength++] = ch;
                    };
                    BroadcastVLOGMessage(logData, currentLength);
                }
            }           
        }
    }
}
