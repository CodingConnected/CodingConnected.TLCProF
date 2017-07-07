using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "Module", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class ModuleModel
    {
        #region Properties

        [ModelName]
        [DataMember(IsRequired = true)]
        public string Name { get; set; }

        [DataMember]
        public List<SignalGroupModuleDataModel> SignalGroups { get; private set; }

        #endregion // Properties

        #region Public Methods
        
        public void AddSignalGroup(string sgname)
        {
            SignalGroups.Add(new SignalGroupModuleDataModel(sgname));
        }

        #endregion // Public Methods

        #region Private Methods

        private void OnCreated()
        {
            if (SignalGroups == null)
            {
                SignalGroups = new List<SignalGroupModuleDataModel>();
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        #endregion // Private Methods

        #region Constructor

        public ModuleModel(string name)
        {
            Name = name;
            OnCreated();
        }
        
        #endregion // Constructor
    }
}
