using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber
{
    internal class Verb_AbilityLaunchProjectile : Verb_AbilityShoot
    {
        protected override bool TryCastShot()
        {
            if(base.Ability == null)
            {
                return false;
            }
            if (!base.Ability.CanCast)
            {
                return false;
            }

            bool num = base.TryCastShot();
            if (num)
            {
                this.Ability.StartCooldown(this.Ability.def.cooldownTicksRange.RandomInRange);
            }
            return num;
        }
    }
}

