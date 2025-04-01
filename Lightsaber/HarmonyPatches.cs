using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static Verse.DamageWorker;
using System.Linq;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            harmonyPatch = new Harmony("Lightsabers_ForceThe");
            var type = typeof(HarmonyPatches);
            harmonyPatch.Patch(AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming)),
               prefix: new HarmonyMethod(type, nameof(DrawEquipmentAimingPreFix)));
            harmonyPatch.PatchAll();
        }

        public static Harmony harmonyPatch;
        private static CompCache compCache = new CompCache();
        private static bool meleeAnimationModActive;

        public static bool DrawEquipmentAimingPreFix(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            var eqPrimary = eq;
            if (meleeAnimationModActive || eqPrimary == null || eqPrimary.def?.graphicData == null)
            {
                return true;
            }

            var compLightsaberBlade = compCache.GetCachedComp(eqPrimary);
            if (compLightsaberBlade != null)
            {
                bool flip = false;
                float angle = aimAngle - 90f;

                if (aimAngle > 20f && aimAngle < 160f)
                {
                    angle += compLightsaberBlade.CurrentRotation;
                }
                else if (aimAngle > 200f && aimAngle < 340f)
                {
                    flip = false;
                    angle -= 180f + compLightsaberBlade.CurrentRotation;
                }
                else
                {
                    angle += compLightsaberBlade.CurrentRotation;
                }
                angle = compLightsaberBlade.CurrentRotation;

                if (compLightsaberBlade.IsAnimatingNow)
                {
                    float animationTicks = compLightsaberBlade.AnimationDeflectionTicks;
                    if (!Find.TickManager.Paused && compLightsaberBlade.IsAnimatingNow)
                        compLightsaberBlade.AnimationDeflectionTicks -= 20;
                    float targetAngle = compLightsaberBlade.lastInterceptAngle;
                    angle = Mathf.Lerp(angle, targetAngle, 0.1f);

                    if (animationTicks > 0)
                    {
                        if (flip)
                            angle -= (animationTicks + 1) / 2;
                        else
                            angle += (animationTicks + 1) / 2;
                    }
                }
                angle %= 360f;
                Vector3 offset = compLightsaberBlade.CurrentDrawOffset;
                LightsaberGraphicsUtil.DrawLightsaberGraphics(eqPrimary, drawLoc + offset, angle, flip, compLightsaberBlade);

                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
        public static class Pawn_DraftedPatch
        {
            public static void Postfix(Pawn_DraftController __instance, bool value)
            {
                if (value) return;
                var pawn = __instance?.pawn;
                if (pawn == null || pawn.equipment == null)
                {
                    return;
                }
                var primaryEquipment = pawn.equipment.Primary;
                if (primaryEquipment == null)
                {
                    return;
                }
                var lightsabercomp = compCache.GetCachedComp(primaryEquipment);
                if (lightsabercomp != null)
                {
                    lightsabercomp.ResetToZero();
                }
                else return;
            }
        }

        [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.CarryWeaponOpenly))]
        internal static class PawnRenderUtility_CarryWeaponOpenly_Postfix
        {
            [HarmonyPostfix]
            static void HideLightsaberWhenThrown(ref bool __result, Pawn pawn)
            {
                if (!__result) return;
                var primaryEquipment = pawn.equipment?.Primary;
                if (primaryEquipment == null) return;
                var compLightsaberBlade = compCache.GetCachedComp(primaryEquipment);
                if (compLightsaberBlade?.IsThrowingWeapon == true)
                {
                    compLightsaberBlade.ResetToZero();
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.CarryWeaponOpenly))]
        internal static class PawnRenderUtility_CarryWeaponOpenly_PostfixIgnition
        {
            [HarmonyPostfix]
            static void IgniteLightsaberWhenDeflecting(ref bool __result, Pawn pawn)
            {
                if (__result) return;
                var primaryEquipment = pawn.equipment?.Primary;
                if (primaryEquipment == null) return;
                var compLightsaberBlade = compCache.GetCachedComp(primaryEquipment);
                if (compLightsaberBlade?.IsAnimatingNow == true)
                {
                    compLightsaberBlade.ResetToZero();
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_EquipmentTracker), "GetGizmos")]
        public static class Pawn_EquipmentTracker_GetGizmos_Patch
        {
            [HarmonyPostfix]
            public static void GetGizmosPostfix(Pawn_EquipmentTracker __instance, ref IEnumerable<Gizmo> __result)
            {
                var lightsaberComp = compCache.GetCachedComp(__instance.Primary);
                if (lightsaberComp != null && __instance.pawn.Faction == Faction.OfPlayer)
                {
                    __result = __result?.Concat(lightsaberComp.EquippedGizmos()) ?? lightsaberComp.EquippedGizmos();
                }
            }
        }

        [HarmonyPatch(typeof(Projectile), "ImpactSomething")]
        public static class Patch_Projectile_ImpactSomething
        {
            public static bool Prefix(Projectile __instance)
            {
                if (__instance.usedTarget.Thing is Pawn pawn)
                {
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance) is Hediff_LightsaberDeflection hediff)
                    {
                        if (hediff.ShouldDeflectProjectile(__instance) && __instance.Launcher != pawn)
                        {
                            hediff.DeflectProjectile(__instance);
                            return false;
                        }
                    }
                    else if (pawn.equipment?.Primary?.TryGetComp<Comp_LightsaberStance>() is Comp_LightsaberStance lightsaberComp)
                    {
                        // You might want to add a ShouldDeflect check here too
                        if (LightsaberCombatUtility.ShouldDeflectProjectile(pawn, __instance))
                        {
                            LightsaberCombatUtility.RedirectProjectile(__instance, pawn);
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Mineable), "TrySpawnYield", new Type[] { typeof(Map), typeof(bool), typeof(Pawn) })]
        public static class Patch_Thing_TrySpawnYield
        {
            [HarmonyPostfix]
            public static void Postfix(Mineable __instance, Map map)
            {

                var colorCrystalComp = __instance.TryGetComp<CompColorCrystal>();
                if (colorCrystalComp != null)
                {
                    IntVec3 position = __instance.Position;
                    foreach (Thing thing in position.GetThingList(map))
                    {
                        var itemColorable = thing.TryGetComp<CompColorable>();
                        if (itemColorable != null)
                        {
                            itemColorable.SetColor(colorCrystalComp.parent.DrawColor);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
        public static class MakeRecipeProducts_Patch
        {
            [HarmonyPostfix]
            public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, IBillGiver billGiver)
            {
                // Find the first ingredient with a CompColorable component
                Thing colorableIngredient = ingredients.Find(ingredient => ingredient.TryGetComp<CompColorable>() != null);

                if (colorableIngredient != null && recipeDef.useIngredientsForColor)
                {
                    // Get the color from the CompColorable component
                    Color color = colorableIngredient.TryGetComp<CompColorable>().Color;
                    return ApplyKyberCrystalColor(__result, color);
                }

                return __result;
            }

            private static IEnumerable<Thing> ApplyKyberCrystalColor(IEnumerable<Thing> products, Color crystalColor)
            {
                foreach (var product in products)
                {
                    var comp = compCache.GetCachedComp(product);
                    if (comp != null)
                    {
                        comp.bladeColor = crystalColor;
                        comp.bladeColor2 = crystalColor;
                        comp.SetShaderProperties();
                    }
                    yield return product;
                }
            }
        }

        [HarmonyPatch(typeof(Thing), "Notify_ColorChanged")]
        public static class Patch_ThingWithComps_Notify_ColorChanged
        {
            [HarmonyPostfix]
            public static void Postfix(ThingWithComps __instance)
            {
                // Check if the ThingWithComps has CompGlowerOptions
                var glowerComp = __instance.GetComp<CompGlower_Options>();
                if (glowerComp != null)
                {
                    // Update the glow color based on the parent’s DrawColor
                    ColorInt newGlowColor = new ColorInt(__instance.DrawColor);
                    glowerComp.UpdateGlowerColor(newGlowColor);
                }
            }
        }

        [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyToPawn")]
        public static class Patch_DamageWorker_AddInjury
        {
            public static bool Prefix(DamageInfo dinfo, Pawn pawn, ref DamageResult __result)
            {
                if (pawn == null || dinfo.Weapon == null || dinfo.Instigator == null)
                {
                    return true;
                }
                if (!dinfo.Weapon.IsMeleeWeapon)
                {
                    return true;
                }
                if (LightsaberCombatUtility.CanParry(pawn, dinfo.Instigator as Pawn))
                {
                    __result = HandleParry(dinfo, pawn);
                    return false;
                }
                return true;
            }

            private static DamageResult HandleParry(DamageInfo dinfo, Pawn pawn)
            {
                Effecter effecter = new Effecter(EffecterDefOf.Deflect_General);
                effecter.Trigger(new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
                effecter.Cleanup();
                return new DamageResult();
            }
        }
    }    
}
