using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public static class LightsaberCombatUtility
    {
        public static HediffDef ParryHediffDefName = LightsaberDefOf.Lightsaber_Stance;

        public const int MinDeflectionTicks = 300;
        public const int MaxDeflectionTicks = 1200;
        public const float DefaultDeflectionDistance = 0.3f;
        public const float ScorchMarkChance = 0.15f;

        public static bool CanParry(Pawn targetPawn, Pawn attacker)
        {
            if (!ForceLightsabers_ModSettings.lightsaberParryEnabled)
            {
                return false;
            }

            if (targetPawn == null || attacker == null ||
                targetPawn.skills == null || attacker.skills == null ||
                targetPawn.skills.GetSkill(SkillDefOf.Melee) == null ||
                attacker.skills.GetSkill(SkillDefOf.Melee) == null)
            {
                return false;
            }

            if (targetPawn.health.hediffSet.HasHediff(ParryHediffDefName))
            {
                Hediff targetStance = targetPawn.health.hediffSet.GetFirstHediffOfDef(ParryHediffDefName);
                Hediff attackerStance = attacker.health.hediffSet.GetFirstHediffOfDef(ParryHediffDefName);

                if (targetStance != null)
                {
                    DefStanceAngles stanceDef = targetPawn.equipment.Primary?.def.GetModExtension<DefStanceAngles>()
                                                ?? targetStance.def.GetModExtension<DefStanceAngles>();

                    if (stanceDef != null)
                    {
                        StanceData targetStanceData = stanceDef.GetStanceDataForSeverity(targetStance.Severity);

                        if (targetStanceData != null)
                        {
                            // Base parry chance from skill difference
                            float adjustedTargetMeleeSkill = GetAdjustedMeleeSkill(targetPawn);
                            float adjustedAttackerMeleeSkill = GetAdjustedMeleeSkill(attacker);
                            int skillDifference = (int)(adjustedTargetMeleeSkill - adjustedAttackerMeleeSkill);
                            float baseParryChance = Mathf.Clamp(0.05f * skillDifference, 0.05f, 0.95f);

                            // Apply stance's inherent parry chance modifier
                            float stanceParryModifier = targetStanceData.ParryChance;
                            float finalParryChance = baseParryChance * stanceParryModifier;

                            // Apply matchup bonuses if attacker also has a stance
                            if (attackerStance != null)
                            {
                                StanceData attackerStanceData = stanceDef.GetStanceDataForSeverity(attackerStance.Severity);

                                if (attackerStanceData != null)
                                {
                                    // Check for strong/weak matchups
                                    if (stanceDef.IsStrongAgainst(targetStanceData.StanceID, attackerStanceData.StanceID))
                                    {
                                        finalParryChance *= 1.3f; // 30% bonus for favorable matchup
                                    }
                                    else if (stanceDef.IsWeakAgainst(targetStanceData.StanceID, attackerStanceData.StanceID))
                                    {
                                        finalParryChance *= 0.7f; // 30% penalty for bad matchup
                                    }
                                }
                            }

                            // Final clamp to ensure reasonable values
                            finalParryChance = Mathf.Clamp(finalParryChance, 0.05f, 0.95f);

                            if (Rand.Value <= finalParryChance)
                            {
                                string stanceName = targetStanceData.ShortLabel ?? targetStanceData.StanceID;
                                string parryMessage = $"{stanceName}: {Math.Round(finalParryChance * 100, 1)}%";
                                MoteMaker.ThrowText(targetPawn.DrawPos + new Vector3(0.5f, 0, 0.5f),
                                                  targetPawn.Map,
                                                  parryMessage,
                                                  Color.yellow);
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                float adjustedTargetMeleeSkill = GetAdjustedMeleeSkill(targetPawn);
                float adjustedAttackerMeleeSkill = GetAdjustedMeleeSkill(attacker);
                int skillDifference = (int)(adjustedTargetMeleeSkill - adjustedAttackerMeleeSkill);
                float parryChance = Math.Min(0.95f, Math.Max(0.05f, 0.05f * skillDifference));

                if (Rand.Value <= parryChance)
                {
                    string parryChanceMessage = $"Parry Chance: {Math.Round(parryChance * 100, 2)}%";
                    MoteMaker.ThrowText(new Vector3((float)targetPawn.Position.x + 1f, targetPawn.Position.y, (float)targetPawn.Position.z + 1f), targetPawn.Map, parryChanceMessage, Color.white);
                    return true;
                }
            }

            return false;
        }

        public static float GetAdjustedMeleeSkill(Pawn pawn)
        {
            float manipulation = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
            int meleeSkill = pawn.skills.GetSkill(SkillDefOf.Melee).Level;
            return meleeSkill * manipulation * 600;
        }

        public static float CalculateParryBonus(Pawn targetPawn, Pawn attackerPawn)
        {
            if (targetPawn == null || attackerPawn == null)
                return 0f;

            Hediff targetStanceHediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(ParryHediffDefName);
            Hediff attackerStanceHediff = attackerPawn.health.hediffSet.GetFirstHediffOfDef(ParryHediffDefName);

            DefStanceAngles targetStanceMod = targetPawn.equipment.Primary?.def.GetModExtension<DefStanceAngles>()
                                              ?? targetStanceHediff.def.GetModExtension<DefStanceAngles>();

            DefStanceAngles attackerStanceMod = attackerPawn.equipment.Primary?.def.GetModExtension<DefStanceAngles>()
                                               ?? attackerStanceHediff.def.GetModExtension<DefStanceAngles>();

            if (targetStanceMod == null || attackerStanceMod == null)
                return 0f;

            StanceData targetStanceData = targetStanceMod.GetStanceDataForSeverity(targetStanceHediff.Severity);
            StanceData attackerStanceData = attackerStanceMod.GetStanceDataForSeverity(attackerStanceHediff.Severity);

            if (targetStanceData == null || attackerStanceData == null)
                return 0f;

            float parryBonus = 0f;

            if (targetStanceData.WeakAgainst?.Contains(attackerStanceData.StanceID) == true)
                parryBonus -= 0.2f;

            return parryBonus;
        }

        public static void TriggerWeaponRotationOnParry(Pawn caster, Pawn target)
        {
            var casterBlade = caster.equipment?.Primary?.TryGetComp<Comp_LightsaberBlade>();
            var targetBlade = target.equipment?.Primary?.TryGetComp<Comp_LightsaberBlade>();

            if (casterBlade != null && targetBlade != null)
            {
                float casterSkill = caster.skills.GetSkill(SkillDefOf.Melee).Level;
                float targetSkill = target.skills.GetSkill(SkillDefOf.Melee).Level;
                float angleToTarget = (caster.Position.ToVector3() - target.Position.ToVector3()).AngleFlat();
                float angleDifference = Mathf.DeltaAngle(angleToTarget, casterBlade.CurrentRotation);

                float baseAnimationTicks = Mathf.Clamp(
                    500f / (angleDifference + 1),
                    300,
                    1200
                );

                float casterSkillAdjustment = Mathf.Lerp(1.0f, 0.8f, casterSkill / 20);
                float targetSkillAdjustment = Mathf.Lerp(1.0f, 0.8f, targetSkill / 20);
                float randomVariation = UnityEngine.Random.Range(-0.1f, 0.1f);
                float finalCasterAnimationTicks = baseAnimationTicks * casterSkillAdjustment * (1 + randomVariation);
                float finalTargetAnimationTicks = baseAnimationTicks * targetSkillAdjustment * (1 + randomVariation);

                casterBlade.AnimationDeflectionTicks += (int)finalCasterAnimationTicks;
                targetBlade.AnimationDeflectionTicks += (int)(finalTargetAnimationTicks + 100);
            }
        }


        public static void DestroyLimb(Pawn CasterPawn, Pawn target, BodyPartRecord limb)
        {
            // Get the weapon equipped by the pawn.
            ThingWithComps weapon = CasterPawn.equipment?.Primary;
            Tool selectedTool = SelectWeightedTool(weapon.def.tools);
            int damageAmount = CalculateDamageToDestroyLimb(target, limb);
            ToolCapacityDef mainCapacity = selectedTool.capacities.FirstOrDefault();
            DamageDef damageType = mainCapacity?.Maneuvers.FirstOrDefault().verb.meleeDamageDef ?? DamageDefOf.Cut;
            var damageInfo = new DamageInfo(damageType, damageAmount, selectedTool.armorPenetration, -1, CasterPawn, limb, weapon.def);
            target.TakeDamage(damageInfo);
        }

        public static int CalculateDamageToDestroyLimb(Pawn target, BodyPartRecord limb)
        {
            float partHealth = target.health.hediffSet.GetPartHealth(limb);
            return (int)Math.Ceiling(partHealth) * 2;
        }


        public static Tool SelectWeightedTool(List<Tool> tools)
        {
            float totalWeight = tools.Sum(tool => tool.chanceFactor);
            float randomPoint = Rand.Value * totalWeight;

            float cumulativeWeight = 0f;
            foreach (var tool in tools)
            {
                cumulativeWeight += tool.chanceFactor;
                if (randomPoint <= cumulativeWeight)
                {
                    return tool;
                }
            }
            return tools.Last(); // Fallback in case of rounding errors
        }

        #region Projectile Deflection
        public static bool ShouldDeflectProjectile(Pawn pawn, Projectile projectile)
        {
            if (!IsProjectileDeflectable(projectile)) return false;
            CanGainEntropy(pawn, CalculateEntropyGain(projectile));
            float deflectionSkillChance = pawn.GetStatValue(LightsaberDefOf.Force_Lightsaber_Deflection);
            if (!pawn.HasPsylink)
            {
                deflectionSkillChance = deflectionSkillChance / 2f;
            }
            return CalculateDeflectionChance(deflectionSkillChance);
        }

        public static bool IsProjectileDeflectable(Projectile projectile)
        {
            if (!ForceLightsabers_ModSettings.deflectableProjectileHashes.Contains(projectile.def.shortHash) &&
                ForceLightsabers_ModSettings.projectileDeflectionSelector)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"Projectile {projectile.def.label} (shortHash: {projectile.def.shortHash}) is not deflectable.");
                }
                return false;
            }
            return true;
        }

        public static bool CalculateDeflectionChance(float deflectionSkillChance)
        {
            float deflection = deflectionSkillChance * ForceLightsabers_ModSettings.deflectionMultiplier;
            return deflection >= Rand.Range(0f, 1f);
        }

        public static float CalculateDeflectionTicks(float speed, float angleDifference, int skillLevel)
        {
            float skillFactor = Mathf.Lerp(1.2f, 0.8f, skillLevel / 20f);
            return (5000f / (speed * (angleDifference + 1))) * skillFactor + Rand.Range(-100, 100);
        }

        public static void RedirectProjectile(Projectile projectile, Pawn deflector, float precisionFactor = 1f)
        {
            int meleeSkill = deflector.skills.GetSkill(SkillDefOf.Melee).Level;
            float basePrecisionFactor = Mathf.Lerp(50.0f, 0.1f, meleeSkill / 20f) * precisionFactor;
            float speedFactor = projectile.def.projectile.speed / 100f;
            float finalPrecisionFactor = basePrecisionFactor * speedFactor;
            
            Vector3 launcherPosition = projectile.Launcher.DrawPos;
            Vector3 projectileDirection = (projectile.ExactPosition - launcherPosition).normalized;
            float redirectionDistance = Mathf.Lerp(10f, 2f, meleeSkill / 20f);

            Vector3 randomOffset = new Vector3(
                Rand.Range(-finalPrecisionFactor, finalPrecisionFactor),
                0,
                Rand.Range(-finalPrecisionFactor, finalPrecisionFactor)
            );

            Vector3 targetPosition = launcherPosition + (projectileDirection * redirectionDistance) + randomOffset;

            if (!targetPosition.ToIntVec3().InBounds(deflector.Map))
                targetPosition = launcherPosition + projectileDirection * redirectionDistance;

            if (meleeSkill >= 20)
                targetPosition = launcherPosition;
            TriggerDeflectionEffect(projectile);
            projectile.Launch(
                deflector,
                targetPosition.ToIntVec3(),
                projectile.Launcher,
                ProjectileHitFlags.All,
                false,
                projectile
            );

            float xpGained = CalculateMeleeXP(projectile, deflector, redirectionDistance);
            deflector.skills.Learn(SkillDefOf.Melee, xpGained);
            CreateScorchMark(deflector);
        }

        public static float CalculateMeleeXP(Projectile projectile, Pawn deflector, float redirectionDistance)
        {
            float baseXP = 100f;
            float skillMultiplier = Mathf.Lerp(1.5f, 0.5f, deflector.skills.GetSkill(SkillDefOf.Melee).Level / 20f);
            return baseXP * skillMultiplier;
        }

        public static float CalculateEntropyGain(Projectile projectile)
        {
            return projectile.def.projectile.speed / 20;
        }

        public static bool CanGainEntropy(Pawn pawn, float entropyGain)
        {
            if (!pawn.HasPsylink) return false;
            return pawn.psychicEntropy.EntropyValue + entropyGain < pawn.psychicEntropy.MaxEntropy * 0.99f;
        }
        #endregion

        #region Visual Effects
        public static void TriggerDeflectionEffect(Projectile projectile)
        {
            var effecter = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
            try
            {
                effecter.Trigger(new TargetInfo(projectile.Position, projectile.Map, true), TargetInfo.Invalid);
            }
            finally
            {
                effecter.Cleanup();
            }
        }

        public static void CreateDeflectionFleck(Comp_LightsaberBlade lightsaber, Vector3 location, float angle, Map map)
        {
            if (lightsaber.Props.Fleck is FleckDef fleckDef)
            {
                FleckCreationData fleckData = FleckMaker.GetDataStatic(location, map, fleckDef);
                fleckData.rotation = angle;
                fleckData.scale = Rand.Range(0.8f, 1.2f);
                map.flecks.CreateFleck(fleckData);
            }
        }

        public static void CreateScorchMark(Pawn pawn)
        {
            if (Rand.Value >= ScorchMarkChance) return;

            if (FleckDefOf.MicroSparks is FleckDef fleckDef)
            {
                var validCells = GenAdj.CellsAdjacent8Way(pawn).Where(cell => cell.InBounds(pawn.Map) && cell.Walkable(pawn.Map));
                if (validCells.TryRandomElement(out var scorchCell))
                {
                    FleckCreationData fleckData = FleckMaker.GetDataStatic(scorchCell.ToVector3(), pawn.Map, fleckDef);
                    fleckData.rotation = Rand.Range(0f, 360f);
                    pawn.Map.flecks.CreateFleck(fleckData);
                }
            }
        }

        #endregion
    } 
}

