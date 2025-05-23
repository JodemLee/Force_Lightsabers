﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Lightsaber
{
    internal class DamageWorker_LightsaberCut : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            // Check if this is a duel situation
            if (victim is Pawn pawn && IsInDuel(pawn, dinfo.Instigator as Pawn))
            {
                // Create a copy of the damage info but change it to blunt damage
                DamageInfo bluntDinfo = new DamageInfo(
                    def: LightsaberDefOf.Force_BluntLightsaber,
                    amount: 1f,
                    armorPenetration: dinfo.ArmorPenetrationInt * 0.8f,
                    angle: dinfo.Angle,
                    instigator: dinfo.Instigator,
                    hitPart: dinfo.HitPart,
                    weapon: dinfo.Weapon,
                    category: dinfo.Category,
                    intendedTarget: null,
                    false

                );
                return base.Apply(bluntDinfo, victim);
            }

            // Normal behavior for non-duel situations
            if (!(victim is Pawn))
            {
                return base.Apply(dinfo, victim);
            }
            return ApplyToPawn(dinfo, victim as Pawn);
        }

        private bool IsInDuel(Pawn victim, Pawn instigator)
        {
            // Check if either pawn is in a duel
            return (victim.GetLord()?.LordJob is LordJob_Ritual_LightsaberDuel) ||
                   (instigator?.GetLord()?.LordJob is LordJob_Ritual_LightsaberDuel);
        }


        private DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {

            if (pawn.RaceProps.IsMechanoid)
            {
                var damageExtension = dinfo.Def.GetModExtension<DamageDefExtension>().armorCategoryForMechanoids;
                dinfo.Def.armorCategory = damageExtension;
                dinfo.Def.defaultArmorPenetration = dinfo.Def.defaultArmorPenetration / 2;
            }
            else
            {
                dinfo.Def.armorCategory = dinfo.Def.armorCategory;
            }

            DamageResult damageResult = new DamageResult();
            if (dinfo.Amount <= 0f)
            {
                return damageResult;
            }
            if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer)
            {
                return damageResult;
            }

            var lightsaberBlade = pawn.equipment?.Primary?.TryGetComp<Comp_LightsaberBlade>();
            if (dinfo.Instigator is Pawn attacker)
            {
                if (lightsaberBlade != null && LightsaberCombatUtility.CanParry(pawn, attacker)) // Check if target can parry
                {
                    LightsaberCombatUtility.TriggerWeaponRotationOnParry(pawn, attacker);
                    Effecter effecter = new Effecter(LightsaberDefOf.Force_LClashOne);
                    effecter.Trigger(new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
                    effecter.Cleanup();
                    return damageResult;
                }

            }

            Map mapHeld = pawn.MapHeld;
            bool spawnedOrAnyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
            if (dinfo.ApplyAllDamage)
            {
                float num = dinfo.Amount;
                int num2 = 25;
                float b = num / (float)dinfo.DamagePropagationPartsRange.RandomInRange;
                do
                {
                    DamageInfo dinfo2 = dinfo;
                    dinfo2.SetAmount(Mathf.Min(num, b));
                    ApplyDamageToPart(dinfo2, pawn, damageResult);
                    num -= damageResult.totalDamageDealt;
                }
                while (num2-- > 0 && num > 0f);
            }
            else if (dinfo.AllowDamagePropagation && dinfo.Amount >= (float)dinfo.Def.minDamageToFragment)
            {
                int randomInRange = dinfo.DamagePropagationPartsRange.RandomInRange;
                for (int i = 0; i < randomInRange; i++)
                {
                    DamageInfo dinfo3 = dinfo;
                    dinfo3.SetAmount(dinfo.Amount / (float)randomInRange);
                    ApplyDamageToPart(dinfo3, pawn, damageResult);
                }
            }
            else
            {
                ApplyDamageToPart(dinfo, pawn, damageResult);
                ApplySmallPawnDamagePropagation(dinfo, pawn, damageResult);
            }

            if (damageResult.wounded)
            {
                PlayWoundedVoiceSound(dinfo, pawn);
                pawn.Drawer.Notify_DamageApplied(dinfo);
                EffecterDef damageEffecter = pawn.RaceProps.FleshType.damageEffecter;
                if (damageEffecter != null)
                {
                    if (pawn.health.woundedEffecter != null && pawn.health.woundedEffecter.def != damageEffecter)
                    {
                        pawn.health.woundedEffecter.Cleanup();
                    }
                    pawn.health.woundedEffecter = damageEffecter.Spawn();
                    pawn.health.woundedEffecter.Trigger(pawn, dinfo.Instigator ?? pawn);
                }
                if (dinfo.Def.damageEffecter != null)
                {
                    Effecter effecter = dinfo.Def.damageEffecter.Spawn();
                    effecter.Trigger(pawn, pawn);
                    effecter.Cleanup();
                }
            }

            if (damageResult.headshot && pawn.Spawned)
            {
                MoteMaker.ThrowText(new Vector3((float)pawn.Position.x + 1f, pawn.Position.y, (float)pawn.Position.z + 1f), pawn.Map, "Headshot".Translate(), Color.white);
                if (dinfo.Instigator != null && dinfo.Instigator is Pawn pawn2)
                {
                    pawn2.records.Increment(RecordDefOf.Headshots);
                }
            }

            if ((damageResult.deflected || damageResult.diminished) && spawnedOrAnyParentSpawned)
            {
                EffecterDef effecterDef = (damageResult.deflected ? ((damageResult.deflectedByMetalArmor && dinfo.Def.canUseDeflectMetalEffect) ? ((dinfo.Def != DamageDefOf.Bullet) ? EffecterDefOf.Deflect_Metal : EffecterDefOf.Deflect_Metal_Bullet) : ((dinfo.Def != DamageDefOf.Bullet) ? EffecterDefOf.Deflect_General : EffecterDefOf.Deflect_General_Bullet)) : ((!damageResult.diminishedByMetalArmor) ? EffecterDefOf.DamageDiminished_General : EffecterDefOf.DamageDiminished_Metal));
                if (pawn.health.deflectionEffecter == null || pawn.health.deflectionEffecter.def != effecterDef)
                {
                    if (pawn.health.deflectionEffecter != null)
                    {
                        pawn.health.deflectionEffecter.Cleanup();
                        pawn.health.deflectionEffecter = null;
                    }
                    pawn.health.deflectionEffecter = effecterDef.Spawn();
                }
                pawn.health.deflectionEffecter.Trigger(pawn, dinfo.Instigator ?? pawn);
                if (damageResult.deflected)
                {
                    pawn.Drawer.Notify_DamageDeflected(dinfo);
                }
            }

            if (!damageResult.deflected && spawnedOrAnyParentSpawned)
            {
                ImpactSoundUtility.PlayImpactSound(pawn, dinfo.Def.impactSoundType, mapHeld);
            }

            return damageResult;
        }

        private void ApplySmallPawnDamagePropagation(DamageInfo dinfo, Pawn pawn, DamageResult result)
        {
            if (dinfo.AllowDamagePropagation && result.LastHitPart != null && dinfo.Def.harmsHealth && result.LastHitPart != pawn.RaceProps.body.corePart && result.LastHitPart.parent != null && pawn.health.hediffSet.GetPartHealth(result.LastHitPart.parent) > 0f && result.LastHitPart.parent.coverageAbs > 0f && dinfo.Amount >= 10f && pawn.HealthScale <= 0.5001f)
            {
                DamageInfo dinfo2 = dinfo;
                dinfo2.SetHitPart(result.LastHitPart.parent);
                ApplyDamageToPart(dinfo2, pawn, result);
            }
        }

        private void ApplyDamageToPart(DamageInfo dinfo, Pawn pawn, DamageResult result)
        {
            BodyPartRecord exactPartFromDamageInfo = GetExactPartFromDamageInfo(dinfo, pawn);
            if (exactPartFromDamageInfo == null)
            {
                return;
            }
            dinfo.SetHitPart(exactPartFromDamageInfo);
            float num = dinfo.Amount;
            bool num2 = !dinfo.InstantPermanentInjury && !dinfo.IgnoreArmor;
            bool deflectedByMetalArmor = false;
            if (num2)
            {
                DamageDef damageDef = dinfo.Def;
                num = ArmorUtility.GetPostArmorDamage(pawn, num, dinfo.ArmorPenetrationInt, dinfo.HitPart, ref damageDef, out deflectedByMetalArmor, out var diminishedByMetalArmor);
                dinfo.Def = damageDef;
                if (num < dinfo.Amount)
                {
                    result.diminished = true;
                    result.diminishedByMetalArmor = diminishedByMetalArmor;
                }
            }
            if (dinfo.Def.ExternalViolenceFor(pawn))
            {
                num *= pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
            }
            if (num <= 0f)
            {
                result.AddPart(pawn, dinfo.HitPart);
                result.deflected = true;
                result.deflectedByMetalArmor = deflectedByMetalArmor;
                return;
            }
            if (IsHeadshot(dinfo, pawn))
            {
                result.headshot = true;
            }
            if (!dinfo.InstantPermanentInjury || (HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart).CompPropsFor(typeof(HediffComp_GetsPermanent)) != null && dinfo.HitPart.def.permanentInjuryChanceFactor != 0f && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(dinfo.HitPart)))
            {
                if (!dinfo.AllowDamagePropagation)
                {
                    FinalizeAndAddInjury(pawn, num, dinfo, result);
                }
                else
                {
                    ApplySpecialEffectsToPart(pawn, num, dinfo, result);
                }
            }
        }

        protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
        {
            // Get the lightsaber blade component from the attacker's weapon
            var lightsaberBladeAttacker = dinfo.Instigator is Pawn attacker ?
                attacker.equipment?.Primary?.TryGetComp<Comp_LightsaberBlade>() : null;

            // Check if we have a lightsaber with hilt parts that specify damage types
            if (lightsaberBladeAttacker != null && lightsaberBladeAttacker.HiltManager?.SelectedHiltParts != null)
            {
                // Collect all damageDefs from all hilt parts
                var combinedDamageDefs = new List<DamageDef>();
                foreach (var hiltPart in lightsaberBladeAttacker.HiltManager.SelectedHiltParts)
                {
                    if (hiltPart.damageDefs != null && hiltPart.damageDefs.Count > 0)
                    {
                        combinedDamageDefs.AddRange(hiltPart.damageDefs);
                    }
                }

                // If we have any custom damage defs, use them instead of the default one
                if (combinedDamageDefs.Count > 0)
                {
                    // Randomly select one of the damage defs or use some logic to choose
                    DamageDef selectedDamageDef = combinedDamageDefs.RandomElement();

                    // Create new damage info with the selected damage type
                    DamageInfo newDinfo = new DamageInfo(
                        def: selectedDamageDef,
                        amount: totalDamage,
                        armorPenetration: dinfo.ArmorPenetrationInt,
                        angle: dinfo.Angle,
                        instigator: dinfo.Instigator,
                        hitPart: dinfo.HitPart,
                        weapon: dinfo.Weapon,
                        category: dinfo.Category,
                        intendedTarget: dinfo.IntendedTarget,
                        instigatorGuilty: dinfo.InstigatorGuilty
                    );

                    // Apply the modified damage
                    FinalizeAndAddInjury(pawn, totalDamage, newDinfo, result);
                    CheckDuplicateDamageToOuterParts(newDinfo, pawn, totalDamage, result);
                    return;
                }
            }

            // Default behavior if no custom damage types are specified
            totalDamage = ReduceDamageToPreserveOutsideParts(totalDamage, dinfo, pawn);
            FinalizeAndAddInjury(pawn, totalDamage, dinfo, result);
            CheckDuplicateDamageToOuterParts(dinfo, pawn, totalDamage, result);
        }

        protected new float FinalizeAndAddInjury(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
        {
            if (pawn.health.hediffSet.PartIsMissing(dinfo.HitPart))
            {
                return 0f;
            }
            Pawn pawn2 = dinfo.Instigator as Pawn;
            HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
            Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn);
            hediff_Injury.Part = dinfo.HitPart;
            hediff_Injury.sourceDef = dinfo.Weapon;
            if (pawn2 != null && pawn2.IsMutant && dinfo.Weapon == ThingDefOf.Human)
            {
                hediff_Injury.sourceLabel = pawn2.mutant.Def.label;
            }
            else
            {
                hediff_Injury.sourceLabel = dinfo.Weapon?.label ?? "";
            }
            hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
            hediff_Injury.sourceHediffDef = dinfo.WeaponLinkedHediff;
            hediff_Injury.sourceToolLabel = dinfo.Tool?.labelNoLocation ?? dinfo.Tool?.label;
            hediff_Injury.Severity = totalDamage;
            if (pawn2 != null && pawn2.CurJobDef == JobDefOf.SocialFight)
            {
                hediff_Injury.destroysBodyParts = false;
            }
            if (dinfo.InstantPermanentInjury)
            {
                HediffComp_GetsPermanent hediffComp_GetsPermanent = hediff_Injury.TryGetComp<HediffComp_GetsPermanent>();
                if (hediffComp_GetsPermanent != null)
                {
                    hediffComp_GetsPermanent.IsPermanent = true;
                }
                else
                {
                    Log.Error(string.Concat("Tried to create instant permanent injury on Hediff without a GetsPermanent comp: ", hediffDefFromDamage, " on ", pawn));
                }
            }
            var lightsaberBladeAttacker = pawn2.equipment?.Primary?.TryGetComp<Comp_LightsaberBlade>();
            if (lightsaberBladeAttacker != null)
            {
                foreach (var hiltPart in lightsaberBladeAttacker.HiltManager.SelectedHiltParts)
                {
                    if (hiltPart.bonusDamageHediff != null)
                    {
                        foreach (var hediffDef in hiltPart.bonusDamageHediff)
                        {
                            BodyPartRecord hitPart = dinfo.HitPart;
                            var hediff = HediffMaker.MakeHediff(hediffDef, pawn, hitPart);
                            float scaledSeverity = dinfo.Amount * 0.01f;
                            float maxSeverity = hediffDef.lethalSeverity > 0 ? hediffDef.lethalSeverity : float.MaxValue;
                            hediff.Severity = Math.Min(scaledSeverity, maxSeverity - 0.01f);
                            pawn.health.AddHediff(hediff);
                        }
                    }
                }
            }
            return FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
        }

        protected new float FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, DamageResult result)
        {
            injury.TryGetComp<HediffComp_GetsPermanent>()?.PreFinalizeInjury();
            float partHealth = pawn.health.hediffSet.GetPartHealth(injury.Part);
            if (pawn.IsColonist && !dinfo.IgnoreInstantKillProtection && dinfo.Def.ExternalViolenceFor(pawn) && !Rand.Chance(Find.Storyteller.difficulty.allowInstantKillChance))
            {
                float num = (injury.IsLethal ? (injury.def.lethalSeverity * 1.1f) : 1f);
                float min = 1f;
                float max = Mathf.Min(injury.Severity, partHealth);
                for (int i = 0; i < 7; i++)
                {
                    if (!pawn.health.WouldDieAfterAddingHediff(injury))
                    {
                        break;
                    }
                    float num2 = Mathf.Clamp(partHealth - num, min, max);
                    if (DebugViewSettings.logCauseOfDeath)
                    {
                        Log.Message($"CauseOfDeath: attempt to prevent death for {pawn.Name} on {injury.Part.Label} attempt:{i + 1} severity:{injury.Severity}->{num2} part health:{partHealth}");
                    }
                    injury.Severity = num2;
                    num *= 2f;
                    min = 0f;
                }
            }
            pawn.health.AddHediff(injury, null, dinfo, result);
            float num3 = Mathf.Min(injury.Severity, partHealth);
            result.totalDamageDealt += num3;
            result.wounded = true;
            result.AddPart(pawn, injury.Part);
            result.AddHediff(injury);
            return num3;
        }

        private void CheckDuplicateDamageToOuterParts(DamageInfo dinfo, Pawn pawn, float totalDamage, DamageResult result)
        {
            if (!dinfo.AllowDamagePropagation || !dinfo.Def.harmAllLayersUntilOutside || dinfo.HitPart.depth != BodyPartDepth.Inside)
            {
                return;
            }
            BodyPartRecord parent = dinfo.HitPart.parent;
            do
            {
                if (pawn.health.hediffSet.GetPartHealth(parent) != 0f && parent.coverageAbs > 0f)
                {
                    Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, parent), pawn);
                    hediff_Injury.Part = parent;
                    hediff_Injury.sourceDef = dinfo.Weapon;
                    hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
                    hediff_Injury.Severity = totalDamage;
                    if (hediff_Injury.Severity <= 0f)
                    {
                        hediff_Injury.Severity = 1f;
                    }
                    FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
                }
                if (parent.depth != BodyPartDepth.Outside)
                {
                    parent = parent.parent;
                    continue;
                }
                break;
            }
            while (parent != null);
        }

        private static bool IsHeadshot(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.InstantPermanentInjury)
            {
                return false;
            }
            if (dinfo.HitPart.groups.Contains(BodyPartGroupDefOf.FullHead))
            {
                return dinfo.Def.isRanged;
            }
            return false;
        }

        private BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.HitPart != null)
            {
                if (!pawn.health.hediffSet.GetNotMissingParts().Any((BodyPartRecord x) => x == dinfo.HitPart))
                {
                    return null;
                }
                return dinfo.HitPart;
            }
            BodyPartRecord bodyPartRecord = ChooseHitPart(dinfo, pawn);
            if (bodyPartRecord == null)
            {
                Log.Warning("ChooseHitPart returned null (any part).");
            }
            return bodyPartRecord;
        }

        protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth);
        }

        private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        {
            if (!pawn.Dead && !dinfo.InstantPermanentInjury && pawn.SpawnedOrAnyParentSpawned && dinfo.Def.ExternalViolenceFor(pawn))
            {
                LifeStageUtility.PlayNearestLifestageSound(pawn, (LifeStageAge lifeStage) => lifeStage.soundWounded, (GeneDef gene) => gene.soundWounded, (MutantDef mutantDef) => mutantDef.soundWounded);
            }
        }

        protected new float ReduceDamageToPreserveOutsideParts(float postArmorDamage, DamageInfo dinfo, Pawn pawn)
        {
            if (!ShouldReduceDamageToPreservePart(dinfo.HitPart))
            {
                return postArmorDamage;
            }
            float partHealth = pawn.health.hediffSet.GetPartHealth(dinfo.HitPart);
            if (postArmorDamage < partHealth)
            {
                return postArmorDamage;
            }
            float maxHealth = dinfo.HitPart.def.GetMaxHealth(pawn);
            float f = (postArmorDamage - partHealth) / maxHealth;
            if (Rand.Chance(def.overkillPctToDestroyPart.InverseLerpThroughRange(f)))
            {
                return postArmorDamage;
            }
            return postArmorDamage = partHealth - 1f;
        }

        public new static bool ShouldReduceDamageToPreservePart(BodyPartRecord bodyPart)
        {
            if (bodyPart.depth == BodyPartDepth.Outside)
            {
                return !bodyPart.IsCorePart;
            }
            return false;
        }

        //protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        //{
        //    List<BodyPartRecord> manipulationSources = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
        //        .Where(part => part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment)).ToList();

        //    if (manipulationSources.Any())
        //    {
        //        return manipulationSources.RandomElement();
        //    }
        //    else
        //    {
        //        return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside);
        //    }
        //}

    }


    public class DamageDefExtension : DefModExtension
    {
        public DamageArmorCategoryDef armorCategoryForMechanoids;
    }
}
