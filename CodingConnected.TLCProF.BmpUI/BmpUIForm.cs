using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using CodingConnected.TLCProF.Models;
using CodingConnected.TLCProF.Logging;

namespace CodingConnected.TLCProF.BmpUI
{
    internal sealed class BmpUIForm : Form
    {
		#region Private Classes

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

		#endregion // Private Classes

        #region Imports

        const string Gtklib = "libgtk-x11-2.0.so.0";
        [DllImport(Gtklib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_window_fullscreen(IntPtr win);
        
        #endregion // Imports

        #region Fields
        
        private readonly Application _application;
	    private readonly ControllerModel _model;

	    private readonly Bitmap _mainBitmap;
        private readonly ImageView _mainImage;
        private readonly TableLayout _layout;
        private readonly RichTextArea _parserArea;
        private readonly Label _timeLabel;
        private readonly TextArea _commandTextBox;
	    private readonly TabControl _mainTab;
	    private readonly CheckToolItem[] _speedCheckButtons;

        private readonly List<BitmapDetector> _detectors = new List<BitmapDetector>();
        private readonly List<SignalGroupState> _signalGroupStates = new List<SignalGroupState>();
        private readonly List<DetectorState> _detectorStates = new List<DetectorState>();

		private readonly CommandHandler _commandHandler;
	    private readonly bool _updatealways;

	    private readonly int _bitmapWidth;
	    private readonly int _bitmapHeight;

		private bool _simulation;
        private int _speed;
	    private bool _fast;
        private volatile bool _suspendUpdate = true;

	    #endregion // Fields

        #region Events

        public event EventHandler<bool> SimulationChanged;
        public event EventHandler<int> SpeedChanged;
        public event EventHandler<bool> Halted;
        public event EventHandler StepButtonPressed;
        public event EventHandler<BitmapDetector> DetectorPresenceChanged;
        public event EventHandler<string> CommandEntered;

        #endregion // Events

        #region Public Properties

        public string ControllerInfo { private get; set; }

		#endregion // Public Properties

		#region // Private Properties

		/// <summary>
		/// This property controls the updates to the UI
		/// Normally, the UI gets updated when one or more object (signalgroups of detectors) change state
		/// Alternatively, when update always is true, or running at high speed, updates happen every 100ms
		/// </summary>
		private bool NeedsUpdate
        {
            set
            {
                if (_suspendUpdate || !value) return;
                _suspendUpdate = true;

                _application.Invoke(async () =>
                {
					// check state and floodfill where needed
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

					// cause bitmap update
                    var w = _mainImage.Width;
                    if (Platform.IsGtk)
                    {
                        _mainImage.Image = _mainBitmap;
                    }
                    if (w > 0)
                    {
                        _mainImage.Width = w;
                    }

					// if always update is true, or running at high speed, wait 100ms
                    if (_updatealways || _fast)
                        await Task.Delay(100);

                    _suspendUpdate = false;

					// if always update is true, or running at high speed, set self (tail-recursion)
					if (_updatealways || _fast)
                    {
                        NeedsUpdate = true;
                    }
                });
            }
        }

	    #endregion // Private Properties

		#region Private methods

        

        private void FillSignalGroup(System.Drawing.Point p, InternalSignalGroupStateEnum state)
        {
            switch (state)
            {
                case InternalSignalGroupStateEnum.FixedRed:
                    BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.Maroon);
                    break;
                case InternalSignalGroupStateEnum.Red:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.Maroon);
                    break;
                case InternalSignalGroupStateEnum.NilRed:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.Red);
                    break;
                case InternalSignalGroupStateEnum.FixedGreen:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.DarkGreen);
                    break;
                case InternalSignalGroupStateEnum.WaitGreen:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.DarkCyan);
                    break;
                case InternalSignalGroupStateEnum.ExtendGreen:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.LimeGreen);
                    break;
                case InternalSignalGroupStateEnum.FreeExtendGreen:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.GhostWhite);
                    break;
                case InternalSignalGroupStateEnum.Amber:
	                BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.Yellow);
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
                    BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.Cyan);
                    break;
                default:
                    BmpUITools.FloodFill(_mainBitmap, new SimplePoint(p.X, p.Y), Colors.LightGrey);
                    break;
            }
        }

	    private Command GetSpeedCommand(int speed)
	    {
			return new Command((o, e) =>
			{
				foreach (var speedCheckButton in _speedCheckButtons)
				{
					speedCheckButton.Checked = false;
				}
				_speedCheckButtons[speed - 1].Checked = true;
				_speed = speed;
				_fast = speed >= 4;
				SpeedChanged?.Invoke(this, _speed);
				NeedsUpdate = true;
			})
			{ Shortcut = Keys.Control | Keys.D0 + speed };
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
                var l = BmpUITools.GetPoints(_mainBitmap, new SimplePoint(p.X, p.Y));
                if (l != null)
                    _detectors.Add(new BitmapDetector(name, false, l));
            }
            else
            {
                var l = BmpUITools.GetPoints(_mainBitmap, new SimplePoint(p.X, p.Y));
                if (l != null)
                    _detectors.Add(new BitmapDetector(name, false, l));
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

	    public void InitializeObjects()
	    {
			foreach (var sg in _model.SignalGroups)
			{
				_signalGroupStates.Add(new SignalGroupState(sg.Name, sg.Coordinates, sg.InternalState));
				foreach (var d in sg.Detectors)
				{
					_detectorStates.Add(new DetectorState(d.Name, d.Coordinates, d.Presence));
				}
			}
		}

        #endregion // Public methods

        #region Constructor

        public BmpUIForm(string bitmapName, Application a, ControllerModel model, bool updatealways, bool externaldet)
        {
	        _application = a;
            _updatealways = updatealways;
	        _model = model;

            _commandHandler = new CommandHandler(model);
	        
			Title = Path.GetFileNameWithoutExtension(bitmapName);

            _mainImage = new ImageView();
            var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, bitmapName);
            if (!File.Exists(filename))
            {
                filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", bitmapName);
            }
            _mainBitmap = new Bitmap(filename);
			_bitmapWidth = _mainBitmap.Width;
            _bitmapHeight = _mainBitmap.Height;

            var tp1 = new TabPage(_mainImage)
            {
                Text = "Bitmap"
            };

            var tp2 = new TabPage()
            {
                Text = "Log"
            };

            var m = new MenuBar();
            var bmi = new ButtonMenuItem
            {
                Text = "File"
            };
            bmi.Items.Add(new ButtonMenuItem(new Command((sender, args) => { _application.Quit(); })){ Text = "Quit"});
            m.Items.Add(bmi);
	        
			var toolBar = new ToolBar();
            var simcheck = new CheckToolItem { Text = "Sim" };

            simcheck.Command = new Command((o, e) =>
				{
				    _simulation = !_simulation;
				    SimulationChanged?.Invoke(this, _simulation);
				    simcheck.Checked = _simulation;
				})
            {
	            Shortcut = Application.Instance.CommonModifier | Keys.F2
            };
            toolBar.Items.Add(simcheck);
            toolBar.Items.Add(new ButtonToolItem{ Enabled = false, Text = "Speed:" });

	        _speedCheckButtons = new CheckToolItem[5];
	        _speedCheckButtons[0] = new CheckToolItem {Text = "1", Checked = true};
	        _speedCheckButtons[1] = new CheckToolItem {Text = "2"};
	        _speedCheckButtons[2] = new CheckToolItem {Text = "3"};
	        _speedCheckButtons[3] = new CheckToolItem {Text = "4"};
	        _speedCheckButtons[4] = new CheckToolItem {Text = "5"};

            var pauseCheck = new CheckToolItem { Text = "Halt" };
            var stepButton = new ButtonToolItem { Text = "Step" };

			_speedCheckButtons[0].Command = GetSpeedCommand(1);
			_speedCheckButtons[1].Command = GetSpeedCommand(2);
			_speedCheckButtons[2].Command = GetSpeedCommand(3);
			_speedCheckButtons[3].Command = GetSpeedCommand(4);
			_speedCheckButtons[4].Command = GetSpeedCommand(5);

            stepButton.Command = new Command((o, e) =>
            {
                StepButtonPressed?.Invoke(this, EventArgs.Empty);
                NeedsUpdate = true;
            });

	        pauseCheck.Command = new Command((o, e) =>
				{
				    if (pauseCheck.Checked)
				    {
						Halted?.Invoke(this, true);
				    }
				    else
				    {
						Halted?.Invoke(this, false);
						SpeedChanged?.Invoke(this, _speed);
				    }
				    NeedsUpdate = true;
				    _suspendUpdate = false;
				})
			{ Shortcut = Keys.F5 };

            toolBar.Items.Add(_speedCheckButtons[0]);
            toolBar.Items.Add(_speedCheckButtons[1]);
            toolBar.Items.Add(_speedCheckButtons[2]);
            toolBar.Items.Add(_speedCheckButtons[3]);
            toolBar.Items.Add(_speedCheckButtons[4]);
            toolBar.Items.Add(pauseCheck);
            toolBar.Items.Add(stepButton);

			Menu = m;
            ToolBar = toolBar;

			if (Platform.IsGtk)
            {
                var fullscreencheck = new CheckCommand((o, e) =>
                {
                    var com = o as CheckCommand;
                    if (com != null && !com.Checked)
                    {
                        com.Checked = true;
                        Content = _mainImage;
                        Menu = null;
                        ToolBar = null;
                        gtk_window_fullscreen(NativeHandle);
                    }
                    else
                    {
	                    if (com != null) com.Checked = false;
	                    Content = _layout;
                        ToolBar = toolBar;
                        Menu = m;
                        ToolBar = toolBar;
                    }
                    NeedsUpdate = true;
                    _suspendUpdate = false;
                })
                {
                    Shortcut = Keys.F11,
                    ToolBarText = "Fullscreen"
                };
                toolBar.Items.Add(fullscreencheck);
                
            }

			if(!Platform.IsGtk)
				Icon = 
					new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("CodingConnected.TLCProF.BmpUI.icon.ico"));

            _parserArea = new RichTextArea
            {
                Font = new Font(FontFamilies.Monospace, 10f),
                BackgroundColor = Colors.Black,
                TextColor = Colors.White,
                ReadOnly = true
            };
	        _commandTextBox = new TextArea
	        {
		        AcceptsReturn = true,
		        Height = 25
	        };
	        _commandTextBox.KeyDown += CommandTextBoxOnKeyDown;

            var loglayout = new TableLayout();
            loglayout.Rows.Add(new TableRow(new TableCell(_parserArea)) { ScaleHeight = true });
            loglayout.Rows.Add(new TableRow(new TableCell(_commandTextBox)));
            tp2.Content = loglayout;

            bool closed = false;
            Closed += (o, e) => closed = true;

            Task.Run(async() =>
            {
                int time = 0;
                while (!closed)
                {
                    await Task.Delay(50);
                    time++;
                    if(time >= 1)
                    {
                        time = 0;
                        a.Invoke(() =>
                        {
                            _timeLabel.Text = model.Clock.CurrentTime.ToLongTimeString() + " " +
                                              model.Clock.CurrentTime.ToLongDateString() + " | " + ControllerInfo;
                        });
                    }
                }
            });

            tp1.ClientSize = new Size(_bitmapWidth, _bitmapHeight);
	        _mainTab = new TabControl();

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
                _mainImage.MouseDown += MainImageOnMouseDown;
            }

            Shown += OnShown;
        }

	    private void CommandTextBoxOnKeyDown(object sender1, KeyEventArgs keyEventArgs)
	    {
			if (keyEventArgs.Key != Keys.Enter) return;
			CommandEntered?.Invoke(this, _commandTextBox.Text);
			var ret = _commandHandler.HandleCommand(_commandTextBox.Text);
			_parserArea.Append(ret, true);
			_commandTextBox.Text = null;
			_commandTextBox.CaretIndex = 0;
		    keyEventArgs.Handled = true;
		}

	    private void OnShown(object sender1, EventArgs eventArgs)
	    {
			_suspendUpdate = false;
		    NeedsUpdate = true;

		    // On Windows, setting client size for tabpage with bitmap has no effect\
		    if (Platform.IsWinForms)
		    {
			    ClientSize = new Size(_mainTab.Width - _mainImage.Width + _mainBitmap.Width,
				    _timeLabel.Height + (_mainTab.Height - _mainImage.Height) + _mainBitmap.Height);
		    }
		}

	    private void MainImageOnMouseDown(object sender1, MouseEventArgs mouseEventArgs)
	    {
			var scrfact = _mainImage.Width / (double)_mainImage.Height;
		    var corPoint = new SimplePoint();
		    if (scrfact >= _bitmapWidth / (double)_bitmapHeight)
		    {
			    // space is left and right
			    var fact = (double)_bitmapHeight / _mainImage.Height;
			    corPoint.Y = (int)(fact * mouseEventArgs.Location.Y);
			    corPoint.X =
				    (int)(fact * (mouseEventArgs.Location.X - (_mainImage.Width * fact - _bitmapWidth) / fact / 2));
		    }
		    else
		    {
			    // above and below is space
			    var fact = (double)_bitmapWidth / _mainImage.Width;
			    corPoint.X = (int)(fact * mouseEventArgs.Location.X);
			    corPoint.Y = (int)(fact * (mouseEventArgs.Location.Y -
			                               (_mainImage.Height * fact - _bitmapHeight) / fact / 2));
		    }

		    if (corPoint.X >= 0 && corPoint.X < _bitmapWidth &&
		        corPoint.Y >= 0 && corPoint.Y < _bitmapHeight)
		    {
			    Task.Run(() =>
			    {
				    var d = _detectors.FirstOrDefault(x => x.Points.Contains(corPoint));
				    if (d == null) return;
				    d.Presence = !d.Presence;
				    DetectorPresenceChanged?.Invoke(this, d);
				    NeedsUpdate = true;
			    });
		    }
		}

	    #endregion // Constructor
    }
}
