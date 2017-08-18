using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Documents;

namespace CodingConnected.TLCProF.Models
{
    [Serializable]
    [DataContract(Name = "BlockStructure", Namespace = "http://www.codingconnected.eu/TLC_PROF.Models")]
    public class BlockStructureModel : ITLCProFModelBase
    {
        #region Fields

        #endregion // Fields

        #region Properties

        // State
        [IgnoreDataMember]
        public bool BlockStart { get; private set; }
        [IgnoreDataMember]
        public bool Waiting { get; private set; }

        // References
        [IgnoreDataMember]
        public ControllerModel Controller { get; set; }
        [IgnoreDataMember]
        public BlockModel CurrentBlock { get; set; }
        [IgnoreDataMember]
        public BlockModel WaitingBlock { get; set; }
        [IgnoreDataMember]
        public List<BlockSignalGroupDataModel> AllBlocksSignalGroups { get; set; }

        // Settings
        [DataMember]
        public List<BlockModel> Blocks { get; private set; }
        [DataMember(IsRequired = true)]
        public string WaitingBlockName { get; set; }

        #endregion // Properties

        #region Public Methods

        public void UpdatePrimaryRealisations()
        {
            // Primary realisations: main
            foreach (var sg in CurrentBlock.SignalGroups)
            {
                if(sg.PrimaryRealisationDone)
                {
                    continue;
                }

                if(!BlockStart && !Waiting &&
                    (sg.SignalGroup.State == SignalGroupStateEnum.Green || !sg.SignalGroup.HasGreenRequest))
                {
                    sg.HadPrimaryRealisation = true;
                    continue;
                }

                if (sg.SignalGroup.HasGreenRequest)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this);
                }
            }
        }

        public void UpdatePrimaryAOTRealisations()
        {
            foreach (var sg in AllBlocksSignalGroups)
            {
                sg.AheadPrimaryRealisation = false;

                if(CurrentBlock.SignalGroups.All(x => x.SignalGroupName != sg.SignalGroupName))
                {
                    if (sg.SignalGroup.HasGreenRequest && sg.BlocksAheadAllowed == 1 &&
                        sg.SignalGroup.InterGreenTimes.All(x => !x.ConflictingSignalGroup.CyclicGreen))
                    {
                        var j3 = Blocks.IndexOf(CurrentBlock);
                        j3++;
                        var nextBlock = j3 >= Blocks.Count ? Blocks[0] : Blocks[j3];
                        if (nextBlock.SignalGroups.Any(x => x.SignalGroupName == sg.SignalGroupName))
                        {
                            bool ok = true;
                            foreach (var igt in sg.SignalGroup.InterGreenTimes)
                            {
                                var blsg =
                                    CurrentBlock.SignalGroups.FirstOrDefault(x => x.SignalGroupName == igt.SignalGroupTo);
                                if (blsg != null)
                                {
                                    if (!blsg.PrimaryRealisationDone)
                                    {
                                        ok = false;
                                        break;
                                    }
                                }
                            }
                            if (ok)
                            {
                                sg.AheadPrimaryRealisation = true;
                            }
                        }
                    }
                }
            }

            foreach (var sg in AllBlocksSignalGroups.Where(x => x.AheadPrimaryRealisation))
            {
                if (sg.SignalGroup.State != SignalGroupStateEnum.Green)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this);
                }
                else if (CurrentBlock.SignalGroups.All(x => x.SignalGroupName != sg.SignalGroupName))
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.FreeExtendGreen, 0, this);
                }
            }
        }

        public void MoveBlockStructure()
        {
            // Don't move if the waiting block is active, and there are no unhandled requests
            if (CurrentBlock == WaitingBlock && 
                AllBlocksSignalGroups.All(x => x.SignalGroup.State == SignalGroupStateEnum.Green || !x.SignalGroup.HasGreenRequest ||
                                               CurrentBlock.SignalGroups.Any(x2 => x2.SignalGroupName == x.SignalGroupName && !x.PrimaryRealisationDone)))
            {
                Waiting = true;
                return;
            }

            // Move on if all phases have had or skipped their primary realisation
            Waiting = false;
            BlockStart = false;
            if (CurrentBlock.SignalGroups.All(x => !x.SignalGroup.CyclicGreen &&
                                                   (x.PrimaryRealisationDone || !x.SignalGroup.HasGreenRequest) ||
                                                   x.SignalGroup.IsInWaitingGreen))
            {
                foreach (var sg in CurrentBlock.SignalGroups)
                {
                    sg.PrimaryRealisationDone = false;
                }
                var i = Blocks.IndexOf(CurrentBlock);
                i++;
                CurrentBlock = i >= Blocks.Count ? Blocks[0] : Blocks[i];
                BlockStart = true;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private void OnCreated()
        {
            if (Blocks == null)
            {
                Blocks = new List<BlockModel>();
            }
            BlockStart = true;
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
            CurrentBlock = WaitingBlock;
            Blocks.ForEach(x => x.Reset());
        }

        #endregion // ITLCProFModelBase

        #region Contructors

        public BlockStructureModel(ControllerModel controller)
        {
            Controller = controller;
            OnCreated();
        }

        #endregion // Contructors
    }
}
