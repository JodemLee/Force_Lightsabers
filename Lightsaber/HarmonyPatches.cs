using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;
using static Verse.DamageWorker;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            harmonyPatch = new Harmony("Lightsabers_ForceThe");
            var type = typeof(HarmonyPatches);
            harmonyPatch.PatchAll();
        }

        public static Harmony harmonyPatch;
        private static CompCache compCache = new CompCache();
        private static bool meleeAnimationModActive;

        [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
        public static class LightsaberDrawer
        {
            [HarmonyPrefix]
            public static bool DrawEquipmentAimingPreFix(Thing eq, Vector3 drawLoc, float aimAngle)
            {
                var eqPrimary = eq;
                if (meleeAnimationModActive || eqPrimary == null || eqPrimary.def?.graphicData == null || Find.CameraDriver.CurrentZoom == CameraZoomRange.Furthest || !Find.CameraDriver.InViewOf(eqPrimary))
                {
                    return true;
                }

                var compLightsaberBlade = compCache.GetCachedComp(eqPrimary);
                var compLightsaberStance = eqPrimary.TryGetComp<Comp_LightsaberStance>();

                // If either component exists, do lightsaber drawing
                if (compLightsaberBlade != null || compLightsaberStance != null)
                {
                    // Get the pawn holding the equipment
                    var pawn = (eq.ParentHolder as Pawn_EquipmentTracker)?.pawn;

                    // Use blade component's rotation if available, otherwise use default behavior
                    float currentRotation = compLightsaberBlade?.CurrentRotation ?? 0f;

                    var flip = false;
                    var angle = aimAngle - 90f;
                    if (aimAngle > 20f && aimAngle < 160f)
                    {
                        angle += eq.def.equippedAngleOffset;
                    }
                    else if (aimAngle > 200f && aimAngle < 340f)
                    {
                        flip = true;
                        angle -= 180f;
                        angle -= eq.def.equippedAngleOffset;
                    }
                    else
                    {
                        angle += eq.def.equippedAngleOffset;
                    }

                    angle = currentRotation;

                    // Only do animation if we have a blade component
                    if (compLightsaberBlade != null && compLightsaberBlade.IsAnimatingNow)
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
                    Vector3 offset = compLightsaberBlade?.CurrentDrawOffset ?? Vector3.zero;
                    if (pawn != null && compLightsaberBlade != null)
                    {
                        if (pawn.Rotation == Rot4.West && currentRotation <= -150)
                        {
                            offset += new Vector3(1f, 0f, 0);
                        }

                        if (compLightsaberBlade != null && compLightsaberBlade.IsAnimatingNow)
                        {
                            float oscilation = Mathf.Lerp(-1f, 0f, 1f);
                            float verticalOffset = Mathf.Sin(oscilation);
                            offset.y = verticalOffset;
                        }
                    }

                    if (compLightsaberBlade != null)
                    {
                        LightsaberGraphicsUtil.DrawLightsaberGraphics(eqPrimary, drawLoc + offset, angle, flip, compLightsaberBlade);
                    }
                    else
                    {
                        Graphics.DrawMesh(eq.Graphic.MeshAt(Rot4.South),
                                          drawLoc,
                                          angle.ToQuat(),
                                          eq.Graphic.MatSingle,
                                          0);
                    }
                    return false;
                }
                return true;
            }

            // You could also extract the offset logic to a helper method like the other mod did
            private static Vector3 GetRotationBasedOffset(Rot4 rotation, Comp_LightsaberBlade bladeComp)
            {
                if (rotation == Rot4.West)
                {
                    return bladeComp.CurrentDrawOffset;
                }
                // Handle other rotations...
                return Vector3.zero;
            }
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
                try
                {
                    if (__instance == null) return true;
                    var targetThing = __instance.usedTarget.Thing;
                    if (!(targetThing is Pawn pawn)) return true;
                    var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance)
                        as Hediff_LightsaberDeflection;

                    if (hediff != null)
                    {
                        if (__instance.Launcher != pawn &&
                            hediff.ShouldDeflectProjectile(__instance))
                        {
                            hediff.DeflectProjectile(__instance);
                            return false;
                        }
                        return true;
                    }
                    var primaryEquipment = pawn.equipment?.Primary;
                    if (primaryEquipment != null)
                    {
                        var lightsaberComp = primaryEquipment.TryGetComp<Comp_LightsaberStance>();
                        if (lightsaberComp != null &&
                            LightsaberCombatUtility.ShouldDeflectProjectile(pawn, __instance))
                        {
                            LightsaberCombatUtility.RedirectProjectile(__instance, pawn);
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in Projectile.ImpactSomething patch: {ex}");
                    return true;
                }
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
                    Color color = colorableIngredient.TryGetComp<CompColorable>().Color;
                    if (colorableIngredient.def == LightsaberDefOf.Force_SyntheticCrystal)
                    {
                        ApplyHiltParts(__result, LightsaberDefOf.Force_SyntheticKyberCrystalHiltPart);
                    }
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

            private static IEnumerable<Thing> ApplyHiltParts(IEnumerable<Thing> products, HiltPartDef hiltPart)
            {
                foreach (var product in products)
                {
                    var comp = compCache.GetCachedComp(product);
                    if (comp != null)
                    {
                        comp.HiltManager.AddHiltPart(hiltPart);
                    }
                    yield return product;
                }
            }
        }


        [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
        public static class MakeSyntheticRecipeProducts_Patch
        {
            [HarmonyPostfix]
            public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, IBillGiver billGiver)
            {
                if (worker != null && worker.story != null && recipeDef.defName == "Force_CraftSyntheticCrystal")
                {
                    Color blendedColor = ColorUtility.GetSyntheticCrystalColor(worker);
                    return ApplyColorToProducts(__result, blendedColor);
                }

                return __result;
            }

            private static IEnumerable<Thing> ApplyColorToProducts(IEnumerable<Thing> products, Color color)
            {
                foreach (var product in products)
                {
                    var comp = product.TryGetComp<CompColorable>();
                    if (comp != null)
                    {
                        comp.SetColor(color);
                    }
                    yield return product;
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

        [HarmonyPatch(typeof(DamageWorker_LightsaberCut), "ApplyToPawn")]
        public static class Patch_DamageWorker_LightsaberParry
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
                return new DamageResult();
            }
        }

        [HarmonyPatch(typeof(PawnRenderTree), "SetupDynamicNodes")]
        public static class PawnRenderTree_Lightsaber_Patch
        {
            private static MethodInfo addChildMethod;
            private static MethodInfo shouldAddNodeMethod;

            static PawnRenderTree_Lightsaber_Patch()
            {
                addChildMethod = typeof(PawnRenderTree).GetMethod("AddChild", BindingFlags.NonPublic | BindingFlags.Instance);
                shouldAddNodeMethod = typeof(PawnRenderTree).GetMethod("ShouldAddNodeToTree", BindingFlags.Public | BindingFlags.Instance);

                if (addChildMethod == null || shouldAddNodeMethod == null)
                {
                    Log.Error("[Lightsabers] Failed to find required PawnRenderTree methods");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(PawnRenderTree __instance)
            {
                try
                {
                    if (__instance == null || __instance.pawn == null) return;
                    if (!UnityData.IsInMainThread)
                    {
                        Log.Warning("Tried to setup lightsaber render nodes from non-main thread. Aborting.");
                        return;
                    }

                    // Check if pawn has equipment and primary weapon
                    if (__instance.pawn.equipment?.Primary == null) return;

                    // Try to get lightsaber component
                    var lightsaberComp = __instance.pawn.equipment.Primary.TryGetComp<Comp_LightsaberBlade>();
                    if (lightsaberComp == null) return;

                    // Check if we have nodes to add
                    var nodesToAdd = lightsaberComp.activeRenderNodes;
                    if (nodesToAdd == null || nodesToAdd.Count == 0) return;

                    foreach (PawnRenderNode node in lightsaberComp.activeRenderNodes)
                    {
                        if (node != null && (bool)shouldAddNodeMethod.Invoke(__instance, new object[] { node.Props }))
                        {
                            addChildMethod.Invoke(__instance, new object[] { node, null });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in PawnRenderTree_Lightsaber_Patch: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(Comp_LightsaberBlade), "Notify_Unequipped")]
        public static class PawnRenderTree_RenderNodePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn pawn)
            {

                pawn.Drawer?.renderer?.renderTree?.EnsureInitialized(PawnRenderFlags.None);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }
}

