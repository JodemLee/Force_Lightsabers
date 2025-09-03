using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber.Dueling_System
{
    public class RitualRoleAnyHumanlike : RitualRoleColonist
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            if (!AppliesIfChild(p, out reason, skipReason))
            {
                return false;
            }
            if (!p.RaceProps.Humanlike)
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeHumanlike".Translate(base.Label);
                }
                return false;
            }
            if (requiredWorkType != null && p.WorkTypeIsDisabled(requiredWorkType))
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeCapableOfGeneric".Translate(base.LabelCap, requiredWorkType.gerundLabel);
                }
                return false;
            }
            if (usedSkill != null && p.skills.GetSkill(usedSkill).TotallyDisabled)
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeCapableOfGeneric".Translate(base.LabelCap, usedSkill.label);
                }
                return false;
            }
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
