using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberChoSun : CompAbilityEffect_LightsaberCombat
    {
        protected override void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn == null) return;

            var manipulationTarget = FindManipulationTarget(target.Pawn);
            if (manipulationTarget != null)
            {
                LightsaberCombatUtility.DestroyLimb(parent.pawn, target.Pawn, manipulationTarget);
            }
            else
            {
                Messages.Message("No limbs are left to manipulate.", MessageTypeDefOf.RejectInput);
            }
        }

        private BodyPartRecord FindManipulationTarget(Pawn target)
        {
            return target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore) ||
                              part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment))
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
                    Messages.Message("No limbs are left to manipulate.", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.Pawn != null)
            {
                var part = FindManipulationTarget(target.Pawn);
                if (part != null)
                {
                    return "TargetLimb".Translate() + ": " + part.Label;
                }
            }
            return base.ExtraLabelMouseAttachment(target);
        }
    }
}
