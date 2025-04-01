using System;
using System.Collections.Generic;
using System.IO;
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
        private static Quaternion rotationCache;
        private static Mesh meshCache;
        private static Mesh meshFlipCache;

        // Previous values to track changes and avoid unnecessary recalculations
        private static float? previousAngle;

        // Cached offset
        private static readonly Vector3 bladeOffset = new Vector3(0f, BladeYOffset, 0f);
        private static readonly Vector3 hiltOffset = new Vector3(0f, HiltYOffset, 0f);

        public static void DrawLightsaberGraphics(Thing eq, Vector3 drawLoc, float angle, bool flip, Comp_LightsaberBlade compLightsaberBlade)
        {
            // Null check for compLightsaberBlade
            if (compLightsaberBlade == null)
            {
                return;
            }

            // Null check for bladeGraphic
            var bladeGraphic = compLightsaberBlade.BladeGraphic;
            if (bladeGraphic == null)
            {
                return;
            }

            // Update scaling and offset
            compLightsaberBlade.UpdateScalingAndOffset();

            // Update rotation cache if angle has changed
            if (!previousAngle.HasValue || Math.Abs(angle - previousAngle.Value) > 0.01f)
            {
                rotationCache = Quaternion.AngleAxis(angle, Vector3.up);
                previousAngle = angle;
            }

            // Initialize meshes if not already initialized
            meshCache = meshCache ?? MeshPool.plane10;
            meshFlipCache = meshFlipCache ?? MeshPool.plane10Flip;
            Mesh currentMesh = flip ? meshFlipCache : meshCache;

            // Calculate draw locations
            Vector3 bladeDrawLoc = drawLoc + bladeOffset;
            Vector3 hiltDrawLoc = drawLoc + hiltOffset;

            // Draw blade
            var bladeMatrix = Matrix4x4.TRS(bladeDrawLoc, rotationCache, compLightsaberBlade.currentScaleForCore1AndBlade1);
            Graphics.DrawMesh(
                bladeGraphic.MeshAt(Rot4.South),
                bladeMatrix,
                bladeGraphic.MatSingle,
                0,
                null,
                0,
                compLightsaberBlade.propertyBlock
            );

            // Draw hilt
            var hiltManager = compLightsaberBlade.HiltManager;
            if (hiltManager?.SelectedHilt?.graphicData != null)
            {
                // Update the hilt graphic if necessary
                hiltManager.GetHiltGraphic();
                var hiltMaterial = hiltManager.SelectedHilt.graphicData.Graphic.MatSingle;
                var hiltMatrix = Matrix4x4.TRS(hiltDrawLoc, rotationCache, new Vector3(hiltManager.SelectedHilt.graphicData.Graphic.drawSize.x, 1f, hiltManager.SelectedHilt.graphicData.Graphic.drawSize.x));
                Graphics.DrawMesh(currentMesh, hiltMatrix, hiltMaterial, 0, null, 0, hiltManager.HiltMaterialPropertyBlock);
            }
        }
    }
}