using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberCombat : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            NotifyMeleeAttackOn(target);

            if (LightsaberCombatUtility.CanParry(target.Pawn, parent.pawn))
            {
                IntVec3? overlapPoint = GetRandomCellBetween(parent.pawn.Position, target.Pawn.Position);
                if (overlapPoint.HasValue)
                {
                    LightsaberCombatUtility.TriggerWeaponRotationOnParry(parent.pawn, target.Pawn);

                    DamageInfo deflectInfo = new DamageInfo(DamageDefOf.Blunt, 0, 0, -1, parent.pawn);
                    target.Pawn.Drawer.Notify_DamageDeflected(deflectInfo);

                    Effecter effecter = new Effecter(LightsaberDefOf.Force_LClashOne);
                    effecter.Trigger(new TargetInfo(overlapPoint.Value, parent.pawn.Map), TargetInfo.Invalid);
                    effecter.Cleanup();
                }
                return;
            }

            AttackTarget(target);
        }

        protected IntVec3? GetRandomCellBetween(IntVec3 casterPos, IntVec3 targetPos)
        {
            IEnumerable<IntVec3> lineCells = GenSight.PointsOnLineOfSight(casterPos, targetPos)
                .Where(cell => cell.InBounds(parent.pawn.Map) && cell.Walkable(parent.pawn.Map)).ToList();

            return lineCells.Any() ? lineCells.RandomElement() : lineCells.First();
        }

        protected void NotifyMeleeAttackOn(LocalTargetInfo target)
        {
            if (target.HasThing && target.Thing.Position != parent.pawn.Position)
            {
                parent.pawn.Drawer.Notify_MeleeAttackOn(target.Thing);
            }
        }

        protected virtual void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn != null)
            {
                parent.pawn.meleeVerbs.TryMeleeAttack(target.Pawn, parent.pawn.equipment.PrimaryEq.PrimaryVerb, true);
            }
        }
    }
}
