using System;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using TLCGen.Messaging.Messages;
using TLCGen.Models;
using TLCGen.Plugins;

namespace CodingConnected.TLCProF.TLCGenGen
{
    [TLCGenPlugin(TLCGenPluginElems.Generator | TLCGenPluginElems.PlugMessaging)]
    public class TLCProFGeneratorPlugin : ITLCGenGenerator, ITLCGenPlugMessaging
    {
        #region Properties

        public string ControllerFileName { get; private set; }
        private TLCProFGeneratorViewModel _pluginViewModel;

        #endregion // Properties

        #region ITLCGenGenerator

        private TLCProFGeneratorViewModel PluginViewModel =>
            _pluginViewModel ?? (_pluginViewModel = new TLCProFGeneratorViewModel(this));

        public string GetPluginName()
        {
            return "TLC-ProF";
        }

        public ControllerModel Controller { get; set; }

        public bool CanGenerateController()
        {
            return _pluginViewModel.GenerateCodeCommand.CanExecute(null);
        }

        public void GenerateController()
        {
            _pluginViewModel.GenerateCodeCommand.Execute(null);
        }

        public string GetGeneratorName()
        {
            return "TLC-ProF";
        }

        public string GetGeneratorVersion()
        {
            return "0.0.1 (pre-alfa)";
        }

        public UserControl GeneratorView { get; }

        #endregion // ITLCGenGenerator

        #region ITLCGenPlugMessaging

        public void UpdateTLCGenMessaging()
        {
            Messenger.Default.Register(this, new Action<ControllerFileNameChangedMessage>(OnControllerFileNameChanged));
        }

        #endregion // ITLCGenPlugMessaging

        #region TLCGen Events

        private void OnControllerFileNameChanged(TLCGen.Messaging.Messages.ControllerFileNameChangedMessage msg)
        {
            ControllerFileName = msg.NewFileName;
        }

        #endregion // TLCGen Events

        public TLCProFGeneratorPlugin()
        {
            GeneratorView = new TLCProFGeneratorView
            {
                DataContext = PluginViewModel
            };
        }
    }
}
