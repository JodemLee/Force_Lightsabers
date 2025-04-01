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
        static LightsaberGlowShaderLoader()
        {
            LoadShader();
        }

        private static void LoadShader()
        {
            // Find the current mod folder dynamically
            ModContentPack modPack = LoadedModManager.RunningModsListForReading
                .FirstOrDefault(mod => mod.PackageIdPlayerFacing.Contains("lee.theforce.lightsaber"));

            if (modPack == null)
            {
                return;
            }

            // Corrected path: Ensure it includes the full filename with .assetbundle extension
            string bundlePath = Path.Combine(modPack.RootDir, "AssetBundles", "lightsabershaderglow.assetbundle");
            if (!File.Exists(bundlePath))
            {
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                return;
            }

            Shader customShader = bundle.LoadAsset<Shader>("LightsaberGlowShader");
            if (customShader == null)
            {
                return;
            }

            // Assign the shader to ShaderTypeDef manually
            ShaderTypeDef shaderDef = DefDatabase<ShaderTypeDef>.GetNamedSilentFail("Force_LightsaberGlow");
            if (shaderDef != null)
            {
                typeof(ShaderTypeDef).GetField("shaderInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(shaderDef, customShader);
            }
        }
    }

    public class LightsaberShaderDef : ShaderTypeDef
    {

    }
}