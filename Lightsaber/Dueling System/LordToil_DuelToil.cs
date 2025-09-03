using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Lightsaber
{
    public abstract class LordToil_DuelToil : LordToil
    {
        protected LordToilData_GuardDuel Data => (LordToilData_GuardDuel)data;
        private static int SquaredEnemyRange = 144;

        public LordToil_DuelToil(IEnumerable<Pawn> participants, Pawn duelist)
        {
            data = new LordToilData_GuardDuel();
            Data.guards = participants.ToList();
            Data.duelist = duelist;
        }

        protected bool IsValidDuelTarget(Pawn pawn, Pawn potentialTarget)
        {
            LordJob_Duel lordJob = lord?.LordJob as LordJob_Duel;
            if (lordJob == null) return false;

            return potentialTarget == lordJob.GetDuelistPawn() || potentialTarget == Data.duelist;
        }

        protected bool AnyEnemyNear(Pawn pawn)
        {
            if (pawn.Map == null || lord?.LordJob == null)
            {
                return false;
            }

            LordJob_Duel lordJob = lord.LordJob as LordJob_Duel;
            if (lordJob == null) return false;

            Pawn otherDuelist = (pawn == Data.duelist) ? lordJob.GetDuelistPawn() : Data.duelist;

            if (otherDuelist != null && otherDuelist.Spawned && !otherDuelist.Dead && IsValidDuelTarget(pawn, otherDuelist))
            {
                return pawn.PositionHeld.DistanceToSquared(otherDuelist.PositionHeld) < SquaredEnemyRange;
            }

            return false;
        }
    }
}