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
    [DataContract(Name = "Block", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class BlockModel : ITLCProFModelBase
    {
        #region Properties

        [ModelName]
        [DataMember(IsRequired = true)]
        public string Name { get; set; }

        [DataMember]
        public List<BlockSignalGroupDataModel> SignalGroups { get; private set; }

        #endregion // Properties

        #region Public Methods
        
        public void AddSignalGroup(string sgname)
        {
            SignalGroups.Add(new BlockSignalGroupDataModel(sgname));
        }

        #endregion // Public Methods

        #region Private Methods

        private void OnCreated()
        {
            if (SignalGroups == null)
            {
                SignalGroups = new List<BlockSignalGroupDataModel>();
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            OnCreated();
        }

        #endregion // Private Methods

        #region ITLCProFModelBase

        public void Reset()
        {
            SignalGroups.ForEach(x => x.Reset());
        }

        #endregion // ITLCProFModelBase

        #region Constructor

        public BlockModel(string name)
        {
            Name = name;
            OnCreated();
        }
        
        #endregion // Constructor
    }
}
