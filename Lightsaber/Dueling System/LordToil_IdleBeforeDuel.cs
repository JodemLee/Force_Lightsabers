using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
	public class LordToil_IdleBeforeDuel: LordToil_DuelToil
	{
		
		
		public LordToil_IdleBeforeDuel(IEnumerable<Pawn> participants, Pawn duelist) : base(participants, duelist)
		{
		}
		
		public override void UpdateAllDuties()
		{
			if (Data.duelist == null)
			{
				Log.Error("Tried to guard duel with null duelist.");
				return;
			}
			
			foreach (Pawn pawn in Data.guards)
			{
				if (pawn?.mindState != null)
					pawn.mindState.duty = new PawnDuty(DutyDefOf.WanderClose, Data.duelist.PositionHeld);
			
			}
			
			if (Data.duelist.mindState != null)
				Data.duelist.mindState.duty = new PawnDuty(DutyDefOf.WanderClose, Data.duelist.PositionHeld);
		}
	}
}