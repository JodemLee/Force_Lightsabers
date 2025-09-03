using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Lightsaber
{
    public class RaidStrategyWorker_Duel : RaidStrategyWorker
    {
        public override float SelectionWeight(Map map, float basePoints)
        {
            // Only allow this strategy if there's at least one valid duelist on the map
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (IsValidDuelist(pawn))
                {
                    return base.SelectionWeight(map, basePoints);
                }
            }
            return 0f;
        }

        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            Pawn champion = pawns[0];
            List<Pawn> guards = pawns.Count > 1 ? pawns.GetRange(1, pawns.Count - 1) : new List<Pawn>();

            return new LordJob_Duel(champion, guards);
        }

        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            if (parms.faction == null || !parms.faction.def.humanlikeFaction)
                return false;

            if (parms.pawnCount < 1)
                return false;

            return base.CanUseWith(parms, groupKind);
        }

        public override float MinimumPoints(Faction faction, PawnGroupKindDef groupKind)
        {
            return faction.def.MinPointsToGeneratePawnGroup(groupKind);
        }

        public override bool CanUsePawnGenOption(float pointsTotal, PawnGenOption g, List<PawnGenOptionWithXenotype> chosenGroups, Faction faction = null)
        {
            if (!base.CanUsePawnGenOption(pointsTotal, g, chosenGroups, faction))
                return false;
            if (chosenGroups == null || chosenGroups.Count == 0)
            {
                return g.kind.RaceProps.Humanlike;
            }
            return true;
        }

        public override bool CanUsePawn(float pointsTotal, Pawn p, List<Pawn> otherPawns)
        {
            if (otherPawns.Count == 0 && !p.RaceProps.Humanlike)
                return false;

            return base.CanUsePawn(pointsTotal, p, otherPawns);
        }

        private bool IsValidDuelist(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer || pawn.IsSlave || pawn.IsPrisoner)
                return false;

            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
                return false;

            if (pawn.Dead || pawn.Downed)
                return false;

            return true;
        }

        public override void MakeLords(IncidentParms parms, List<Pawn> pawns)
        {
            if (pawns.Count == 0) return;

            Map map = (Map)parms.target;
            Pawn champion = pawns[0];
            List<Pawn> guards = pawns.Count > 1 ? pawns.GetRange(1, pawns.Count - 1) : new List<Pawn>();

            Find.LetterStack.ReceiveLetter(
                "Duel Challenge".Translate(),
                "Duel Challenge".Translate(),
                LightsaberDefOf.Force_DuelChallenge,
                new LookTargets(champion),
                parms.faction
            );
            Lord lord = LordMaker.MakeNewLord(parms.faction, new LordJob_Duel(champion, guards), map);

            foreach (Pawn p in pawns)
            {
                lord.AddPawn(p);
            }

            lord.inSignalLeave = parms.inSignalEnd;
            QuestUtility.AddQuestTag(lord, parms.questTag);
        }
    }
}