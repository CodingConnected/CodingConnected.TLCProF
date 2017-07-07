using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Eto.GtkSharp;
using NLog;
using NLog.Targets;
using CodingConnected.TLCProF.Models;
using System.Reflection;

namespace CodingConnected.TLCProF.BmpUI
{
    internal sealed class TLCProForm : Form
    {
        private class SignalGroupState
        {
            private InternalSignalGroupStateEnum _state;
            public string Name { get; }
            public System.Drawing.Point Coordinates { get; }
            public bool Changed;

            public InternalSignalGroupStateEnum State
            {
                get => _state;
                set
                {
                    _state = value;
                    Changed = true;
                }
            }

            public SignalGroupState(string name, System.Drawing.Point coordinates, InternalSignalGroupStateEnum state)
            {
                Name = name;
                Coordinates = coordinates;
                State = state;
                Changed = true;
            }
        }

        private class DetectorState
        {
            private bool _state;
            public string Name { get; }
            public System.Drawing.Point Coordinates { get; }
            public bool Changed;

            public bool State
            {
                get => _state;
                set
                {
                    _state = value;
                    Changed = true;
                }
            }

            public DetectorState(string name, System.Drawing.Point coordinates, bool state)
            {
                Name = name;
                Coordinates = coordinates;
                State = state;
                Changed = true;
            }
        }

        #region Imports

