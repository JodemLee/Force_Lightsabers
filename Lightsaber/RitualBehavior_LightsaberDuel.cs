using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace Lightsaber
{
    internal class RitualBehavior_LightsaberDuel : RitualBehaviorWorker
    {
        private const int MinSpectatorCells = 20;
        private const int RequiredDuelistCells = 2;
        private const float PreviewRingRadius = 5.9f;

        private Sustainer soundPlaying;

        public static readonly IntRange RadiusRangeSpectators = new IntRange(5, 7);
        public static readonly int RadiusRangeLeader = 3;
        public static readonly int RadiusRangeDuelists = 2;

        public override Sustainer SoundPlaying => soundPlaying;

        public RitualBehavior_LightsaberDuel() { }

        public RitualBehavior_LightsaberDuel(RitualBehaviorDef def) : base(def) { }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            string baseError = base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
            if (baseError != null)
            {
                return baseError;
            }

            if (target.Map == null)
            {
                return "Invalid target map.";
            }

            Room room = target.Cell.GetRoom(target.Map);
            if (room == null)
            {
                return "Target must be indoors.";
            }

            if (CountStandableCellsInRange(target, room, new IntRange(RadiusRangeLeader, RadiusRangeLeader)) < 1)
            {
                return "CantStartNotEnoughSpaceDuelSpeaker".Translate(RadiusRangeLeader.Named("MINRADIUS"));
            }

            if (CountStandableCellsInRange(target, room, new IntRange(RadiusRangeDuelists, RadiusRangeDuelists)) < RequiredDuelistCells)
            {
                return "CantStartNotEnoughSpaceDuelDuelists".Translate(RadiusRangeSpectators.Average.Named("MINRADIUS"), RequiredDuelistCells);
            }

            if (CountStandableCellsInRange(target, room, RadiusRangeSpectators) < MinSpectatorCells)
            {
                return "CantStartNotEnoughSpaceDuelSpectators".Translate(RadiusRangeSpectators.Average.Named("MINRADIUS"), MinSpectatorCells);
            }

            return null;
        }

        private int CountStandableCellsInRange(TargetInfo target, Room room, IntRange range)
        {
            int count = 0;
            foreach (IntVec3 cell in CellRect.CenteredOn(target.Cell, range.max))
            {
                float distance = cell.DistanceTo(target.Cell);
                if (distance >= range.min &&
                    distance <= range.max &&
                    cell.Standable(target.Map) &&
                    cell.GetRoom(target.Map) == room)
                {
                    count++;
                }
            }
            return count;
        }

        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            return new LordJob_Ritual_LightsaberDuel(target, ritual, obligation, def.stages, assignments, organizer);
        }

        public override void Tick(LordJob_Ritual ritual)
        {
            base.Tick(ritual);
            if (ritual?.StageIndex == 4)
            {
                if (soundPlaying == null || soundPlaying.Ended)
                {
                    TargetInfo selectedTarget = ritual.selectedTarget;
                    soundPlaying = SoundDefOf.DuelMusic.TrySpawnSustainer(
                        SoundInfo.InMap(new TargetInfo(selectedTarget.Cell, selectedTarget.Map),
                        MaintenanceType.PerTick));
                }
                soundPlaying?.Maintain();
            }
        }

        public override void Cleanup(LordJob_Ritual ritual)
        {
            soundPlaying?.End();
            soundPlaying = null;
        }

        public override void DrawPreviewOnTarget(TargetInfo targetInfo)
        {
            base.DrawPreviewOnTarget(targetInfo);
            GenDraw.DrawRadiusRing(targetInfo.CenterCell, PreviewRingRadius);
        }
    }
}
