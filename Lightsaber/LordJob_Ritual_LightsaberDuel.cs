using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    internal class LordJob_Ritual_LightsaberDuel : LordJob_Ritual_Duel
    {
        private Dictionary<Pawn, int> totalPresenceTmp = new Dictionary<Pawn, int>();

        public LordJob_Ritual_LightsaberDuel()
        {
        }

        public LordJob_Ritual_LightsaberDuel(TargetInfo selectedTarget, Precept_Ritual ritual, RitualObligation obligation, List<RitualStage> allStages, RitualRoleAssignments assignments, Pawn organizer = null)
            : base(selectedTarget, ritual, obligation, allStages, assignments, organizer)
        {
            foreach (RitualRole item2 in assignments.AllRolesForReading)
            {
                if (item2 != null && item2.id.Contains("duelist"))
                {
                    Pawn item = assignments.FirstAssignedPawn(item2);
                    duelists.Add(item);
                    pawnsDeathIgnored.Add(item);
                }
            }
        }


        public override void ApplyOutcome(float progress, bool showFinishedMessage = true, bool showFailedMessage = true, bool cancelled = false)
        {
            if (ended)
            {
                return;
            }
            ended = true;
            if (RitualFinished(progress, cancelled))
            {
                totalPresenceTmp.Clear();
                foreach (LordToil_Ritual toil in toils)
                {
                    foreach (KeyValuePair<Pawn, int> presentForTick in toil.Data.presentForTicks)
                    {
                        if (presentForTick.Key != null && !presentForTick.Key.Dead)
                        {
                            if (!totalPresenceTmp.ContainsKey(presentForTick.Key))
                            {
                                totalPresenceTmp.Add(presentForTick.Key, presentForTick.Value);
                            }
                            else
                            {
                                totalPresenceTmp[presentForTick.Key] += presentForTick.Value;
                            }
                        }
                    }
                }
                float tickScale = ticksPassedWithProgress / (float)ticksPassed;
                float targetDuration = ((durationTicks > 0) ? ((float)durationTicks) : ticksPassedWithProgress);
                totalPresenceTmp.RemoveAll((KeyValuePair<Pawn, int> tp) => targetDuration * (float)tp.Value < tickScale / 2f);
                if (totalPresenceTmp.Count > 0 || ritual.outcomeEffect.def.allowOutcomeWithNoAttendance)
                {
                    AddParticipantThoughts();
                    try
                    {
                        ritual.outcomeEffect.Apply(progress, totalPresenceTmp, this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error while applying ritual outcome effect: " + ex);
                    }
                    if (obligation != null)
                    {
                        ritual.RemoveObligation(obligation, completed: true);
                    }
                    if (showFinishedMessage)
                    {
                        Messages.Message("RitualFinished".Translate(ritual.Label), new TargetInfo(spot, base.Map), MessageTypeDefOf.SilentInput);
                    }
                }
                else
                {
                    Messages.Message("RitualNobodyAttended".Translate(ritual.Label), new TargetInfo(spot, base.Map), MessageTypeDefOf.NegativeEvent);
                }
                totalPresenceTmp.Clear();
                if (Ritual != null && Ritual.ideo != null)
                {
                    foreach (Precept item in Ritual.ideo.PreceptsListForReading)
                    {
                        if (!(item is Precept_Ritual precept_Ritual) || precept_Ritual.obligationTriggers.NullOrEmpty())
                        {
                            continue;
                        }
                        foreach (RitualObligationTrigger obligationTrigger in precept_Ritual.obligationTriggers)
                        {
                            obligationTrigger.Notify_RitualExecuted(this);
                        }
                    }
                }
                ritual.lastFinishedTick = GenTicks.TicksGame;
            }
            else
            {
                if (showFailedMessage)
                {
                    Messages.Message("RitualCalledOff".Translate(ritual.Label).CapitalizeFirst(), new TargetInfo(spot, base.Map), MessageTypeDefOf.NegativeEvent);
                }
                try
                {
                    if (ritual.outcomeEffect.ApplyOnFailure)
                    {
                        ritual.outcomeEffect.Apply(progress, totalPresenceTmp, this);
                    }
                }
                catch (Exception ex2)
                {
                    Log.Error("Error while applying ritual outcome effect: " + ex2);
                }
            }
            ritual.outcomeEffect?.ResetCompDatas();
            base.Map.lordManager.RemoveLord(lord);
        }

        private void AddRelicInRoomThought()
        {
            if (!ModsConfig.IdeologyActive || Ritual?.ideo == null || selectedTarget.Map == null)
            {
                return;
            }
            Room room = selectedTarget.Cell.GetRoom(selectedTarget.Map);
            if (room == null || room.TouchesMapEdge)
            {
                return;
            }
            int num = 0;
            string str = string.Empty;
            foreach (Thing item in room.ContainedThings(ThingDefOf.Reliquary))
            {
                CompRelicContainer compRelicContainer = item.TryGetComp<CompRelicContainer>();
                if (compRelicContainer == null)
                {
                    continue;
                }
                Precept_ThingStyle precept_ThingStyle = compRelicContainer.ContainedThing?.TryGetComp<CompStyleable>()?.SourcePrecept;
                if (precept_ThingStyle != null && precept_ThingStyle.ideo == Ritual.ideo)
                {
                    if (num == 0)
                    {
                        str = compRelicContainer.ContainedThing.Label;
                    }
                    num++;
                }
            }
            if (num <= 0)
            {
                return;
            }
            foreach (KeyValuePair<Pawn, int> item2 in totalPresenceTmp)
            {
                if (item2.Key.Ideo == Ritual.ideo)
                {
                    Thought_RelicAtRitual thought_RelicAtRitual = (Thought_RelicAtRitual)ThoughtMaker.MakeThought(ThoughtDefOf.RelicAtRitual, Mathf.Min(num, ThoughtDefOf.RelicAtRitual.stages.Count) - 1);
                    thought_RelicAtRitual.relicName = Find.ActiveLanguageWorker.WithDefiniteArticle(str, Gender.None);
                    item2.Key.needs.mood.thoughts.memories.TryGainMemory(thought_RelicAtRitual);
                }
            }
        }

        private void AddParticipantThoughts()
        {
            if (!ModsConfig.IdeologyActive || Ritual?.ideo == null || Ritual.def.mergeRitualGizmosFromAllIdeos)
            {
                return;
            }
            foreach (KeyValuePair<Pawn, int> item in totalPresenceTmp)
            {
                if (item.Key.Ideo != Ritual.ideo)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.ParticipatedInOthersRitual, item.Key.Named(HistoryEventArgsNames.Doer)));
                }
            }
            AddRelicInRoomThought();
        }

    }
}
