using System;
using System.Linq;
using CodingConnected.TLCProF.Models;
using Eto.Forms;

namespace CodingConnected.TLCProF.BmpUI
{
    public class BmpUIMain
    {
        #region Fields

        private bool _initialized;
        
        private readonly bool _usemodelforupdate;
        private readonly bool _externaldet;
		private readonly Application _application;
        private readonly BmpUIForm _mainForm;
        private readonly ControllerModel _model;

        #endregion // Fields

        #region Properties

        public string ControllerInfo
        {
	        set => _mainForm.ControllerInfo = value;
        }

        #endregion // Properties

        #region Events

		/// <summary>
		/// Raised if the user changes the simulation state
		/// </summary>
        public event EventHandler<bool> SimulationChanged;

		/// <summary>
		/// Raised if the user changes the desired application speed
		/// The argument indicates the desired speed, ranging from 1 to 5
		/// </summary>
        public event EventHandler<int> SpeedChanged;

		/// <summary>
		/// Raised if the user wishes to halt or unhalt the application
		/// </summary>
        public event EventHandler<bool> HaltedChanged;

		/// <summary>
		/// Raised if the user requests a single application step to be executed
		/// </summary>
        public event EventHandler StepRequested;

		/// <summary>
		/// Raised once when the application is closed
		/// </summary>
        public event EventHandler<EventArgs> Closed;

		/// <summary>
		/// Raised when the user clicks on a detector in the bitmap
		/// The argument contains the internal data concerning the detector
		/// </summary>
        public event EventHandler<BitmapDetector> DetectorPresenceChanged;

		/// <summary>
		/// Raised when the user enters text into the command textbox and hits enter
		/// The text in the argument may be parsed and used to take appropriate action
		/// <remarks>The UI handles commands idenpendently of this event,
		/// using the default CommandHandler class; any custom handling is extra</remarks>
		/// </summary>
        public event EventHandler<string> CommandEntered;

        #endregion // Events

        #region Public methods

		/// <summary>
		/// Starts the main UI
		/// <remarks>Note that the Initialize() method MUST be called first.</remarks>
		/// </summary>
        public void StartUI()
        {
            if (!_initialized)
            {
                throw new NotSupportedException("Not initialized: cannot run.");
            }
            try
            {
                _application.Run(_mainForm);
            }
            catch
            {
                // ignored
            }
        }

		/// <summary>
		/// Initializes the UI, using data from the Controller object parsed to the constructor
		/// </summary>
        public void Initialize()
        {
			_mainForm.InitializeObjects();

            foreach (var sg in _model.SignalGroups)
            {
                _mainForm.SetSignalGroupState(sg.Name, sg.InternalState, false);
                sg.InternalStateChanged += (sender, args) =>
                {
                    if (_usemodelforupdate) _mainForm.SetSignalGroupState(sg.Name, sg.InternalState);
                };
                foreach (var d in sg.Detectors)
                {
                    _mainForm.AddDetector(d.Name, d.Coordinates);
                    _mainForm.SetDetectorPresence(d.Name, d.Presence, false);
                    d.PresenceChanged += (sender, b) =>
                    {
                        _mainForm.SetDetectorPresence(d.Name, b);
                    };
                }
            }
            _mainForm.DetectorPresenceChanged += (sender, detector) =>
            {
                var d = _model.SignalGroups.SelectMany(x => x.Detectors).FirstOrDefault(x => x.Name == detector.Name);
                if (d != null)
                {
                    d.Presence = detector.Presence;
                }
            };
            _initialized = true;
        }

		/// <summary>
		/// Sets the internal state for a given signalgroup.
		/// </summary>
		/// <param name="name">The name uniquely identifying this signalgroup</param>
		/// <param name="state">New internal state</param>
		/// <param name="update">Indicates whether or not to update the UI (defaults to true)</param>
        public void SetSignalGroupState(string name, InternalSignalGroupStateEnum state, bool update = true)
        {
            _mainForm.SetSignalGroupState(name, state, update);
        }

		/// <summary>
		/// Sets the presence of a given detector.
		/// This method enables using the UI as a display for controller state, without user interaction
		/// <remarks>This will only work if externaldet is set to true the construto</remarks>
		/// </summary>
		/// <param name="name">The name uniquely identifying this detector</param>
		/// <param name="pres">New presence state</param>
		/// <param name="update">Indicates whether or not to update the UI (defaults to true)</param>
		public void SetDetectorPresence(string name, bool pres, bool update = true)
        {
	        if (!_externaldet)
	        {
		        return;
	        }
            _mainForm.SetDetectorPresence(name, pres, update);
        }

		/// <summary>
		/// Closes the main UI
		/// </summary>
        public void Quit()
        {
            _application.Quit();
        }

        #endregion // Public methods

        #region Constructor

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="model">The ControllerModel instance that will be coupled with the UI</param>
		/// <param name="bitmapName">Filename of the bitmap to be used (.png only)</param>
		/// <param name="updatealways">Indicates whether or not always to update the UI with an interval of 100 ms</param>
		/// <param name="externaldet">Indicates whether the detector presence will be controller from elsewhere</param>
		/// <param name="usemodelforupdate">Indicates whether to use the ControllerModel instance as a source to keep the UI up to date
		/// If set to false, SetSignalGroupState() will have to be called manually</param>
        public BmpUIMain(ControllerModel model, string bitmapName, bool updatealways = false, bool externaldet = false,
            bool usemodelforupdate = true)
        {
	        _model = model ?? throw new NullReferenceException("The parsed Controller object may not be null.");
            _usemodelforupdate = usemodelforupdate;
	        _externaldet = externaldet;

			_application = new Application();

            _mainForm = new BmpUIForm(bitmapName, _application, model, updatealways, externaldet);
            _mainForm.DetectorPresenceChanged += (o, e) => DetectorPresenceChanged?.Invoke(this, e);
            _mainForm.SimulationChanged += (o, e) => SimulationChanged?.Invoke(this, e);
            _mainForm.SpeedChanged += (o, e) => SpeedChanged?.Invoke(this, e);
            _mainForm.CommandEntered += (o, e) => CommandEntered?.Invoke(this, e);
            _mainForm.Halted += (o, e) => HaltedChanged?.Invoke(this, e);
            _mainForm.StepButtonPressed += (o, e) => StepRequested?.Invoke(this, e);
            _mainForm.Closed += (sender, args) =>
            {
                Closed?.Invoke(this, args);
            };

			// Workaround for exception caused by Eto forms when minimizing the application
            Application.Instance.UnhandledException += (o, e) =>
            {
            };

            #endregion // Constructor
        }
    }
}