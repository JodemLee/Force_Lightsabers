using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class CompColorCrystal : ThingComp
    {
        public ColorInt CrystalColor;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            var colorableComp = parent.GetComp<CompColorable>();
            if (colorableComp != null)
            {
                CrystalColor = new ColorInt(parent.DrawColor);
            }
            else
            {
                CrystalColor = new ColorInt(Color.white);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            
            Scribe_Values.Look(ref CrystalColor, "CrystalColor", new ColorInt(Color.white));
        }
    }

    public class CompProperties_ColorCrystal : CompProperties
    {
        public CompProperties_ColorCrystal()
        {
            this.compClass = typeof(CompColorCrystal);
        }
    }
}