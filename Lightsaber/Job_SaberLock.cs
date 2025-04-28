using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Lightsaber
{
    public class JobDriver_SaberLock : JobDriver_AttackMelee
    {
        private const int LockDurationTicks = 240; // 2 seconds
        private int lockStartTick;
        private bool inLockPhase = true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Saber Lock Phase
            yield return new Toil
            {
                initAction = () =>
                {
                    lockStartTick = Find.TickManager.TicksGame;
                    pawn.pather.StopDead();
                },
                tickAction = () =>
                {
                    // Force facing during lock
                    pawn.rotationTracker.FaceTarget(TargetA);
                    if (TargetA.Thing is Pawn enemy)
                        enemy.rotationTracker.FaceTarget(pawn);

                    // End lock after duration or if conditions change
                    if (Find.TickManager.TicksGame > lockStartTick + LockDurationTicks ||
                        !TargetA.Thing.Spawned ||
                        (TargetA.Thing as Pawn)?.CurJob?.def != DefDatabase<JobDef>.GetNamed("Force_SaberLock"))
                    {
                        inLockPhase = false;
                        ReadyForNextToil();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };

            // Then proceed with normal attack behavior
            foreach (var toil in base.MakeNewToils())
            {
                yield return toil;
            }
        }

        public override void Notify_PatherFailed()
        {
            if (!inLockPhase) base.Notify_PatherFailed();
        }
    }
}