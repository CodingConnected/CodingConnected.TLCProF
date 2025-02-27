using System;
using System.Linq;
using System.Runtime.Serialization;
using CodingConnected.TLCProF.Models.Attributes;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "BlockSignalGroupData", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class BlockSignalGroupDataModel : ITLCProFModelBase
    {
        #region Properties

        // State
        [LogWithDump]
        [IgnoreDataMember]
        public bool HadPrimaryRealisation { get; set; }
        [LogWithDump]
        [IgnoreDataMember]
        public bool SkippedPrimaryRealisation { get; set; }
        [LogWithDump]
        [IgnoreDataMember]
        public bool AheadPrimaryRealisation { get; set; }
        [LogWithDump]
        [IgnoreDataMember]
        public bool MayRealisePrimaryAhead { get; set; }
        [LogWithDump]
        [IgnoreDataMember]
        public bool MayRealiseAlternatively { get; set; }
        [LogWithDump]
        [IgnoreDataMember]
        public bool AlternativeRealisation { get; set; }

        [LogWithDump]
        [IgnoreDataMember]
        public bool PrimaryRealisationDone
        {
            get => HadPrimaryRealisation || SkippedPrimaryRealisation || AheadPrimaryRealisation;
            set => HadPrimaryRealisation = SkippedPrimaryRealisation = AheadPrimaryRealisation = value;
        }

        // Settings
        [LogWithDump]
        [DataMember]
        public string SignalGroupName { get; private set; }
        [LogWithDump]
        [DataMember]
        public int BlocksAheadAllowed { get; set; }
        [LogWithDump]
        [DataMember]
        public int AlternativeSpace { get; set; }

        // References
        [IgnoreDataMember]
        public SignalGroupModel SignalGroup { get; set; }

        #endregion // Properties

        #region ITLCProFModelBase

        public void Reset()
        {
            HadPrimaryRealisation = false;
            SkippedPrimaryRealisation = false;
            AheadPrimaryRealisation = false;
        }

        #endregion // ITLCProFModelBase

        #region Public Methods

        #endregion // Public Methods

        #region Private Methods

        #endregion // Private Methods

        #region Constructor

        public BlockSignalGroupDataModel(string sgname)
        {
            SignalGroupName = sgname;
        }

        #endregion // Constructor
    }
}
