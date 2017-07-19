using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using CodingConnected.TLCProF.Models;
using Eto;
using Eto.Forms;

namespace CodingConnected.TLCProF.BmpUI
{
    public class TLCProFMain
    {
        #region

        private bool _initialized;
        private bool _usemodelforupdate;
        private readonly Application _application;
        private readonly TLCProForm _mainForm;
        private readonly ControllerModel _model;

        #endregion // Fields

        #region Properties

        #endregion // Properties

        #region Events

        public event EventHandler<bool> SimulationChanged;
        public event EventHandler<int> SpeedChanged;
        public event EventHandler<BitmapDetector> DetectorPresenceChanged;
        public event EventHandler<EventArgs> Closed;

        #endregion // Events

        #region Public methods

        public void StartUI()
        {
            if (!_initialized)
            {
                throw new NotImplementedException("Not initialized: cannot run.");
            }
            _application.Run(_mainForm);
        }

        public void Initialize()
        {
            foreach (var sg in _model.SignalGroups)
            {
                _mainForm.SetSignalGroupState(sg.Name, sg.InternalState, false);
                sg.InternalStateChanged += (sender, args) =>
                {
                    if(_usemodelforupdate) _mainForm.SetSignalGroupState(sg.Name, sg.InternalState);
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

        public void SetSignalGroupState(string name, InternalSignalGroupStateEnum state, bool update = true)
        {
            _mainForm.SetSignalGroupState(name, state, update);
        }

        public void SetDetectorPresence(string name, bool pres, bool update = true)
        {
            _mainForm.SetDetectorPresence(name, pres, update);
        }

        public void Quit()
        {
            _application.Quit();
        }

        #endregion // Public methods

        #region Constructor

        public TLCProFMain(ControllerModel model, string bitmapName, bool updatealways = false, bool externaldet = false, bool usemodelforupdate = true)
        {
            _model = model;
            _usemodelforupdate = usemodelforupdate;
            _application = new Application();
            _mainForm = new TLCProForm(bitmapName, _application, model, updatealways, externaldet);
            _mainForm.DetectorPresenceChanged += (o, e) => DetectorPresenceChanged?.Invoke(this, e);
            _mainForm.SimulationChanged += (o, e) => SimulationChanged?.Invoke(this, e);
            _mainForm.SpeedChanged += (o, e) => SpeedChanged?.Invoke(this, e);
            _mainForm.Closed += (sender, args) =>
            {
                Closed?.Invoke(this, args);
            };
        }

        #endregion // Constructor
    }
}