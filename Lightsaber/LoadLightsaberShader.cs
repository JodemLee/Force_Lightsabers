using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public static class LightsaberGlowShaderLoader
    {
        private static readonly string[] PlatformFolders = new string[]
        {
        "StandaloneWindows64",
        "StandaloneLinux64",
        "StandaloneOSX"
            // Add other platforms as needed
        };

        static LightsaberGlowShaderLoader()
        {
            LoadShader();
        }

        private static void LoadShader()
        {
            ModContentPack modPack = LoadedModManager.RunningModsListForReading
                .FirstOrDefault(mod => mod.PackageIdPlayerFacing.Contains("lee.theforce.lightsaber"));

            if (modPack == null)
            {
                Log.Error("[Lightsaber] Mod not found in running mod list");
                return;
            }

            // Try platform-specific folder first
            string bundlePath = FindPlatformSpecificBundle(modPack);

            // Fallback to root AssetBundles folder if platform-specific not found
            if (string.IsNullOrEmpty(bundlePath))
            {
                bundlePath = Path.Combine(modPack.RootDir, "AssetBundles", "lightsabershaderglow.assetbundle");
            }

            if (!File.Exists(bundlePath))
            {
                Log.Error($"[Lightsaber] AssetBundle not found at {bundlePath}");
                return;
            }

            LoadBundleAndShader(bundlePath);
        }

        private static string FindPlatformSpecificBundle(ModContentPack modPack)
        {
            string currentPlatform = GetCurrentPlatformFolderName();
            if (string.IsNullOrEmpty(currentPlatform))
            {
                return null;
            }

            string platformPath = Path.Combine(modPack.RootDir, "AssetBundles", currentPlatform, "lightsabershaderglow.assetbundle");
            return File.Exists(platformPath) ? platformPath : null;
        }

        private static string GetCurrentPlatformFolderName()
        {
            // Map Unity's Application.platform to your folder names
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return "StandaloneWindows64";
                case RuntimePlatform.LinuxPlayer:
                    return "StandaloneLinux64";
                case RuntimePlatform.OSXPlayer:
                    return "StandaloneOSX";
                // Add other platforms as needed
                default:
                    Log.Warning($"[Lightsaber] Unhandled platform: {Application.platform}");
                    return null;
            }
        }

        private static void LoadBundleAndShader(string bundlePath)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Log.Error($"[Lightsaber] Failed to load AssetBundle from {bundlePath}");
                return;
            }

            try
            {
                Shader customShader = bundle.LoadAsset<Shader>("LightsaberGlowShader");
                if (customShader == null)
                {
                    Log.Error("[Lightsaber] LightsaberGlowShader not found in bundle");
                    return;
                }

                ApplyShaderToDef(customShader);
            }
            finally
            {
                bundle.Unload(false); // Important: Unload the bundle when done
            }
        }

        private static void ApplyShaderToDef(Shader customShader)
        {
            ShaderTypeDef shaderDef = DefDatabase<ShaderTypeDef>.GetNamedSilentFail("Force_LightsaberGlow");
            if (shaderDef != null)
            {
                typeof(ShaderTypeDef).GetField("shaderInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(shaderDef, customShader);
                Log.Message("[Lightsaber] Successfully applied custom shader");
            }
            else
            {
                Log.Error("[Lightsaber] ShaderTypeDef 'Force_LightsaberGlow' not found");
            }
        }
    }

    public class LightsaberShaderDef : ShaderTypeDef
    {

    }
}