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
        public List<StuffCategoryDef> validStuffCategoriesHiltColorOne = new List<StuffCategoryDef>();
        public List<StuffCategoryDef> validStuffCategoriesHiltColorTwo = new List<StuffCategoryDef>();
        public List<HiltDef> preferredHilts = new List<HiltDef>();
        public List<HiltPartDef> preferredHiltParts = new List<HiltPartDef>();
    }
}
