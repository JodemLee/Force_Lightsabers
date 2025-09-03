using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberChoMai : CompAbilityEffect_LightsaberCombat
    {
        protected override void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn == null) return;

            var manipulationTarget = FindManipulationTarget(target.Pawn);
            if (manipulationTarget != null)
            {
                LightsaberCombatUtility.DestroyLimb(parent.pawn, target.Pawn, manipulationTarget);
                if (target.Pawn.equipment?.Primary != null)
                {
                    target.Pawn.equipment.TryDropEquipment(target.Pawn.equipment.Primary,
                        out _,
                        target.Pawn.Position);
                }
            }
            else
            {
                Messages.Message("No manipulable limbs are left.", MessageTypeDefOf.RejectInput);
            }
        }

        private BodyPartRecord FindManipulationTarget(Pawn target)
        {
            return target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment))
                .RandomElementWithFallback();
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (target.Pawn == null)
            {
                if (throwMessages)
                    Messages.Message("AbilityCanOnlyTargetPawns".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            if (FindManipulationTarget(target.Pawn) == null)
            {
                if (throwMessages)
                    Messages.Message("No manipulable limbs are left.", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }
}