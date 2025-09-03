using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public static class LightsaberGraphicsUtil
    {
        // Constants for offsets
        private const float HiltYOffset = 0.001f;
        private const float BladeYOffset = 0f;

        // Cached values for performance
        public static Quaternion rotationCache;
        private static Mesh meshCache;
        private static Mesh meshFlipCache;
        private static float? previousAngle;

        // Cached offset
        private static readonly Vector3 bladeOffset = new Vector3(0f, BladeYOffset, 0f);
        private static readonly Vector3 hiltOffset = new Vector3(0f, HiltYOffset, 0f);

        // Cached Type references
        private static readonly Type smyhType = Type.GetType("ShowMeYourHands.ShowMeYourHandsMain, ShowMeYourHands");
        private static readonly Type tupleType = Type.GetType("System.Tuple`2[[UnityEngine.Vector3, UnityEngine.CoreModule],[System.Single, mscorlib]], mscorlib");

        // Cached FieldInfo
        private static FieldInfo weaponLocationsField;
        private static bool weaponLocationsFieldInitialized;
        private static readonly object weaponLocationsLock = new object();

        static LightsaberGraphicsUtil()
        {
            // Initialize cached field info
            InitializeWeaponLocationsField();
        }

        private static void InitializeWeaponLocationsField()
        {
            if (smyhType != null && !weaponLocationsFieldInitialized)
            {
                lock (weaponLocationsLock)
                {
                    if (!weaponLocationsFieldInitialized)
                    {
                        weaponLocationsField = smyhType.GetField("weaponLocations", BindingFlags.Public | BindingFlags.Static);
                        weaponLocationsFieldInitialized = true;
                    }
                }
            }
        }

        public static void DrawLightsaberGraphics(Thing eq, Vector3 drawLoc, float angle, bool flip, Comp_LightsaberBlade compLightsaberBlade)
        {
            // Null check for compLightsaberBlade
            if (compLightsaberBlade == null || compLightsaberBlade.parent == null)
            {
                return;
            }

            // Null check for bladeGraphic
            var bladeGraphic = compLightsaberBlade.BladeGraphic;
            if (bladeGraphic == null)
            {
                return;
            }

            compLightsaberBlade.UpdateScalingAndOffset();

            if (!previousAngle.HasValue || Math.Abs(angle - previousAngle.Value) > 0.01f)
            {
                rotationCache = Quaternion.AngleAxis(angle, Vector3.up);
                previousAngle = angle;
            }

            meshCache ??= MeshPool.plane10;
            meshFlipCache ??= MeshPool.plane10Flip;
            Mesh currentMesh = flip ? meshFlipCache : meshCache;
            Mesh bladeMesh = flip ? meshFlipCache : meshCache;


            Vector3 bladeDrawLoc = drawLoc + bladeOffset;
            Vector3 hiltDrawLoc = drawLoc + hiltOffset;

            try
            {
                if (smyhType != null && weaponLocationsField != null)
                {
                    var weaponLocations = weaponLocationsField.GetValue(null) as IDictionary;

                    if (weaponLocations != null && tupleType != null)
                    {
                        object tuple;

                        if (flip == true)
                        {
                            tuple = Activator.CreateInstance(tupleType, hiltDrawLoc, -angle);
                        }
                        else
                        {
                            tuple = Activator.CreateInstance(tupleType, hiltDrawLoc, angle);
                        }

                        weaponLocations[eq] = tuple;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ShowMeYourHands integration: {ex}");
            }

            if (compLightsaberBlade?.Wearer == null) return;
            var wearer = compLightsaberBlade.Wearer;
            MaterialPropertyBlock propertyBlock = Comp_LightsaberBlade.propertyBlock;
            compLightsaberBlade.SetShaderProperties();
            if (wearer != null && wearer.health?.hediffSet?.GetFirstHediffOfDef(LightsaberDefOf.Force_LightsaberShortCircuit) == null)
            {
                // Get the appropriate mesh for the current rotation
                Mesh bladeRenderMesh = bladeGraphic.MeshAt(wearer.Rotation);
                var bladeMatrix = Matrix4x4.TRS(
                    bladeDrawLoc,
                    flip ? Quaternion.AngleAxis(-angle, Vector3.up) : rotationCache,
                    flip ? new Vector3(-compLightsaberBlade.currentScaleForCore1AndBlade1.x,
                                      compLightsaberBlade.currentScaleForCore1AndBlade1.y,
                                      compLightsaberBlade.currentScaleForCore1AndBlade1.z)
                         : compLightsaberBlade.currentScaleForCore1AndBlade1
                );

                Graphics.DrawMesh(
                    bladeRenderMesh,
                    bladeMatrix,
                    bladeGraphic.MatSingle,
                    0,
                    null,
                    0,
                    propertyBlock
                );
            }

            // Draw hilt (unchanged)
            var hiltManager = compLightsaberBlade.HiltManager;
            if (hiltManager != null)
            {
                // Get or create hilt graphic if needed
                hiltManager.GetHiltGraphic();

                if (hiltManager.SelectedHilt?.graphicData?.Graphic != null)
                {
                    var hiltMaterial = hiltManager.SelectedHilt.graphicData.Graphic.MatSingle;
                    var hiltSize = new Vector3(
                        hiltManager.SelectedHilt.graphicData.Graphic.drawSize.x,
                        1f,
                        hiltManager.SelectedHilt.graphicData.Graphic.drawSize.x
                    );
                    var hiltMatrix = Matrix4x4.TRS(
                    hiltDrawLoc,
                    flip ? Quaternion.AngleAxis(-angle, Vector3.up) : rotationCache,
                    hiltSize);

                    Graphics.DrawMesh(
                        currentMesh,
                        hiltMatrix,
                        hiltMaterial,
                        0,
                        null,
                        0,
                        hiltManager.HiltMaterialPropertyBlock
                    );
                }
                else
                {
                    // Fallback to default hilt graphic
                    var hiltMaterial = compLightsaberBlade.parent.Graphic?.MatSingle;
                    var hiltMatrix = Matrix4x4.TRS(
                    hiltDrawLoc,
                    flip ? Quaternion.AngleAxis(-angle, Vector3.up) : rotationCache,
                    Vector3.one);
                    if (hiltMaterial != null)
                    {
                        var hiltSize = new Vector3(
                            compLightsaberBlade.parent.Graphic.drawSize.x,
                            1f,
                            compLightsaberBlade.parent.Graphic.drawSize.x
                        );
                        Graphics.DrawMesh(currentMesh, hiltMatrix, hiltMaterial, 0);
                    }
                }
            }
        }
    }

    public static class ColorUtility
    {
        public static List<Color> GetTraitColors(Pawn pawn)
        {
            List<Color> colors = new List<Color>();

            if (pawn?.story?.traits == null)
                return colors;

            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                var ext = trait.def.GetModExtension<ModExtension_TraitColor>();
                if (ext != null)
                {
                    Color degreeColor = ext.GetColorForDegree(trait.Degree);
                    if (degreeColor != Color.white)
                    {
                        colors.Add(degreeColor);
                    }
                }
            }

            return colors;
        }

        public static Color BlendColors(List<Color> colors)
        {
            if (colors.Count == 0) return Color.white;
            if (colors.Count == 1) return colors[0];

            float r = 0f, g = 0f, b = 0f, a = 0f;
            foreach (Color color in colors)
            {
                r += color.r;
                g += color.g;
                b += color.b;
                a += color.a;
            }

            int count = colors.Count;
            return new Color(r / count, g / count, b / count, a / count);
        }

        public static Color GetSyntheticCrystalColor(Pawn pawn)
        {
            if (pawn == null)
                return Color.white;

            List<Color> traitColors = GetTraitColors(pawn);
            return traitColors.Count > 0 ? BlendColors(traitColors) : Color.white;
        }
    }
}