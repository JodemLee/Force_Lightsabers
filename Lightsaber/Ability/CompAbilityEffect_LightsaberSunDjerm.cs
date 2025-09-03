using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Ability
{
    internal class CompAbilityEffect_LightsaberSunDjerm : CompAbilityEffect_LightsaberCombat
    {
        protected override void AttackTarget(LocalTargetInfo target)
        {
            if (target.Pawn == null || target.Pawn.equipment?.Primary == null)
            {
                Messages.Message("TargetHasNoWeapon".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (target.Pawn.equipment.TryDropEquipment(target.Pawn.equipment.Primary,
                out ThingWithComps droppedWeapon,
                target.Pawn.Position,
                true))
            {
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
                return false;

            if (target.Pawn?.equipment?.Primary == null)
            {
                if (throwMessages)
                    Messages.Message("TargetHasNoWeapon".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (target.Pawn?.equipment?.Primary != null)
            {
                return "TargetWeapon".Translate() + ": " + target.Pawn.equipment.Primary.LabelCap;
            }
            return base.ExtraLabelMouseAttachment(target);
        }
    }
}