        const string gtklib = "libgtk-x11-2.0.so.0";
        [DllImport(gtklib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_fullscreen(IntPtr win);
        
        #endregion // Imports

        #region Fields
        
        private readonly Application _application;

        private readonly TabControl _mainTab;
        private readonly Bitmap _mainBitmap;
        private readonly ImageView _mainImage;
        private readonly TableLayout _layout;
        private readonly RichTextArea _logArea;
        private readonly Label _timeLabel;

        private readonly List<BitmapDetector> _detectors = new List<BitmapDetector>();
        private readonly List<SignalGroupState> _signalGroupStates = new List<SignalGroupState>();
        private readonly List<DetectorState> _detectorStates = new List<DetectorState>();
        private readonly ControllerModel _model;
        private readonly bool _updatealways;

        private bool _simulation;
        private bool _fast;
        private bool _needsUpdate;
        private bool _suspendUpdate = true;

        private static readonly Queue<string> _logQueue = new Queue<string>();
        private static bool _logChanged;

        #endregion // Fields

        #region Events

        public event EventHandler<bool> SimulationChanged;
        public event EventHandler<int> SpeedChanged;
        public event EventHandler<BitmapDetector> DetectorPresenceChanged;

        #endregion // Events

        #region Properties

        public bool NeedsUpdate
        {
            // ReSharper disable once UnusedMember.Global
            get => _needsUpdate;
            set
            {
                try
                {
                    _needsUpdate = value;
                    if (_suspendUpdate || !value) return;
                    _suspendUpdate = true;
                    _application.Invoke(async () =>
                    {
                        foreach (var sg in _signalGroupStates)
                        {
                            if (!sg.Changed) continue;
                            FillSignalGroup(sg.Coordinates, sg.State);
                            sg.Changed = false;
                        }
                        foreach (var d in _detectorStates)
                        {
                            if (!d.Changed) continue;
                            FillDetector(d.Coordinates, d.State);
                            d.Changed = false;
                        }

                        var w = _mainImage.Width;
                        var h = _mainImage.Height;
                        _mainImage.Image = _mainBitmap;
                        if (h > 0 && w > 0)
                        {
                            _mainImage.Height = h;
                            _mainImage.Width = w;
                        }
                        if(_updatealways || _fast)
                            await Task.Delay(100);

                        _suspendUpdate = false;
                        if (_updatealways || _fast)
                        {
                            NeedsUpdate = true;
                        }
                    });
                }
                catch
                {
                    // ignored
                }
            }
        }

        #endregion // Properties

        #region Private methods

        //private static Bitmap BitmapFromEditableBitmapLinux(EditableBitmap editableBitmap)
        //{
        //    var bm = new Bitmap(editableBitmap.Bitmap.Width, editableBitmap.Bitmap.Height, PixelFormat.Format32bppRgb);
        //    using (var bd = bm.Lock())
        //    {
        //        var pdata = bd.Data;
        //        for (var i = 0; i < bm.Width * bm.Height; ++i)
        //        {
        //            var j = i * 4;
        //            Marshal.WriteByte(pdata + j + 2, editableBitmap.Bits[j]);
        //            Marshal.WriteByte(pdata + j + 1, editableBitmap.Bits[j + 1]);
        //            Marshal.WriteByte(pdata + j, editableBitmap.Bits[j + 2]);
        //            Marshal.WriteByte(pdata + j + 3, editableBitmap.Bits[j + 3]);
        //        }
        //    }
        //    return bm;
        //}
        //
        //private static Bitmap BitmapFromEditableBitmap(EditableBitmap editableBitmap)
        //{
        //    var bm = new Bitmap(editableBitmap.Bitmap.Width, editableBitmap.Bitmap.Height, PixelFormat.Format32bppRgb);
        //    using (var bd = bm.Lock())
        //    {
        //        var pdata = bd.Data;
        //        for (var i = 0; i < bm.Width * bm.Height; ++i)
        //        {
        //            var j = i * 4;
        //            Marshal.WriteByte(pdata + j, editableBitmap.Bits[j]);
        //            Marshal.WriteByte(pdata + j + 1, editableBitmap.Bits[j + 1]);
        //            Marshal.WriteByte(pdata + j + 2, editableBitmap.Bits[j + 2]);
        //            Marshal.WriteByte(pdata + j + 3, editableBitmap.Bits[j + 3]);
        //        }
        //    }
        //    return bm;
        //}

        private void FloodFill(Bitmap bmp, Point pt, Color replacementColor)
        {
            if (pt.X == 0 || pt.Y == 0 || pt.X >= bmp.Width || pt.Y >= bmp.Height)
                return;

            var targetColor = bmp.GetPixel(pt.X, pt.Y);
            if (targetColor == replacementColor)
            {
                return;
            }
            var pixels = new Stack<Point>();

            pixels.Push(pt);
            while (pixels.Count != 0)
            {
                var temp = pixels.Pop();
                int y1 = temp.Y;
                while (y1 >= 0 && bmp.GetPixel(temp.X, y1) == targetColor)
                {
                    y1--;
                }
                y1++;
                var spanLeft = false;
                var spanRight = false;
                while (y1 < bmp.Height && bmp.GetPixel(temp.X, y1) == targetColor)
                {
                    bmp.SetPixel(temp.X, y1, replacementColor);

                    if (!spanLeft && temp.X > 0 && bmp.GetPixel(temp.X - 1, y1) == targetColor)
                    {
                        var np = new Point(temp.X - 1, y1);
                        pixels.Push(np);
                        spanLeft = true;
                    }
                    else if (spanLeft && temp.X - 1 == 0 && bmp.GetPixel(temp.X - 1, y1) != targetColor)
                    {
                        spanLeft = false;
                    }
                    if (!spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) == targetColor)
                    {
                        var np = new Point(temp.X + 1, y1);
                        pixels.Push(np);
                        spanRight = true;
                    }
                    else if (spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) != targetColor)
                    {
                        spanRight = false;
                    }
                    y1++;
                }
            }
        }

