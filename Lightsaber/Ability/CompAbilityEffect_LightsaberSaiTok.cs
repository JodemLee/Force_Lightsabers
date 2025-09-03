using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberSaiTok : CompAbilityEffect_LightsaberCombat
    {
        protected override void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn == null)
            {
                Messages.Message("Invalid target.", MessageTypeDefOf.RejectInput);
                return;
            }

            var movableLimbs = FindMovableLimbs(target.Pawn);
            if (movableLimbs?.Count > 0)
            {
                var limb = movableLimbs.RandomElement();
                LightsaberCombatUtility.DestroyLimb(parent.pawn, target.Pawn, limb);

            }
            else
            {
                Messages.Message("No movable limbs left to amputate.", MessageTypeDefOf.RejectInput);
            }
        }

        private List<BodyPartRecord> FindMovableLimbs(Pawn target)
        {
            return target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore))
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

            var movableLimbs = FindMovableLimbs(target.Pawn);
            if (movableLimbs == null || movableLimbs.Count == 0)
            {
                if (throwMessages)
                    Messages.Message("No movable limbs left to amputate.", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.Pawn != null)
            {
                var parts = FindMovableLimbs(target.Pawn);
                if (parts?.Count > 0)
                {
                    return "PotentialTargets".Translate() + ": " +
                           parts.Count + " movable limbs";
                }
            }
            return base.ExtraLabelMouseAttachment(target);
        }
    }
}
