using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberMouKei : CompAbilityEffect_LightsaberCombat
    {
        protected override void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn == null) return;

            var limbsToAmputate = FindManipulationTargets(target.Pawn);
            if (limbsToAmputate?.Count > 0)
            {
                foreach (var limb in limbsToAmputate)
                {
                    if (limb != null && !target.Pawn.health.hediffSet.PartIsMissing(limb))
                    {
                        LightsaberCombatUtility.DestroyLimb(parent.pawn, target.Pawn, limb);
                    }
                }
            }
            else
            {
                Messages.Message("No limbs are left to amputate.", MessageTypeDefOf.RejectInput);
            }
        }

        private List<BodyPartRecord> FindManipulationTargets(Pawn target)
        {
            return target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore) ||
                              part.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore))
                .ToList();
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

            var availableLimbs = FindManipulationTargets(target.Pawn);
            if (availableLimbs == null || availableLimbs.Count == 0)
            {
                if (throwMessages)
                    Messages.Message("No limbs are left to amputate.", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.Pawn != null)
            {
                var parts = FindManipulationTargets(target.Pawn);
                if (parts != null && parts.Count > 0)
                {
                    return "TargetLimbs".Translate() + ": " +
                           string.Join(", ", parts.Take(3).Select(p => p.Label)) +
                           (parts.Count > 3 ? "..." : "");
                }
            }
            return base.ExtraLabelMouseAttachment(target);
        }
    }
}
