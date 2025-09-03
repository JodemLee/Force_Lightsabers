using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.PsychicRitualRoleDef;

namespace Lightsaber
{
    internal class JobGiver_LightsaberDuel : JobGiver_Duel
    {
        protected override bool DisableAbilityVerbs => true;
        public new const float MinDistOpponentWhenMoving = 1.9f;
        public new const float MaxFightMoveDist = 2.3f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn.GetLord()?.LordJob is LordJob_Ritual_LightsaberDuel duel))
                return null;

            duel.StartDuelIfNotStartedYet();

            if (duel.CurrentDuelStage == DuelBehaviorStage.Attack)
                return base.TryGiveJob(pawn);

            LocalTargetInfo moveTarget = GetMoveTarget(pawn, duel);
            return CreateDuelMovementJob(pawn, moveTarget, duel.Opponent(pawn));
        }

        private Job CreateDuelMovementJob(Pawn pawn, LocalTargetInfo target, Pawn opponent)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, target, opponent);
            job.checkOverrideOnExpire = true;
            job.expiryInterval = Rand.Range(30, 60);

            // 50% sprint, 50% walk
            job.locomotionUrgency = Rand.Value < 0.5f
                ? LocomotionUrgency.Sprint
                : LocomotionUrgency.Walk;

            return job;
        }

        private LocalTargetInfo GetMoveTarget(Pawn pawn, LordJob_Ritual_LightsaberDuel duel)
        {
            if (duel.selectedTarget == null || !duel.selectedTarget.IsValid)
                return pawn.Position;

            Pawn opponent = duel.Opponent(pawn);
            if (opponent == null || opponent.Dead)
                return pawn.Position;

            float rand = Rand.Value;
            IntVec3 baseTarget = duel.selectedTarget.Cell;

            if (rand < 0.2f)
            {
                IntVec3 target = GenRadial.RadialCellsAround(opponent.Position, MinDistOpponentWhenMoving * 0.8f, false)
                    .Where(c => IsValidDuelMoveCell(pawn, c, opponent, baseTarget, opponent.Position))
                    .RandomElementWithFallback(IntVec3.Invalid);

                if (target.IsValid) return target;
            }
            else if (rand < 0.3f) // Retreat
            {
                // Calculate direction away from opponent (non-normalized)
                IntVec3 retreatDir = (pawn.Position - opponent.Position);

                // Avoid division by zero if pawns are overlapping (unlikely in duels)
                if (retreatDir.LengthHorizontalSquared > 0)
                {
                    retreatDir = new IntVec3(
                        retreatDir.x > 0 ? 1 : (retreatDir.x < 0 ? -1 : 0),
                        retreatDir.y > 0 ? 1 : (retreatDir.y < 0 ? -1 : 0),
                        retreatDir.z > 0 ? 1 : (retreatDir.z < 0 ? -1 : 0)
                    );

                    // Random retreat distance (2-4 tiles)
                    IntVec3 target = pawn.Position + (retreatDir * Rand.Range(2, 4));

                    if (IsValidDuelMoveCell(pawn, target, opponent, baseTarget, opponent.Position))
                        return target;
                }
            }

            // Default: Circle opponent
            return RCellFinder.RandomWanderDestFor(
                pawn, baseTarget, MaxFightMoveDist,
                (p, c, r) => IsValidDuelMoveCell(p, c, opponent, baseTarget, opponent.Position),
                Danger.Deadly
            );
        }

        private bool IsValidDuelMoveCell(Pawn pawn, IntVec3 cell, Pawn opponent, IntVec3 duelCenter, IntVec3 opponentTarget)
        {
            // Early exit if cell is invalid
            if (cell == pawn.Position ||
                !cell.Standable(pawn.Map) ||
                !pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly) ||
                cell.DistanceTo(duelCenter) > MaxFightMoveDist)
            {
                return false;
            }

            // Avoid moving too close to the opponent
            if (cell.DistanceTo(opponent.Position) < MinDistOpponentWhenMoving ||
                (opponentTarget.IsValid && cell.DistanceTo(opponentTarget) < MinDistOpponentWhenMoving))
            {
                return false;
            }

            // Validate path
            using (PawnPath path = pawn.Map.pathFinder.FindPathNow(pawn.Position, cell, pawn))
            {
                foreach (IntVec3 node in path.NodesReversed)
                {
                    if (node.DistanceTo(opponent.Position) < MinDistOpponentWhenMoving ||
                        node.DistanceTo(duelCenter) > MaxFightMoveDist)
                    {
                        return false;
                    }
                }

                // Check opponent's path if moving
                if (opponentTarget.IsValid && opponent.pather.curPath != null)
                {
                    foreach (IntVec3 opponentNode in opponent.pather.curPath.NodesReversed)
                    {
                        if (opponentNode.DistanceTo(pawn.Position) < MinDistOpponentWhenMoving ||
                            opponentNode.DistanceTo(duelCenter) > MaxFightMoveDist)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        protected override void UpdateEnemyTarget(Pawn pawn)
        {
            if (!(pawn.GetLord()?.LordJob is LordJob_Ritual_LightsaberDuel duel))
            {
                pawn.mindState.enemyTarget = null;
                return;
            }

            Pawn opponent = duel.Opponent(pawn);
            pawn.mindState.enemyTarget = (opponent != null && !opponent.Dead) ? opponent : null;
        }

        protected override Job MeleeAttackJob(Pawn pawn, Thing enemyTarget)
        {
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, enemyTarget);
            job.expiryInterval = ExpiryInterval_Melee.RandomInRange;
            job.checkOverrideOnExpire = true;
            job.expireRequiresEnemiesNearby = true;
            job.killIncappedTarget = false;
            return job;
        }
    }
}