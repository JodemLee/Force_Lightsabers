using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

//TODO
namespace Lightsaber
{
	public class JobDriver_AwaitDuel : JobDriver
	{
		public static int DuelRadius = 8;
		
		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil stand = Toils_General.Wait(int.MaxValue);
			stand.tickIntervalAction = delta =>
			{
				pawn.rotationTracker.FaceCell(NearestEnemyOrMapCenter());
				pawn.GainComfortFromCellIfPossible(delta);
				Pawn actor = stand.actor;
				
				
				if (!actor.IsHashIntervalTick(100))
					return;
				actor.jobs.CheckForJobOverride();
			};
			stand.defaultCompleteMode = ToilCompleteMode.Never;
			stand.handlingFacing = true;
			yield return stand;
		}

		private IntVec3 NearestEnemyOrMapCenter()
		{
			Pawn nearestEnemy = null;
			int maxDistance = int.MaxValue;
			foreach (Pawn pawn1 in pawn.Map.mapPawns.AllHumanlikeSpawned)
			{
				if (pawn.HostileTo(pawn1))
				{
					int distance = pawn.PositionHeld.DistanceToSquared(pawn1.PositionHeld);
					if (distance < maxDistance)
					{
						nearestEnemy = pawn1;
						maxDistance = distance;
					}
				}
			}

			if (nearestEnemy == null)
			{
				return pawn.Map.Center;
			}

			return nearestEnemy.PositionHeld;
		}
	}
}