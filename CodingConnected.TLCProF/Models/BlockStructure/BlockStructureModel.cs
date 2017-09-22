using System;
using System.Collections.Generic;
using System.Data;
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

                if (!BlockStart && !Waiting)
                {
                    if (sg.SignalGroup.State == SignalGroupStateEnum.Green)
                    {
                        sg.HadPrimaryRealisation = true; continue;
                        
                    }
                    if (!sg.SignalGroup.HasGreenRequest)
                    {
                        sg.SkippedPrimaryRealisation = true; continue;
                    }
                }

                if (sg.SignalGroup.HasGreenRequest && sg.SignalGroup.InterGreenTimes.All(x => x.ConflictingSignalGroup.InternalState != InternalSignalGroupStateEnum.NilRed))
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this, "primary");
                }
            }
        }

        public void UpdatePrimaryAOTRealisations()
        {
            foreach (var sg in AllBlocksSignalGroups)
            {
                // the signalgroup is green, and is in the current block
                if (sg.SignalGroup.State != SignalGroupStateEnum.Green &&
                    CurrentBlock.SignalGroups.All(x => x.SignalGroupName != sg.SignalGroupName))
                {
                    // the signalgroup has a request, is allowed to realise AOT,
                    // and has no conflicts in cyclic green
                    if (sg.SignalGroup.HasGreenRequest && sg.BlocksAheadAllowed == 1 &&
                        sg.SignalGroup.InterGreenTimes.All(x => !x.ConflictingSignalGroup.CyclicGreen))
                    {
                        var i = Blocks.IndexOf(CurrentBlock);
                        i++;
                        var nextBlock = i >= Blocks.Count ? Blocks[0] : Blocks[i];
                        // the signalgroup is in the next block
                        if (nextBlock.SignalGroups.Any(x => x.SignalGroupName == sg.SignalGroupName))
                        {
                            bool ok = true;
                            foreach (var igt in sg.SignalGroup.InterGreenTimes)
                            {
                                var blsg = CurrentBlock.SignalGroups.FirstOrDefault(x => x.SignalGroupName == igt.SignalGroupTo);
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
                                sg.MayRealisePrimaryAhead = true;
                            }
                        }
                    }
                }
            }

            foreach (var sg in AllBlocksSignalGroups.Where(x => x.AheadPrimaryRealisation && 
                x.SignalGroup.InternalState == InternalSignalGroupStateEnum.ExtendGreen))
            {
                if (CurrentBlock.SignalGroups.All(x => x.SignalGroupName != sg.SignalGroupName))
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.ExtendGreen, 0, this, "ahead");
                }
                else
                {
                    sg.MayRealisePrimaryAhead = false;
                    sg.AheadPrimaryRealisation = false;
                }
            }

            foreach (var sg in AllBlocksSignalGroups.Where(x => x.MayRealisePrimaryAhead))
            {
                if (sg.SignalGroup.State == SignalGroupStateEnum.Green)
                {
                    sg.AheadPrimaryRealisation = true;
                }
                if (sg.SignalGroup.State != SignalGroupStateEnum.Green)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this, "ahead");
                }
            }
        }

        public void UpdateAtlernativeRealisations()
        {
            foreach (var sg in AllBlocksSignalGroups)
            {
                // if a signalgroup
                // - has no requests for green
                // - may come AOT or has already had primary realisation
                // - has no alternative space
                // - has a conflict in cyclic green, or with a request for that state, or that may realise AOT
                // - any of the signalgroups in the current block is not done primarily and
                //   - it is this signalgroup or
                //   - is conflicts with this signalgroup
                // do nothing for this signalgroup
                foreach (var igt in sg.SignalGroup.InterGreenTimes)
                {
                    var igtblsg = AllBlocksSignalGroups.FirstOrDefault(x => x.SignalGroupName == igt.SignalGroupTo);
                    if (igtblsg != null && igtblsg.MayRealisePrimaryAhead)
                    {
                        sg.MayRealiseAlternatively = false;
                        continue;
                    }
                }
                if (!sg.SignalGroup.HasGreenRequest || 
                    sg.MayRealisePrimaryAhead || sg.AheadPrimaryRealisation ||
                    sg.AlternativeSpace == 0 ||
                    sg.SignalGroup.InterGreenTimes.Any(x => x.ConflictingSignalGroup.CyclicGreen || x.ConflictingSignalGroup.HasValidGreenStateRequest) ||
                    CurrentBlock.SignalGroups.Any(x => !x.PrimaryRealisationDone &&
                                                       (x.SignalGroup.HasConflictWith(sg.SignalGroupName) ||
                                                        x.SignalGroupName == sg.SignalGroupName)))
                {
                    sg.MayRealiseAlternatively = false;
                    continue;
                }

                // find signalgroups that
                // - do not conflict with this signalgroup
                // - have a primairy realisation
                // - are in cyclic green
                // - are either 
                //   - not in extend green or
                //   - have enough (potentially) remaining time in extend green
                // and set this signalgroup to may realise alternatively if any are found
                if (CurrentBlock.SignalGroups.Any(x => !x.SignalGroup.HasConflictWith(sg.SignalGroupName) &&
                                                       x.HadPrimaryRealisation && !x.AlternativeRealisation &&
                                                       x.SignalGroup.CyclicGreen))
                {
                    var space = 0;

                    var i = Blocks.IndexOf(CurrentBlock);
                    i++;
                    var nextBlock = i >= Blocks.Count ? Blocks[0] : Blocks[i];

                    // loop all signalgroups in the current block that are in cyclic green state
                    foreach (var sg2 in CurrentBlock.SignalGroups.Where(x => x.SignalGroup.CyclicGreen))
                    {
                        // loop all conflicts of those signalgroups
                        foreach (var igt in sg2.SignalGroup.InterGreenTimes)
                        {
                            // check if a conflict is in the next block, has not realised yet, and has a request for green
                            if (nextBlock.SignalGroups.Any(x => x.SignalGroupName == igt.SignalGroupTo &&
                                                                !x.PrimaryRealisationDone &&
                                                                x.SignalGroup.HasGreenRequest))
                            {
                                // check if remaining space is larger than currently recorded space
                                var thisSpace = sg.SignalGroup.InternalState == InternalSignalGroupStateEnum.FixedGreen
                                    ? sg.SignalGroup.GreenFixed.Remaining + sg.SignalGroup.GreenExtend.Maximum + sg.SignalGroup.Amber.Maximum + igt.Timer.Maximum
                                    : sg.SignalGroup.GreenExtend.Running 
                                        ? sg.SignalGroup.GreenExtend.Remaining + sg.SignalGroup.Amber.Maximum + igt.Timer.Maximum
                                        : sg.SignalGroup.GreenExtend.Maximum + sg.SignalGroup.Amber.Maximum + igt.Timer.Maximum;
                                if (thisSpace > space) space = thisSpace;
                            }
                        }
                    }

                    var spaceNeeded = 0;

                    foreach (var igt in sg.SignalGroup.InterGreenTimes)
                    {
                        // check if a conflict is in the next block, has not realised yet, and has a request for green
                        if (nextBlock.SignalGroups.Any(x => x.SignalGroupName == igt.SignalGroupTo &&
                                                            !x.PrimaryRealisationDone &&
                                                            x.SignalGroup.HasGreenRequest))
                        {
                            var thisSpaceNeeded = sg.AlternativeSpace + sg.SignalGroup.Amber.Maximum + igt.Timer.Maximum;
                            if (spaceNeeded > thisSpaceNeeded) spaceNeeded = thisSpaceNeeded;
                        }
                    }

                    if (spaceNeeded <= space)
                    {
                        sg.MayRealiseAlternatively = true;
                    }
                    else
                    {
                        sg.MayRealiseAlternatively = false;
                    }
                }
                else
                {
                    sg.MayRealiseAlternatively = false;
                }
            }

            // for each signalgroup that may realize alternatively and is not green
            // check if a conflicting signalgroup could realise alternatively and
            // has a longer current waiting time
            // if so, the signalgroup may not realise alternatively
            foreach (var sg in AllBlocksSignalGroups)
            {
                if (sg.MayRealiseAlternatively && sg.SignalGroup.State != SignalGroupStateEnum.Green &&
                    AllBlocksSignalGroups.Any(x => x != sg &&
                                                   x.SignalGroup.HasConflictWith(sg.SignalGroupName) &&
                                                   x.MayRealiseAlternatively &&
                                                   sg.SignalGroup.CurrentWaitingTime.Current <=
                                                   x.SignalGroup.CurrentWaitingTime.Current))
                {
                    sg.MayRealiseAlternatively = false;
                }
            }

            foreach (var sg in AllBlocksSignalGroups)
            {
                // if a signalgroup may realise alternatively,
                // or if it has but is still in NIL red,
                // request green
                if (sg.MayRealiseAlternatively && sg.SignalGroup.State == SignalGroupStateEnum.Red ||
                    sg.AlternativeRealisation && sg.SignalGroup.InternalState == InternalSignalGroupStateEnum.NilRed)
                {
                    sg.AlternativeRealisation = true;
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.Green, 0, this, "alternative");
                }
                // if a signalgroup is green alternatively,
                // but may no longer realise alternatively,
                // abort green
                else if (sg.AlternativeRealisation && !sg.MayRealiseAlternatively &&
                         sg.SignalGroup.InterGreenTimes.Any(x => x.ConflictingSignalGroup.HasValidGreenStateRequest) &&
                         sg.SignalGroup.State == SignalGroupStateEnum.Green)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.AbortGreen, 1, this, "alternative");
                }
                else if (sg.AlternativeRealisation && (sg.SignalGroup.Amber.Running || sg.SignalGroup.RedFixed.Running))
                {
                    sg.AlternativeRealisation = false;
                }

                if (sg.AlternativeRealisation && sg.SignalGroup.HasConflict)
                {
                    sg.SignalGroup.AddStateRequest(SignalGroupStateRequestEnum.HoldRed, 1, this, "alternative");
                    sg.AlternativeRealisation = false;
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
                                                   x.SignalGroup.IsInWaitingGreen ||
                                                   x.AlternativeRealisation && x.SignalGroup.InternalState != InternalSignalGroupStateEnum.NilRed))
            {
                foreach (var sg in CurrentBlock.SignalGroups)
                {
                    sg.PrimaryRealisationDone = false;
                }
                var i = Blocks.IndexOf(CurrentBlock);
                i++;
                CurrentBlock = i >= Blocks.Count ? Blocks[0] : Blocks[i];
                BlockStart = true;
                foreach (var sg in CurrentBlock.SignalGroups)
                {
                    if (sg.AlternativeRealisation && sg.SignalGroup.State == SignalGroupStateEnum.Green)
                    {
                        sg.HadPrimaryRealisation = true;
                        sg.AlternativeRealisation = false;
                    }
					/* Reset may realise ahead: at this point, it is irrelevant if it may
					 * - at this point, it is irrelevant if it may: it either has come ahead or not
					 * - leaving this true results in faulty behaviour in combination with waitgreen
					 */
		            sg.MayRealisePrimaryAhead = false;
	            }
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