        private Point[] GetPoints(Bitmap bmp, Point pt)
        {
            if (pt.X == 0 || pt.Y == 0 || pt.X >= bmp.Width || pt.Y >= bmp.Height)
                return null;

            var targetColor = bmp.GetPixel(pt.X, pt.Y);
            if (targetColor.R != 1  || targetColor.G !=1 || targetColor.B != 1)
                return null;

            var l = new List<Point>();
            
            var pixels = new Stack<Point>();

            pixels.Push(pt);
            while (pixels.Count != 0)
            {
                var temp = pixels.Pop();
                int y1 = temp.Y;
                while (y1 >= 0 && bmp.GetPixel(temp.X, y1) == targetColor)
                {
                    y1--;
                }
                y1++;
                var spanLeft = false;
                var spanRight = false;
                while (y1 < bmp.Height && bmp.GetPixel(temp.X, y1) == targetColor)
                {
                    l.Add(new Point(temp.X, y1));

                    if (!spanLeft && temp.X > 0 && bmp.GetPixel(temp.X - 1, y1) == targetColor)
                    {
                        var np = new Point(temp.X - 1, y1);
                        if (!l.Any(x => x.X == np.X && x.Y == np.Y))
                        {
                            pixels.Push(np);
                            spanLeft = true;
                        }
                    }
                    else if (spanLeft && temp.X - 1 == 0 && bmp.GetPixel(temp.X - 1, y1) != targetColor)
                    {
                        spanLeft = false;
                    }
                    if (!spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) == targetColor)
                    {
                        var np = new Point(temp.X + 1, y1);
                        if (!l.Any(x => x.X == np.X && x.Y == np.Y))
                        {
                            pixels.Push(np);
                            spanRight = true;
                        }
                    }
                    else if (spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) != targetColor)
                    {
                        spanRight = false;
                    }
                    y1++;
                }
            }
            return l.ToArray();
        }

        private void FillSignalGroup(System.Drawing.Point p, InternalSignalGroupStateEnum state)
        {
            switch (state)
            {
                case InternalSignalGroupStateEnum.FixedRed:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.Maroon);
                    break;
                case InternalSignalGroupStateEnum.Red:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.Maroon);
                    break;
                case InternalSignalGroupStateEnum.NilRed:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.Red);
                    break;
                case InternalSignalGroupStateEnum.FixedGreen:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.DarkGreen);
                    break;
                case InternalSignalGroupStateEnum.WaitGreen:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.DarkCyan);
                    break;
                case InternalSignalGroupStateEnum.ExtendGreen:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.LimeGreen);
                    break;
                case InternalSignalGroupStateEnum.FreeExtendGreen:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.GhostWhite);
                    break;
                case InternalSignalGroupStateEnum.Amber:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.Yellow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void FillDetector(System.Drawing.Point p, bool presence)
        {
            switch (presence)
            {
                case true:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.Cyan);
                    break;
                default:
                    FloodFill(_mainBitmap, new Point(p.X, p.Y), Colors.LightGrey);
                    break;
            }
        }

        #endregion // Private methods

        #region Public methods
        
        public void AddDetector(string name, System.Drawing.Point p)
        {
            if (p.X <= 0 && p.Y <= 0)
            {
                return;
            }
            if (Platform.IsGtk)
            {
                var l = GetPoints(_mainBitmap, new Point(p.X, p.Y));
                if (l != null)
                    _detectors.Add(new BitmapDetector(name, false, p, l));
            }
            else
            {
                var l = GetPoints(_mainBitmap, new Point(p.X, p.Y));
                if (l != null)
                    _detectors.Add(new BitmapDetector(name, false, p, l));
            }
        }

        public void SetDetectorPresence(string name, bool pres, bool update = true)
        {
            var d = _detectors.FirstOrDefault(x => x.Name == name);
            if (d == null) return;
            d.Presence = pres;
            _detectorStates.First(x => x.Name == name).State = pres;
            if (update && !_fast) NeedsUpdate = true;
        }

        public void SetSignalGroupState(string name, InternalSignalGroupStateEnum state, bool update = true)
        {
            var signalGroupState = _signalGroupStates.FirstOrDefault(x => x.Name == name);
            if (signalGroupState != null)
                signalGroupState.State = state;
            if (update && !_fast) NeedsUpdate = true;
        }

        public static void LogMethod(string callsite, string level, string message)
        {
            if (_logQueue.Count >= 250)
            {
                _logQueue.Dequeue();
            }
            _logQueue.Enqueue("[" + level + "] [" + callsite + "] " + message);
            _logChanged = true;
        }

        #endregion // Public methods

        #region Constructor

        public TLCProForm(string bitmapName, Application a, ControllerModel model, bool updatealways, bool externaldet)
        {
            _application = a;
            _model = model;
            _updatealways = updatealways;

            var target = new MethodCallTarget()
            {
                ClassName = typeof(TLCProForm).AssemblyQualifiedName,
                MethodName = "LogMethod"
            };
            target.Parameters.Add(new MethodCallParameter("${callsite}"));
            target.Parameters.Add(new MethodCallParameter("${level}"));
            target.Parameters.Add(new MethodCallParameter("${message}"));

            //NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
            //SpeedChanged += (o, e) =>
            //{
            //    if(e != 0)
            //    {
            //        NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Off);
            //    }
            //    else
            //    {
            //        NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
            //    }
            //};
            
            foreach (var sg in model.SignalGroups)
            {
                _signalGroupStates.Add(new SignalGroupState(sg.Name, sg.Coordinates, sg.InternalState));
                foreach (var d in sg.Detectors)
                {
                    _detectorStates.Add(new DetectorState(d.Name, d.Coordinates, d.Presence));
                }
            }

            Location = new Point(0, 0);

            Title = Path.GetFileNameWithoutExtension(bitmapName);

            _mainTab = new TabControl();
            _mainImage = new ImageView();
            var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, bitmapName);
            if (!File.Exists(filename))
            {
                filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", bitmapName);
            }
            var bitmap = new Bitmap(filename);
            var bitmapWidth = bitmap.Width;
            var bitmapHeight = bitmap.Height;
            var tp1 = new TabPage(_mainImage)
            {
                Text = "Bitmap"
            };

            var tp2 = new TabPage()
            {
                Text = "Log"
            };

            _mainBitmap = bitmap;

            var m = new MenuBar();
            var bmi = new ButtonMenuItem
            {
                Text = "File"
            };
            bmi.Items.Add(new ButtonMenuItem(new Command((sender, args) => { _application.Quit(); })){ Text = "Quit"});
            m.Items.Add(bmi);

            Menu = m;
            var toolBar = new ToolBar();
            var simcheck = new CheckToolItem { Text = "Sim" };
            simcheck.Click += (o, e) => 
            {
                _simulation = !_simulation;
                SimulationChanged?.Invoke(this, _simulation);
                if (_simulation)
                {
                    simcheck.Checked = true;
                }
                else
                {
                    simcheck.Checked = false;
                }
            };
            toolBar.Items.Add(simcheck);
            var speedcheck = new CheckCommand((o, e) =>
            {
                var com = o as CheckCommand;
                _fast = !_fast;
                SpeedChanged?.Invoke(this, _fast ? 3 : 0);
                if (_fast)
                {
                    com.Checked = true;
                }
                else
                {
                    com.Checked = false;
                }
                NeedsUpdate = true;
                _suspendUpdate = false;
            })
            {
                ToolBarText = "Fast"
            };
            toolBar.Items.Add(speedcheck);

            if (Platform.IsGtk)
            {
                var fullscreencheck = new CheckCommand((o, e) =>
                {
                    var com = o as CheckCommand;
                    if (!com.Checked)
                    {
                        com.Checked = true;
                        this.Content = _mainImage;
                        this.Menu = null;
                        this.ToolBar = null;
                        gtk_window_fullscreen(this.NativeHandle);
                    }
                    else
                    {
                        com.Checked = false;
                        this.Content = _layout;
                        this.ToolBar = toolBar;
                        this.Menu = m;
                        this.ToolBar = toolBar;
                    }
                    NeedsUpdate = true;
                    _suspendUpdate = false;
                })
                {
                    Shortcut = Application.Instance.CommonModifier | Keys.F11,
                    ToolBarText = "Fullscreen"
                };
                toolBar.Items.Add(fullscreencheck);
                
            }
            this.ToolBar = toolBar;

            //this.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("TLC_PROF.BmpUI.icon.ico"));

            _logArea = new RichTextArea()
            {
                Font = new Font(FontFamilies.Monospace, 10f),
                BackgroundColor = Colors.Black,
                TextColor = Colors.White,
                ReadOnly = true
            };
            tp2.Content = _logArea;

            bool closed = false;
            this.Closed += (o, e) => closed = true;

            Task.Run(async() =>
            {
                int time = 0;
                while (!closed)
                {
                    await Task.Delay(100);
                    time++;
                    if(time >= 10)
                    {
                        time = 0;
                        if (_model != null)
                        {
                            a.Invoke(() =>
                            {
                                _timeLabel.Text = _model.Clock.CurrentTime.ToLongTimeString() + " " +
                                                  _model.Clock.CurrentTime.ToLongDateString();
                            });
                        }
                    }
                    if (_logChanged)
                    {
                        a.Invoke(() =>
                        {
                            while (_logQueue.Count > 0)
                            {
                                var s = _logQueue.Dequeue();
                                if (s.Contains("[Info]"))
                                    _logArea.SelectionForeground = Colors.White;
                                if (s.Contains("[Trace]"))
                                    _logArea.SelectionForeground = Colors.DarkGray;
                                if (s.Contains("[Warn]"))
                                    _logArea.SelectionForeground = Colors.Yellow;
                                if (s.Contains("[Debug]"))
                                    _logArea.SelectionForeground = Colors.Orange;
                                if (s.Contains("[Error]"))
                                    _logArea.SelectionForeground = Colors.Red;

                                _logArea.Append(s + Environment.NewLine);
                            }
                            if (_logArea.HasFocus)
                                _logArea.Selection = new Range<int>(_logArea.Text.Length - 1);
                        });
                        _logChanged = false;
                    }
                }
            });

            tp1.ClientSize = new Size(bitmapWidth, bitmapHeight);
            _mainTab.Pages.Add(tp1);
            _mainTab.Pages.Add(tp2);

            _layout = new TableLayout();
            _timeLabel = new Label
            {
                Text = "",
                Height = 24, VerticalAlignment = VerticalAlignment.Center
            };
            _timeLabel.VerticalAlignment = VerticalAlignment.Center;
            _layout.Rows.Add(new TableRow(new TableCell(_mainTab)){ ScaleHeight = true});
            _layout.Rows.Add(new TableRow(new TableCell(_timeLabel)));
            _mainImage.Image = _mainBitmap;
            Content = _layout;
            
            MinimumSize = new Size(320, 240);

            if (!externaldet)
            {
                _mainImage.MouseDown += (sender, args) =>
                {
                    var scrfact = _mainImage.Width / (double) _mainImage.Height;
                    var corPoint = new Point();
                    if (scrfact >= bitmapWidth / (double) bitmapHeight)
                    {
                        // space is left and right
                        var fact = (double) bitmapHeight / _mainImage.Height;
                        corPoint.Y = (int) (fact * args.Location.Y);
                        corPoint.X =
                            (int) (fact * (args.Location.X - (_mainImage.Width * fact - bitmapWidth) / fact / 2));
                    }
                    else
                    {
                        // above and below is space
                        var fact = (double) bitmapWidth / _mainImage.Width;
                        corPoint.X = (int) (fact * args.Location.X);
                        corPoint.Y = (int) (fact * (args.Location.Y -
                                                   (_mainImage.Height * fact - bitmapHeight) / fact / 2));
                    }

                    if (corPoint.X >= 0 && corPoint.X < bitmapWidth &&
                        corPoint.Y >= 0 && corPoint.Y < bitmapHeight)
                    {
                        Task.Run(() =>
                        {
                            var d = _detectors.FirstOrDefault(x => x.Points.Contains(corPoint));
                            if (d == null) return;
                            d.Presence = !d.Presence;
                            DetectorPresenceChanged?.Invoke(this, d);
                        });
                    }
                };
            }

            this.Shown += (o, e) =>
            {
                _suspendUpdate = false;
                NeedsUpdate = true;

                // On Windows, setting client size for tabpage with bitmap has no effect\
                if (Platform.IsWinForms)
                {
                    ClientSize = new Size(_mainTab.Width - _mainImage.Width + _mainBitmap.Width, 
                                          _timeLabel.Height + (_mainTab.Height - _mainImage.Height) + _mainBitmap.Height);    
                }
            };
        }

        #endregion // Constructor
    }
}
