using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    internal class ModExtension_LightsaberPresets : DefModExtension
    {
        public List<Color> bladeColors;
        public List<Color> coreColors;
        public Color? hiltColorOne = null;  // Now nullable
        public Color? hiltColorTwo = null;  // Now nullable
        public List<StuffCategoryDef> validStuffCategoriesHiltColorOne = new List<StuffCategoryDef>();
        public List<StuffCategoryDef> validStuffCategoriesHiltColorTwo = new List<StuffCategoryDef>();
        public float defaultBladeLength1 = 1f;
        public float defaultBladeLength2 = 1f;
        public List<HiltDef> preferredHilts = new List<HiltDef>();
        public List<HiltPartDef> preferredHiltParts = new List<HiltPartDef>();
    }
}
