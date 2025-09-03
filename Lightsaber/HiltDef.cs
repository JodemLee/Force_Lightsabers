using Lightsaber;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class HiltDef : Def
    {
        public GraphicData graphicData;
        public Graphic GetColoredVersion(Shader shader, Color colorOne, Color colorTwo)
        {
            return graphicData.Graphic.GetColoredVersion(shader, colorOne, colorTwo);
        }
    }

    public class HiltPartCategoryDef : Def
    {
        public int displayOrder = 0;
        public bool canChangeColor;
    }

    public class HiltPartDef : Def
    {
        public HiltPartCategoryDef category; // Changed from string to HiltPartCategoryDef
        public List<StatModifier> equippedStatOffsets;
        public ColorGenerator colorGenerator;
        public ColorGenerator colorGenerator2;
        public ThingDef requiredComponent;
        public ThingDef droppedComponent => requiredComponent;
        public List<HediffDef> bonusDamageHediff;
        public List<DamageDef> damageDefs;
        public float commonality = 1f;
        public List<HiltEffectors> effects;

        private Dictionary<StatDef, float> _statOffsetLookup;
        public Dictionary<StatDef, float> StatOffsetLookup =>
            _statOffsetLookup ??= equippedStatOffsets?.ToDictionary(x => x.stat, x => x.value) ?? new Dictionary<StatDef, float>();
    }

    public class StatPart_EquippedStatOffsetIncrease : StatPart
    {
        public HiltPartCategoryDef category; // Changed from string to HiltPartCategoryDef
        private static CompCache compCache = new CompCache();
        private static readonly string TotalPrefix = "Total: ";
        private static readonly string ToStatSuffix = " to ";

        private const int DefaultCacheDurationTicks = 250;
        private const int CacheVarianceTicks = 50;

        private static Dictionary<Pawn, Dictionary<StatDef, EquipmentStatCache>> statCacheDict =
            new Dictionary<Pawn, Dictionary<StatDef, EquipmentStatCache>>();

        public static void ClearAllCaches()
        {
            statCacheDict.Clear();
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (parentStat == null || req.Thing is not Pawn pawn || pawn.equipment?.Primary == null)
                return;

            var cache = GetStatCache(pawn, parentStat);
            val += cache.StatOffset;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is not Pawn pawn || pawn.equipment?.Primary == null) return null;

            var cache = GetStatCache(pawn, parentStat);

            if (cache.StatOffset == 0f) return null;

            var explanation = new StringBuilder();
            if (cache.ExplanationParts.Count > 0)
            {
                foreach (var part in cache.ExplanationParts)
                {
                    explanation.AppendLine(part);
                }
            }

            explanation.Append(TotalPrefix)
                       .Append(cache.StatOffset.ToString("+0.##;-0.##"))
                       .Append(ToStatSuffix)
                       .Append(parentStat.label);

            return explanation.ToString();
        }

        private EquipmentStatCache GetStatCache(Pawn pawn, StatDef statDef)
        {
            if (pawn == null || statDef == null)
            {
                Log.WarningOnce($"Null pawn or statDef in GetStatCache ({pawn}, {statDef})", 76453321);
                return new EquipmentStatCache(null, null, null);
            }

            if (!statCacheDict.TryGetValue(pawn, out var pawnCache))
            {
                pawnCache = new Dictionary<StatDef, EquipmentStatCache>();
                statCacheDict[pawn] = pawnCache;
            }

            if (!pawnCache.TryGetValue(statDef, out var statCache))
            {
                statCache = new EquipmentStatCache(pawn, statDef, category);
                pawnCache[statDef] = statCache;
            }

            if (statCache.IsStale)
            {
                try
                {
                    statCache.Reset();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error resetting stat cache for {pawn}: {ex}");
                    statCache = new EquipmentStatCache(pawn, statDef, category);
                    pawnCache[statDef] = statCache;
                }
            }

            return statCache;
        }

        private class EquipmentStatCache
        {
            public readonly Pawn Pawn;
            public readonly StatDef StatDef;
            public readonly HiltPartCategoryDef Category; // Changed from string to HiltPartCategoryDef

            public float StatOffset { get; private set; }
            public List<string> ExplanationParts { get; } = new List<string>();
            public bool IsStale => Find.TickManager.TicksGame > recacheTick;

            private int recacheTick;

            public EquipmentStatCache(Pawn pawn, StatDef statDef, HiltPartCategoryDef category)
            {
                Pawn = pawn;
                StatDef = statDef;
                Category = category;
                Reset();
            }

            public void Reset()
            {
                recacheTick = Find.TickManager.TicksGame +
                             DefaultCacheDurationTicks +
                             Rand.Range(-CacheVarianceTicks, CacheVarianceTicks);

                StatOffset = 0f;
                ExplanationParts.Clear();
                if (Pawn?.equipment?.Primary == null)
                    return;
                var lightsaberComp = compCache.GetCachedComp(Pawn.equipment.Primary);
                if (lightsaberComp?.parent == null)
                    return;
                if (!IsEquipmentWornByPawn(lightsaberComp.parent, Pawn))
                    return;

                ProcessHiltParts(lightsaberComp);
            }

            private bool IsEquipmentWornByPawn(Thing equipment, Pawn pawn)
            {
                // Check all possible ways equipment can be associated with a pawn
                return (equipment.ParentHolder is Pawn_EquipmentTracker et && et.pawn == pawn) ||
                       (equipment.ParentHolder is Pawn_InventoryTracker it && it.pawn == pawn) ||
                       (equipment.holdingOwner?.Owner is Pawn p && p == pawn);
            }

            private void ProcessHiltParts(Comp_LightsaberBlade lightsaberComp)
            {
                var hiltParts = lightsaberComp.HiltManager?.SelectedHiltParts;
                if (hiltParts == null || hiltParts.Count == 0)
                    return;

                var relevantParts = Category != null
                    ? hiltParts.Where(p => p?.category == Category).ToList()
                    : hiltParts;

                foreach (var part in relevantParts)
                {
                    if (part?.StatOffsetLookup?.TryGetValue(StatDef, out float partOffset) == true &&
                        partOffset != 0f)
                    {
                        StatOffset += partOffset;
                        ExplanationParts.Add($"{part.label}: {partOffset:+0.##;-0.##} to {StatDef.label}");
                    }
                }
            }
        }
    }
}