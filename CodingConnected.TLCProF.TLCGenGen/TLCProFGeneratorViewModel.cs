using System.IO;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using TLCGen.Messaging.Messages;
using TLCGen.Messaging.Requests;

namespace CodingConnected.TLCProF.TLCGenGen
{
    public class TLCProFGeneratorViewModel : ViewModelBase
    {
        #region Fields

        private readonly TLCProFGeneratorPlugin _plugin;

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Commands

        RelayCommand _generateCodeCommand;
        public ICommand GenerateCodeCommand =>
            _generateCodeCommand ?? (_generateCodeCommand =
                new RelayCommand(GenerateCodeCommand_Executed, GenerateCodeCommand_CanExecute));

        #endregion // Commands

        #region Command Functionality

        private void GenerateCodeCommand_Executed()
        {
            var prepreq = new PrepareForGenerationRequest();
            MessengerInstance.Send(prepreq);
            var s = TLCGen.Integrity.TLCGenIntegrityChecker.IsControllerDataOK(_plugin.Controller);
            if (s == null)
            {
                TLCProFCodeGenerator.GenerateXml(_plugin.Controller, Path.GetDirectoryName(_plugin.ControllerFileName));
                MessengerInstance.Send(new ControllerCodeGeneratedMessage());
            }
            else
            {
                MessageBox.Show(s, "Fout in conflictmatrix");
            }
        }

        private bool GenerateCodeCommand_CanExecute()
        {
            return _plugin.Controller?.Fasen != null &&
                   _plugin.Controller.Fasen.Count > 0 &&
                   !string.IsNullOrWhiteSpace(_plugin.ControllerFileName);
        }

        #endregion // Command Functionality


        #region Constructor

        public TLCProFGeneratorViewModel(TLCProFGeneratorPlugin plugin)
        {
            _plugin = plugin;
        }

        #endregion // Constructor
    }
}