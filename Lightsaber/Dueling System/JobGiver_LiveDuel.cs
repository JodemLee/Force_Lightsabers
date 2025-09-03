using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
	public class JobGiver_LiveDuel : JobGiver_AIFightEnemies
	{
		public const float MinDistOpponentWhenMoving = 1.9f;
		public const float MaxFightMoveDist = 3.1f;

		protected override bool DisableAbilityVerbs => true;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!(pawn.GetLord()?.LordJob is LordJob_Duel lordJob))
				return null;
			lordJob.StartDuelIfNotStartedYet();
			if (lordJob.CurrentDuelStage == LiveDuelBehaviorStage.Attack)
				return base.MeleeAttackJob(pawn , lordJob.Opponent(pawn));
			if (lordJob.CurrentDuelStage == LiveDuelBehaviorStage.BackOff)
			{
				Job backOffJob = JobMaker.MakeJob(JobDefOf.Goto, GetMoveTarget(pawn, lordJob), (LocalTargetInfo) (Thing) lordJob.Opponent(pawn));
				backOffJob.checkOverrideOnExpire = true;
				backOffJob.expiryInterval = 240;
				backOffJob.collideWithPawns = true;
				backOffJob.locomotionUrgency = LocomotionUrgency.Amble;
				return backOffJob;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Goto, GetMoveTarget(pawn, lordJob), (LocalTargetInfo) (Thing) lordJob.Opponent(pawn));
			job.checkOverrideOnExpire = true;
			job.expiryInterval = 40;
			job.collideWithPawns = true;
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			return job;
		}
		

		private LocalTargetInfo GetMoveTarget(Pawn pawn, LordJob_Duel duel)
		{
			Pawn opponent = duel.Opponent(pawn);
			return RCellFinder.RandomWanderDestFor(pawn, duel.point, MaxFightMoveDist, (p, c, r) =>
			{
				if (c == pawn.Position || !c.Standable(p.Map) ||
				    !p.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly) ||
				    c.DistanceTo(duel.point) > MaxFightMoveDist)
					return false;
				IntVec3 a1 = opponent.CurJob?.def == JobDefOf.Goto ? opponent.CurJob.targetA.Cell : IntVec3.Invalid;
				if (c.DistanceTo(opponent.Position) < MinDistOpponentWhenMoving ||
				    a1 != IntVec3.Invalid && a1.DistanceTo(c) < MinDistOpponentWhenMoving)
					return false;
				PawnPath path = pawn.Map.pathFinder.FindPathNow(pawn.Position, c, pawn);
				try
				{
					foreach (IntVec3 a2 in path.NodesReversed)
					{
						if (a2.DistanceTo(opponent.Position) < MinDistOpponentWhenMoving ||
						    a2.DistanceTo(duel.point) > MaxFightMoveDist)
							return false;
					}

					if (a1 != IntVec3.Invalid)
					{
						if (opponent.pather.curPath != null)
						{
							foreach (IntVec3 a3 in opponent.pather.curPath.NodesReversed)
							{
								if (a3.DistanceTo(pawn.Position) < MinDistOpponentWhenMoving ||
								    a3.DistanceTo(duel.point) > MaxFightMoveDist)
									return false;
								foreach (IntVec3 b in path.NodesReversed)
								{
									if (a3.DistanceTo(b) < MinDistOpponentWhenMoving)
										return false;
								}
							}
						}
					}
				}
				finally
				{
					path.ReleaseToPool();
				}

				return true;
			}, Danger.Deadly);
		}

		protected override void UpdateEnemyTarget(Pawn pawn)
		{
			Pawn pawn1 = ((LordJob_Duel) pawn.GetLord().LordJob).Opponent(pawn);
			if (pawn1 == null || pawn1.Dead)
				pawn.mindState.enemyTarget = null;
			else
				pawn.mindState.enemyTarget = pawn1;
		}

		protected override Job MeleeAttackJob(Pawn pawn, Thing enemyTarget)
		{
			Job job = base.MeleeAttackJob(pawn, enemyTarget);
			job.killIncappedTarget = true;
			return job;
		}
	}
}