using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberSaiCha : CompAbilityEffect_LightsaberCombat
    {
        protected override void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn == null)
            {
                Messages.Message("Invalid target.", MessageTypeDefOf.RejectInput);
                return;
            }

            var breathingPart = FindBreathingTarget(target.Pawn);
            if (breathingPart != null)
            {
                LightsaberCombatUtility.DestroyLimb(parent.pawn, target.Pawn, breathingPart);
                if (!target.Pawn.Dead)
                {
                    var lungHediff = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, target.Pawn, breathingPart);
                    target.Pawn.health.AddHediff(lungHediff);
                }
            }
            else
            {
                Messages.Message("No breathing pathways left to target.", MessageTypeDefOf.RejectInput);
            }
        }

        private BodyPartRecord FindBreathingTarget(Pawn target)
        {
            return target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.BreathingPathway))
                .RandomElementWithFallback();
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (target.Pawn == null)
            {
                if (throwMessages)
                    Messages.Message("Invalid target.", MessageTypeDefOf.RejectInput);
                return false;
            }

            if (FindBreathingTarget(target.Pawn) == null)
            {
                if (throwMessages)
                    Messages.Message("No breathing pathways left to target.", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.Pawn != null)
            {
                var part = FindBreathingTarget(target.Pawn);
                if (part != null)
                {
                    return "TargetBreathingPathway".Translate() + ": " + part.Label;
                }
            }
            return base.ExtraLabelMouseAttachment(target);
        }
    }
}
