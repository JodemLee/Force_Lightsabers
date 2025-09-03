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
        private static AssetBundle _bundleInt;
        private static Dictionary<string, Shader> _lookupShaders;
        private const string _rootPathUnlit = "Assets/Shader/";

        // thing specific (ideally lol)
        public static readonly Shader LightsaberGlowShader = LoadShader(Path.Combine(_rootPathUnlit, "LightsaberGlowShader.shader"));
        public static readonly Shader LightsaberGlowShaderSolid = LoadShader(Path.Combine(_rootPathUnlit, "LightsaberGlowShaderSolid.shader"));

        public static AssetBundle ForceBundle
        {
            get
            {
                if (_bundleInt != null) return _bundleInt;
                try
                {
                    _bundleInt = TheForceLightsaber_Mod.Lightsaber_Mod.MainBundle;
                    if (_bundleInt == null)
                    {
                        throw new Exception("MainBundle is null.");
                    }
                    return _bundleInt;
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to load AssetBundle. " +
                                  $"Exception: {ex.Message}");
                    return null;
                }
            }
        }

        private static Shader LoadShader(string shaderName)
        {
            _lookupShaders ??= new Dictionary<string, Shader>();
            try
            {
                if (!_lookupShaders.ContainsKey(shaderName))
                {
                    _lookupShaders[shaderName] = ForceBundle.LoadAsset<Shader>(shaderName);
                }

                Shader shader = _lookupShaders[shaderName];
                if (shader == null)
                {
                    throw new Exception($"Shader '{shaderName}' " +
                                        $"is null after loading.");
                }
                return shader;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to load shader: {shaderName}. " +
                              $"Exception: {ex.Message}");
                return ShaderDatabase.DefaultShader;
            }
        }
    }

    public class LightsaberShaderDef : ShaderTypeDef
    {

    }
}