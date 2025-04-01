using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public static class StuffColorUtility
    {
        private static System.Random random = new System.Random();
        public static List<ThingDef> GetAllStuffed()
        {
            List<ThingDef> allStuffedDefs = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.stuffProps != null)
                {
                    allStuffedDefs.Add(def);
                }
            }

            return allStuffedDefs;
        }
        public static Color GetStuffColor(ThingDef thingDef)
        {
            if (thingDef.stuffProps != null && thingDef.stuffProps.color != null)
            {
                return thingDef.stuffProps.color;
            }
            if (thingDef.graphicData != null && thingDef.graphicData.color != null)
            {
                return thingDef.graphicData.color;
            }
            return Color.white;
        }

        public static Color GetRandomColorFromStuffCategories(List<StuffCategoryDef> categories)
        {
            List<ThingDef> allStuffedDefs = GetAllStuffed();
            Dictionary<StuffCategoryDef, List<ThingDef>> categorizedStuffDefs = new Dictionary<StuffCategoryDef, List<ThingDef>>();

            foreach (ThingDef def in allStuffedDefs)
            {
                if (def.stuffProps != null && def.stuffProps.categories != null)
                {
                    foreach (StuffCategoryDef category in def.stuffProps.categories)
                    {
                        if (category.label != "Metallic")
                        {
                            if (!categorizedStuffDefs.ContainsKey(category))
                            {
                                categorizedStuffDefs[category] = new List<ThingDef>();
                            }
                            categorizedStuffDefs[category].Add(def);
                        }
                    }
                }
            }

            List<ThingDef> matchingDefs = categorizedStuffDefs
                .Where(pair => categories.Contains(pair.Key))
                .SelectMany(pair => pair.Value)
                .ToList();

            List<Color> colors = matchingDefs
                .Select(def => StuffColorUtility.GetStuffColor(def))
                .Where(color => color != Color.white)
                .ToList();

            if (colors.Count > 0)
            {
                return colors[random.Next(colors.Count)];
            }
            else
            {
                return Color.white;
            }
        }
    }

    public static class ProjectileUtility
    {
        public static List<ThingDef> GetAllProjectiles()
        {
            List<ThingDef> allProjectiles = new List<ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                // Check if this ThingDef has a projectile component
                if (def.projectile != null)
                {
                    allProjectiles.Add(def);
                }
            }

            return allProjectiles;
        }
    }
}
