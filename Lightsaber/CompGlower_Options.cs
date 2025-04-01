using RimWorld;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class CompGlower_Options : CompGlower
    {
        private ColorInt defaultColorInt = new ColorInt(Color.white);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (parent != null)
            {
                ApplyColor(parent.DrawColor);
            }
        }

        private void ApplyColor(Color color)
        {
            defaultColorInt = new ColorInt(color);
            this.GlowColor = defaultColorInt; // Update the glow color to match the parent's draw color
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref defaultColorInt, "defaultColorInt", new ColorInt(Color.white));
        }

        public void UpdateGlowerColor(ColorInt newColor)
        {
            GlowColor = newColor;
            UpdateLit(parent.MapHeld);
        }
    }




    public class CompProperties_GlowerOptions : CompProperties_Glower
    {
        public CompProperties_GlowerOptions()
        {
            this.compClass = typeof(CompGlower_Options);
        }
    }
}
