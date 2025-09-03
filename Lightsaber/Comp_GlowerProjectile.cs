using RimWorld;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    internal class Comp_GlowerProjectile : CompGlower
    {
        private IntVec3 previousPosition;

        public override void CompTick()
        {
            if (parent.Spawned)
            {
                var position = parent.Position;
                if (position != previousPosition)
                {
                    ForceRegister(parent.Map);
                    previousPosition = position;
                }
            }
        }
    }
}