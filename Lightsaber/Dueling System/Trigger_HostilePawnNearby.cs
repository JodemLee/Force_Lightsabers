using RimWorld;
using Verse;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
	public class Trigger_HostilePawnNearby: Trigger
	{
		private const int CheckInterval = 40;
		
		public override bool ActivateOn(Lord lord, TriggerSignal signal)
		{
			return signal.type == TriggerSignalType.Tick && Find.TickManager.TicksGame % CheckInterval == 0 &&
			       AnyHostileNear(lord);
		}
			
		private static int SquaredEnemyRange = 144;
		
		private bool AnyHostileNear(Lord lord)
		{
			foreach (Pawn pawn1 in lord.Map.mapPawns.AllHumanlikeSpawned)
			{
				if (pawn1 != null)
				{
					if (pawn1.HostileTo(lord.faction))
					{
						foreach (Pawn lordPawn in lord.ownedPawns)
						{
							if (lordPawn.PositionHeld.DistanceToSquared(pawn1.PositionHeld) < SquaredEnemyRange)
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}
	}
}