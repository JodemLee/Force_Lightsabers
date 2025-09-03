

using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Lightsaber
{
    public class LordToilLiveDuel : LordToil_DuelToil
    {
        public LordToilLiveDuel(IEnumerable<Pawn> participants, Pawn duelist) : base(participants, duelist)
        {
        }

        public override void UpdateAllDuties()
        {
            LordJob_Duel job = (LordJob_Duel)lord.LordJob;
            if (Data.duelist == null)
            {
                Log.Error("Tried to guard duel with null duelist.");
                return;
            }

            if (job == null)
            {
                Log.Error("Tried to attach Live Duel Toil to non-duel Lord Job.");
                return;
            }

            // Set up the duelist's duty
            if (Data.duelist.mindState != null)
            {
                Data.duelist.mindState.duty = new PawnDuty(LightsaberDefOf.Force_LiveDuel)
                {
                    focus = job.GetDuelistPawn(),
                };
            }

            // Set up the other duelist's duty
            Pawn otherDuelist = job.GetDuelistPawn();
            if (otherDuelist?.mindState != null)
            {
                otherDuelist.mindState.duty = new PawnDuty(LightsaberDefOf.Force_LiveDuel)
                {
                    focus = Data.duelist,
                };
            }

            foreach (Pawn pawn in Data.guards)
            {
                if (pawn?.mindState != null && pawn != Data.duelist && pawn != otherDuelist)
                {
                    pawn.mindState.duty = new PawnDuty(LightsaberDefOf.Force_GuardDuelDuty)
                    {
                        focus = Data.duelist,
                    };
                }
            }
        }
    }
}