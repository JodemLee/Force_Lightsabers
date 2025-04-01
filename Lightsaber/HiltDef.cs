using Lightsaber;
using RimWorld;
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

    public class HiltPartDef : Def
    {
        public HiltPartCategory category; // "PowerCell", "Casing", "Lens", "Emitter", "Crystal"
        public List<StatModifier> equippedStatOffsets;
        public ColorGenerator colorGenerator;
        public ThingDef requiredComponent;
        public ThingDef droppedComponent => requiredComponent;
        public List<HediffDef> bonusDamageHediff;
        public float commonality = 1f;
    }

    public enum HiltPartCategory
    {
        PowerCell,
        Crystal,
        Lens,
        Emitter,
        Casing
    }

    public class StatPart_EquippedStatOffsetIncrease : StatPart
    {
        public HiltPartCategory? category;
        private static CompCache compCache = new CompCache();

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is not Pawn pawn || pawn.equipment?.Primary == null) return;

            var hiltParts = GetRelevantHiltParts(pawn);
            if (hiltParts == null) return;

            val += CalculateStatOffset(hiltParts);
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is not Pawn pawn || pawn.equipment?.Primary == null) return null;

            var hiltParts = GetRelevantHiltParts(pawn);
            if (hiltParts == null) return null;

            StringBuilder explanation = new StringBuilder();
            float totalOffset = CalculateStatOffset(hiltParts, explanation);

            if (totalOffset != 0f)
            {
                explanation.AppendLine($"Total: {totalOffset:+0.##;-0.##} to {parentStat.label}");
                return explanation.ToString();
            }

            return null;
        }

        private float CalculateStatOffset(List<HiltPartDef> hiltParts, StringBuilder explanation = null)
        {
            float totalOffset = 0f;

            foreach (var part in hiltParts)
            {
                float partOffset = 0f;

                if (part.equippedStatOffsets != null)
                {
                    foreach (var mod in part.equippedStatOffsets)
                    {
                        if (mod.stat == parentStat)
                        {
                            partOffset += mod.value;
                        }
                    }
                }

                if (partOffset != 0f)
                {
                    explanation?.AppendLine($"{part.label}: {partOffset:+0.##;-0.##} to {parentStat.label}");
                    totalOffset += partOffset;
                }
            }

            return totalOffset;
        }

        private List<HiltPartDef> GetRelevantHiltParts(Pawn pawn)
        {
            var primary = pawn.equipment.Primary;
            var lightsaberComp = compCache.GetCachedComp(primary);

            if (lightsaberComp?.parent.ParentHolder?.ParentHolder is not Pawn weaponWearer || weaponWearer != pawn)
                return null;

            // Access the HiltManager to get the current hilt parts
            var hiltParts = lightsaberComp.HiltManager.SelectedHiltParts;

            if (category.HasValue)
                return hiltParts.Where(part => part.category == category.Value).ToList();

            return hiltParts;
        }
    }
}