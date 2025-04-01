using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public static class ShaderPropertyIDAddon
    {
        // Main Textures (Blade Textures)
        private static readonly string MainTex2Name = "_MainTex2"; // Blade Texture 2
        private static readonly string MainTexScaleName = "_MainTexScale1";
        private static readonly string MainTex2ScaleName = "_MainTexScale2";

        // Blade Colors
        private static readonly string Color1Name = "_Color"; // Blade Color 1
        private static readonly string Color2Name = "_ColorTwo"; // Blade Color 2

        // Blade Intensities
        private static readonly string BladeIntensity1Name = "_BladeIntensity1"; // Blade Intensity 1
        private static readonly string BladeIntensity2Name = "_BladeIntensity2"; // Blade Intensity 2

        // Core Properties
        private static readonly string CoreColor1Name = "_CoreColor1"; // Core Color 1
        private static readonly string CoreColor2Name = "_CoreColor2"; // Core Color 2
        private static readonly string CoreIntensity1Name = "_CoreIntensity1"; // Core Intensity 1
        private static readonly string CoreIntensity2Name = "_CoreIntensity2"; // Core Intensity 2
        private static readonly string CoreTex1Name = "_CoreTex1"; // Core Texture 1
        private static readonly string CoreTex2Name = "_CoreTex2"; // Core Texture 2

        // Glow Properties
        private static readonly string GlowIntensity1Name = "_GlowIntensity1"; // Glow Intensity 1
        private static readonly string GlowIntensity2Name = "_GlowIntensity2"; // Glow Intensity 2
        private static readonly string GlowSpeed1Name = "_GlowSpeed1"; // Glow Speed 1
        private static readonly string GlowSpeed2Name = "_GlowSpeed2"; // Glow Speed 2
        private static readonly string GlowTex1Name = "_GlowTex1"; // Glow Texture 1
        private static readonly string GlowTex2Name = "_GlowTex2"; // Glow Texture 2

        // Animation Properties
        private static readonly string NumFramesName = "_NumFrames"; // Number of frames
        private static readonly string AgeSecsName = "_AgeSecs"; // Animation age in seconds
        private static readonly string FramesPerSecName = "_FramesPerSec"; // Frames per second

        // Public Property IDs

        // Main Textures
        public static readonly int MainTex2 = Shader.PropertyToID(MainTex2Name); // Blade Texture 2 ID
        public static readonly int MainTexScale = Shader.PropertyToID(MainTexScaleName); // Blade Scale  ID
        public static readonly int MainTexScale2 = Shader.PropertyToID(MainTex2ScaleName); // Blade Scale  2 ID

        // Blade Colors
        public static readonly int Color1 = Shader.PropertyToID(Color1Name); // Blade Color 1 ID
        public static readonly int Color2 = Shader.PropertyToID(Color2Name); // Blade Color 2 ID

        // Blade Intensities
        public static readonly int BladeIntensity1 = Shader.PropertyToID(BladeIntensity1Name); // Blade Intensity 1 ID
        public static readonly int BladeIntensity2 = Shader.PropertyToID(BladeIntensity2Name); // Blade Intensity 2 ID

        // Core Properties
        public static readonly int CoreColor1 = Shader.PropertyToID(CoreColor1Name); // Core Color 1 ID
        public static readonly int CoreColor2 = Shader.PropertyToID(CoreColor2Name); // Core Color 2 ID
        public static readonly int CoreIntensity1 = Shader.PropertyToID(CoreIntensity1Name); // Core Intensity 1 ID
        public static readonly int CoreIntensity2 = Shader.PropertyToID(CoreIntensity2Name); // Core Intensity 2 ID
        public static readonly int CoreTex1 = Shader.PropertyToID(CoreTex1Name); // Core Texture 1 ID
        public static readonly int CoreTex2 = Shader.PropertyToID(CoreTex2Name); // Core Texture 2 ID

        // Glow Properties
        public static readonly int GlowIntensity1 = Shader.PropertyToID(GlowIntensity1Name); // Glow Intensity 1 ID
        public static readonly int GlowIntensity2 = Shader.PropertyToID(GlowIntensity2Name); // Glow Intensity 2 ID
        public static readonly int GlowSpeed1 = Shader.PropertyToID(GlowSpeed1Name); // Glow Speed 1 ID
        public static readonly int GlowSpeed2 = Shader.PropertyToID(GlowSpeed2Name); // Glow Speed 2 ID
        public static readonly int GlowTex1 = Shader.PropertyToID(GlowTex1Name); // Glow Texture 1 ID
        public static readonly int GlowTex2 = Shader.PropertyToID(GlowTex2Name); // Glow Texture 2 ID

        // Animation Properties
        public static readonly int NumFrames = Shader.PropertyToID(NumFramesName); // Number of frames ID
        public static readonly int AgeSecs = Shader.PropertyToID(AgeSecsName); // Animation age ID
        public static readonly int FramesPerSec = Shader.PropertyToID(FramesPerSecName); // Frames per second ID
    }



    public class CompCache
    {
        private readonly Dictionary<Thing, Comp_LightsaberBlade> cachedComps = new Dictionary<Thing, Comp_LightsaberBlade>();
        public Comp_LightsaberBlade GetCachedComp(Thing thing)
        {
            if (thing == null)
                return null;
            if (cachedComps.TryGetValue(thing, out var cachedComp))
            {
                return cachedComp;
            }
            var thingWithComps = thing as ThingWithComps;
            var comp = thingWithComps?.GetComp<Comp_LightsaberBlade>();

            if (comp != null)
            {
                cachedComps[thing] = comp;
            }

            return comp;
        }

        public void ClearCache(Thing thing)
        {
            // Remove the cache entry for this specific Thing
            if (thing != null)
            {
                cachedComps.Remove(thing);
            }
        }

        public void ClearAllCaches()
        {
            cachedComps.Clear();
        }
    }
}