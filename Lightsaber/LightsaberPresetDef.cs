using RimWorld;
using System.Collections.Generic;
using UnityEngine;

namespace Lightsaber
{
    internal class LightsaberPresetDef : WeaponTraitDef
    {
        public LightsaberPreset LightsaberPreset;
        public new ColorDef forcedColor;
    }

    public class LightsaberPreset
    {
        public List<HiltDef> preferredHilts = new List<HiltDef>();
        public List<HiltPartDef> preferredHiltParts = new List<HiltPartDef>();

        // Change these to ColorDef references
        public ColorDef bladeColor1;
        public ColorDef bladeColor2;
        public ColorDef coreColor1;
        public ColorDef coreColor2;
        public ColorDef hiltColor1;
        public ColorDef hiltColor2;

        // Helper properties to get the actual colors
        public Color BladeColor1 => bladeColor1?.color ?? Color.white;
        public Color BladeColor2 => bladeColor2?.color ?? Color.white;
        public Color CoreColor1 => coreColor1?.color ?? Color.white;
        public Color CoreColor2 => coreColor2?.color ?? Color.white;
        public Color HiltColor1 => hiltColor1?.color ?? Color.white;
        public Color HiltColor2 => hiltColor2?.color ?? Color.white;
    }
}
