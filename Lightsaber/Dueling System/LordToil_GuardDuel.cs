using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
	public class LordToil_GuardDuel : LordToil_DuelToil
	{

		public LordToil_GuardDuel(IEnumerable<Pawn> participants, Pawn duelist) : base(participants, duelist)
		{
		}
		

		public override void UpdateAllDuties()
		{
			if (Data.duelist == null)
			{
				Log.Error("Tried to guard duel with null duelist.");
			}
			foreach (Pawn pawn in Data.guards)
			{
				if (pawn?.mindState != null)
					pawn.mindState.duty = new PawnDuty(LightsaberDefOf.Force_GuardDuelDuty, Data.duelist);
			}
			
			
			
			if (Data.duelist.mindState != null)
				Data.duelist.mindState.duty = new PawnDuty(LightsaberDefOf.Force_PrepareToDuelDuty, Data.duelist.PositionHeld);
		}
		
		

	}
}