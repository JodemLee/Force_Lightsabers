using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class Comp_LightsaberStance : ThingComp
    {
        public CompProperties_LightsaberStance Props => (CompProperties_LightsaberStance)props;

        private Dictionary<AbilityDef, bool> alreadyHadAbilities = new Dictionary<AbilityDef, bool>();
        private Dictionary<HediffDef, float> lastSeverities = new Dictionary<HediffDef, float>();

        public List<float> savedStanceAngles = new List<float>();
        public List<Vector3> savedDrawOffsets = new List<Vector3>();
        private DefStanceAngles extension;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref alreadyHadAbilities, "alreadyHadAbilities", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref lastSeverities, "lastSeverities", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref savedStanceAngles, "savedStanceAngles", LookMode.Value);
            Scribe_Collections.Look(ref savedDrawOffsets, "savedDrawOffsets", LookMode.Value);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (pawn?.equipment?.Primary == null) return;

            try
            {
                AssignAbilities(pawn, pawn.HasPsylink);
                AssignHediffs(pawn, pawn.HasPsylink);
                ApplyStanceRotation(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"Error equipping lightsaber stance: {ex}");
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (pawn == null) return;

            RemoveAbilities(pawn);
            RemoveHediffs(pawn);

            if (pawn.equipment.Primary?.GetComp<Comp_LightsaberStance>() is Comp_LightsaberStance compStance)
            {
                compStance.savedStanceAngles = new List<float>(savedStanceAngles);
                compStance.savedDrawOffsets = new List<Vector3>(savedDrawOffsets);
            }
            alreadyHadAbilities.Clear();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent is Pawn pawn && pawn.equipment?.Primary != null)
            {
                ApplyStanceRotation(pawn);
            }
        }

        private void AssignAbilities(Pawn pawn, bool hasPsylink)
        {
            if (pawn?.abilities == null) return;

            var abilitiesToAdd = hasPsylink ? Props.abilitiesRequiringPsylink : Props.abilitiesNotRequiringPsylink;
            foreach (var abilityDef in abilitiesToAdd.Where(a => a != null))
            {
                // Check if pawn already had this ability before we added it
                alreadyHadAbilities[abilityDef] = pawn.abilities.abilities.Any(a => a.def == abilityDef);

                if (!alreadyHadAbilities[abilityDef])
                {
                    pawn.abilities.GainAbility(abilityDef);
                }
            }
        }

        private void RemoveAbilities(Pawn pawn)
        {
            if (pawn?.abilities == null) return;

            foreach (var abilityDef in Props.abilitiesRequiringPsylink.Concat(Props.abilitiesNotRequiringPsylink))
            {
                // Only remove the ability if we added it (pawn didn't have it before)
                if (alreadyHadAbilities.TryGetValue(abilityDef, out bool hadBefore) && !hadBefore)
                {
                    var ability = pawn.abilities.abilities.FirstOrDefault(a => a.def == abilityDef);
                    if (ability != null)
                    {
                        pawn.abilities.RemoveAbility(ability.def);
                    }
                }
            }
        }

        private void AssignHediffs(Pawn pawn, bool hasPsylink)
        {
            if (pawn?.health == null) return;

            var hediffsToAdd = hasPsylink ? Props.hediffsRequiringPsylink : Props.hediffsNotRequiringPsylink;
            foreach (var hediffDef in hediffsToAdd.Where(h => h != null))
            {
                if (pawn.health.hediffSet.HasHediff(hediffDef)) continue;

                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = lastSeverities.TryGetValue(hediffDef, out var severity)
                    ? severity
                    : Rand.Range(Props.minSeverity, Props.maxSeverity);

                pawn.health.AddHediff(hediff);
            }
        }

        private void RemoveHediffs(Pawn pawn)
        {
            foreach (var hediffDef in Props.hediffsRequiringPsylink.Concat(Props.hediffsNotRequiringPsylink))
            {
                var hediffs = pawn.health.hediffSet.hediffs.Where(h => h.def == hediffDef).ToList();
                foreach (var hediff in hediffs)
                {
                    lastSeverities[hediff.def] = hediff.Severity;
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        private void ApplyStanceRotation(Pawn pawn)
        {
            var lightsaberComp = pawn.equipment.Primary?.GetComp<Comp_LightsaberBlade>();
            if (lightsaberComp != null)
            {
                var (angle, offset) = GetStanceAngleAndOffset(pawn);

                lightsaberComp.UpdateRotationForStance(angle);
                lightsaberComp.UpdateDrawOffsetForStance(offset);
            }
        }

        private (float angle, Vector3 offset) GetStanceAngleAndOffset(Pawn pawn)
        {
            var hediffs = Props.hediffsRequiringPsylink.Concat(Props.hediffsNotRequiringPsylink)
                .Select(hediffDef => pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef))
                .Where(h => h != null)
                .ToList();

            if (!hediffs.Any()) return (0f, Vector3.zero);

            var thingDef = pawn.equipment.Primary?.def;
            extension = thingDef?.GetModExtension<DefStanceAngles>() ?? hediffs.First().def.GetModExtension<DefStanceAngles>();

            var maxSeverityHediff = hediffs.OrderByDescending(h => h.Severity).First();
            StanceData stanceData = extension?.GetStanceDataForSeverity(maxSeverityHediff.Severity);

            return (stanceData?.Angle ?? 0f, stanceData?.Offset ?? Vector3.zero);
        }

        public void ResetToDefault(List<float> defaultStanceAngles, List<Vector3> defaultDrawOffsets)
        {
            savedStanceAngles = new List<float>(defaultStanceAngles);
            savedDrawOffsets = new List<Vector3>(defaultDrawOffsets);
        }
    }

    public class CompProperties_LightsaberStance : CompProperties
    {
        public List<AbilityDef> abilitiesRequiringPsylink;
        public List<AbilityDef> abilitiesNotRequiringPsylink;
        public List<HediffDef> hediffsRequiringPsylink;
        public List<HediffDef> hediffsNotRequiringPsylink;

        public int minSeverity = 1;
        public int maxSeverity = 7;

        public CompProperties_LightsaberStance()
        {
            this.compClass = typeof(Comp_LightsaberStance);
            this.hediffsRequiringPsylink = new List<HediffDef>();
            this.hediffsNotRequiringPsylink = new List<HediffDef>();
            this.abilitiesRequiringPsylink = new List<AbilityDef>();
            this.abilitiesNotRequiringPsylink = new List<AbilityDef>();
        }
    }
}