using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lightsaber
{
    public class Comp_LightsaberBlade : ThingComp
    {
        #region Fields and Properties

        // Blade Graphics and Colors
        public Graphic bladeGraphic; // Single field for blade graphic
        public Color bladeColor = Color.white;
        public Color coreColor = Color.white;
        public Color bladeColor2 = Color.white;
        public Color coreColor2 = Color.white;

        // Blade Lengths
        public float bladeLength = 1;
        public float bladeLength2 = 1;
        public float minBladeLength;
        public float maxBladeLength;

        // Animation and Rotation
        public bool IsThrowingWeapon = false;
        public float lastInterceptAngle;
        public float vibrationrate;
        public float vibrationrate2;
        private int animationDeflectionTicks;
        private float stanceRotation;
        private Vector3 drawOffset;

        public Vector3 currentDrawOffset;
        public Vector3 targetDrawOffset;
        public Vector3 currentScaleForCore1AndBlade1 = Vector3.zero;
        public Vector3 currentScaleForCore2AndBlade2 = Vector3.zero;
        public Vector3 targetScaleForCore1AndBlade1;
        public Vector3 targetScaleForCore2AndBlade2;

        public bool colorsInitialized; // Flag to track if colors have been initialized
        private bool _graphicsInitialized; // Flag to track if graphics have been initialized

        // Hilt Management
        private HiltManager _hiltManager = new HiltManager();

        // Sound and Effects
        public List<SoundDef> lightsaberSound => Props.lightsaberSound ?? new List<SoundDef>();
        public SoundDef selectedSoundEffect;
        public int selectedSoundIndex = 0;
        public FleckDef Fleck => Props.Fleck;

        // Utility
        private const int TicksPerGlowerUpdate = 60;
        private float scaleTimer;
        public MaterialPropertyBlock propertyBlock;
        public bool isFlipped = false;

        // Properties
        public float LastInterceptAngle
        {
            get => lastInterceptAngle;
            set => lastInterceptAngle = value;
        }

        public int AnimationDeflectionTicks
        {
            get => animationDeflectionTicks;
            set => animationDeflectionTicks = value;
        }

        public Pawn Wearer
        {
            get
            {
                return parent?.ParentHolder?.ParentHolder as Pawn;
            }
        }

        public bool IsAnimatingNow => animationDeflectionTicks > 0;
        public float CurrentRotation => stanceRotation;
        public Vector3 CurrentDrawOffset => drawOffset;
        public CompProperties_LightsaberBlade Props => (CompProperties_LightsaberBlade)props;

        public HiltManager HiltManager => _hiltManager;



        #endregion

        #region Initialization and Setup

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            // Initialize blade lengths
            bladeLength = Rand.Range(Props.minBladeLength1, Props.maxBladeLength1);
            bladeLength2 = Rand.Range(Props.minBladeLength2, Props.maxBladeLength2);
            propertyBlock = new MaterialPropertyBlock();

            if (props is CompProperties_LightsaberBlade lightsaberProps)
            {
                _hiltManager.AvailableHilts = lightsaberProps.availableHiltGraphics;
            }

            if (lightsaberSound != null && lightsaberSound.Count > 0)
            {
                int randomIndex = Rand.Range(0, lightsaberSound.Count);
                selectedSoundEffect = lightsaberSound[randomIndex];
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (!colorsInitialized)
            {
                InitializeColors();
            }

            targetScaleForCore1AndBlade1 = new Vector3(bladeLength, 1f, bladeLength);
            targetScaleForCore2AndBlade2 = new Vector3(bladeLength2, 1f, bladeLength2);
        }

        public Graphic BladeGraphic => GetOrCreateGraphic(ref bladeGraphic, Props?.bladeGraphicData, bladeColor, Props?.bladeGraphicData?.Graphic?.Shader ?? null);
        

        public Graphic GetOrCreateGraphic(ref Graphic graphicField, GraphicData graphicData, Color color, Shader shaderOverride)
        {
            if (graphicField == null && graphicData != null)
            {
                var newColor = color == Color.white ? parent.DrawColor : color;
                var shader = graphicData.Graphic.Shader ?? shaderOverride;
                graphicField = graphicData.Graphic.GetColoredVersion(shader, newColor, newColor);
                SetShaderProperties();
            }
            return graphicField;
        }

        public void SetShaderProperties()
        {
            if (bladeGraphic == null) return;

            Material bladeMaterial = BladeGraphic.MatSingle;
            if (bladeMaterial == null) return;

            propertyBlock.Clear();

            propertyBlock.SetColor(ShaderPropertyIDAddon.CoreColor1, coreColor);
            propertyBlock.SetColor(ShaderPropertyIDAddon.Color1, bladeColor);
            propertyBlock.SetColor(ShaderPropertyIDAddon.Color2, bladeColor2);
            propertyBlock.SetColor(ShaderPropertyIDAddon.CoreColor2, coreColor2);
            propertyBlock.SetFloat(ShaderPropertyIDAddon.MainTexScale, bladeLength);
            propertyBlock.SetFloat(ShaderPropertyIDAddon.MainTexScale2, bladeLength2);
            propertyBlock.SetFloat(ShaderPropertyIDAddon.GlowSpeed1, vibrationrate);
            propertyBlock.SetFloat(ShaderPropertyIDAddon.GlowSpeed2, vibrationrate2);

            if (Props.bladeGraphicData?.shaderParameters != null)
            {
                foreach (var param in Props.bladeGraphicData.shaderParameters)
                {
                    param.Apply(bladeMaterial);
                }
            }
        }

        private void InitializeColors()
        {
            var pawn = Wearer;
            if (pawn != null)
            {
                var modExt = pawn.kindDef.GetModExtension<ModExtension_LightsaberPresets>();
                if (modExt != null)
                {
                    SetColors(modExt);
                }
                else
                {
                    SetColors();
                }
            }
            else
            {
                SetColors();
            }
            colorsInitialized = true; // Mark colors as initialized
            isFlipped = Rand.Chance(0.5f);
            parent.Notify_ColorChanged();
        }

        private void SetColors(ModExtension_LightsaberPresets modExt = null)
        {
            // First set up hilt parts if we have preferences
            if (modExt != null && modExt.preferredHiltParts != null && modExt.preferredHiltParts.Count > 0)
            {
                var allHiltParts = DefDatabase<HiltPartDef>.AllDefsListForReading;
                var validPreferredParts = modExt.preferredHiltParts
                    .Where(part => part != null && allHiltParts.Contains(part))
                    .Distinct()
                    .ToList();

                if (!validPreferredParts.Any())
                {
                    validPreferredParts = allHiltParts;
                }

                HiltManager.SelectedHiltParts = Enum.GetValues(typeof(HiltPartCategory))
                    .Cast<HiltPartCategory>()
                    .Select(category =>
                    {
                        var preferredForCategory = validPreferredParts
                            .Where(def => def.category == category)
                            .ToList();

                        if (preferredForCategory.Count > 0 && Rand.Chance(0.9f))
                        {
                            return preferredForCategory.RandomElementByWeight(def => def.commonality * 2f);
                        }

                        var allCategoryParts = allHiltParts
                            .Where(def => def.category == category)
                            .ToList();

                        return allCategoryParts.RandomElementByWeight(def =>
                            validPreferredParts.Contains(def) ? def.commonality * 1.5f : def.commonality);
                    })
                    .Where(part => part != null)
                    .ToList();
            }

            // Get crystal color after parts are set
            HiltPartDef crystalPart = HiltManager.GetHiltPartByCategory(HiltPartCategory.Crystal);
            Color? crystalColor = crystalPart?.colorGenerator?.NewRandomizedColor();

            // Set blade color with priority: ModExtension > Crystal Part > Random
            if (modExt?.bladeColors != null && modExt.bladeColors.Any())
            {
                bladeColor = modExt.bladeColors.RandomElement();
            }
            else if (crystalColor.HasValue)
            {
                bladeColor = crystalColor.Value;
            }
            else
            {
                bladeColor = GetRandomRGBColor();
            }

            // Set core color with priority: ModExtension > White/Black
            if (modExt?.coreColors != null && modExt.coreColors.Any())
            {
                coreColor = modExt.coreColors.RandomElement();
            }
            else
            {
                coreColor = GetBlackOrWhiteCore();
            }

            // Set secondary colors to match primary
            bladeColor2 = bladeColor;
            coreColor2 = coreColor;

            // Set hilt colors
            if (modExt != null)
            {
                HiltManager.HiltColorOne = StuffColorUtility.GetRandomColorFromStuffCategories(modExt.validStuffCategoriesHiltColorOne);
                HiltManager.HiltColorTwo = StuffColorUtility.GetRandomColorFromStuffCategories(modExt.validStuffCategoriesHiltColorTwo);
            }
            else
            {
                HiltManager.HiltColorOne = GetRandomRGBColor();
                HiltManager.HiltColorTwo = GetRandomRGBColor();
            }

            // Handle preferred hilts
            if (modExt != null && modExt.preferredHilts != null && Props.availableHiltGraphics != null)
            {
                var matchingHilts = modExt.preferredHilts
                    .Where(hilt => Props.availableHiltGraphics.Contains(hilt))
                    .ToList();

                if (matchingHilts.Any())
                {
                    HiltManager.SelectedHilt = matchingHilts.RandomElement();
                }
            }

            // Fallback hilt selection
            if (HiltManager.SelectedHilt == null && Props.availableHiltGraphics != null && Props.availableHiltGraphics.Any())
            {
                HiltManager.SelectedHilt = Props.availableHiltGraphics.RandomElement();
            }
        }

        private Color GetRandomRGBColor()
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        private Color GetBlackOrWhiteCore()
        {
            return UnityEngine.Random.value < 1f ? Color.white : Color.black;
        }

        public void SetSoundEffect(SoundDef soundDef)
        {
            if (soundDef != null)
            {
                selectedSoundEffect = soundDef;
            }
        }

        

        #endregion

        #region Save/Load Data

        public override void PostExposeData()
        {
            base.PostExposeData();

            // Save and restore blade colors
            Scribe_Values.Look(ref bladeColor, "bladeColor", Color.white);
            Scribe_Values.Look(ref coreColor, "coreColor", Color.white);
            Scribe_Values.Look(ref bladeColor2, "bladeColor2", Color.white);
            Scribe_Values.Look(ref coreColor2, "coreColor2", Color.white);

            // Save and restore blade lengths
            Scribe_Values.Look(ref bladeLength, "bladeLength", 1.5f);
            Scribe_Values.Look(ref bladeLength2, "bladeLength2", 1.5f);

            // Save and restore flip state
            Scribe_Values.Look(ref isFlipped, "isFlipped", false);

            // Save and restore initialization flags
            Scribe_Values.Look(ref colorsInitialized, "colorsInitialized", false);
            Scribe_Values.Look(ref _graphicsInitialized, "graphicsInitialized", false);

            // Save and restore hilt colors
            Scribe_Values.Look(ref _hiltManager._hiltColorOne, "hiltColorOne", Color.white);
            Scribe_Values.Look(ref _hiltManager._hiltColorTwo, "hiltColorTwo", Color.white);

            // Save and restore selected hilt
            var selectedHilt = HiltManager.SelectedHilt;
            Scribe_Defs.Look(ref selectedHilt, "selectedHilt");
            HiltManager.SelectedHilt = selectedHilt;

            // Save and restore selected hilt parts
            var selectedHiltParts = HiltManager.SelectedHiltParts ?? new List<HiltPartDef>();
            Scribe_Collections.Look(ref selectedHiltParts, "selectedHiltParts", LookMode.Def);
            HiltManager.SelectedHiltParts = selectedHiltParts;

            // Save and restore current scaling values
            Scribe_Values.Look(ref currentScaleForCore1AndBlade1, "currentScaleForCore1AndBlade1", Vector3.zero);
            Scribe_Values.Look(ref currentScaleForCore2AndBlade2, "currentScaleForCore2AndBlade2", Vector3.zero);

            // Save and restore target scaling values
            Scribe_Values.Look(ref targetScaleForCore1AndBlade1, "targetScaleForCore1AndBlade1", new Vector3(bladeLength, 1f, bladeLength));
            Scribe_Values.Look(ref targetScaleForCore2AndBlade2, "targetScaleForCore2AndBlade2", new Vector3(bladeLength2, 1f, bladeLength2));

            // Save and restore current draw offset
            Scribe_Values.Look(ref currentDrawOffset, "currentDrawOffset", Vector3.zero);
            Scribe_Values.Look(ref targetDrawOffset, "targetDrawOffset", Vector3.zero);
        }

        #endregion

        #region Gizmos

        public IEnumerable<Gizmo> EquippedGizmos()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "Customize Lightsaber",
                defaultDesc = "Customize the blade lengths, colors, and hilt of the lightsaber.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/LightsaberCustomization"),
                action = () =>
                {
                    Find.WindowStack.Add(new Dialog_LightsaberCustomization(
                        Wearer,
                        this,
                        (bladeColor, coreColor, bladeColor2, coreColor2) =>
                        {
                            this.bladeColor = bladeColor;
                            this.coreColor = coreColor;
                            this.bladeColor2 = bladeColor2;
                            this.coreColor2 = coreColor2;

                            SetShaderProperties();
                        }
                    ));
                }
            };
        }

        #endregion

        #region Combat and Effects

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            targetScaleForCore1AndBlade1 = new Vector3(bladeLength, 1f, bladeLength);
            targetScaleForCore2AndBlade2 = new Vector3(bladeLength2, 1f, bladeLength2);
            ResetToZero();

            if (lightsaberSound != null && lightsaberSound.Count > 0 && selectedSoundEffect != null)
            {
                selectedSoundEffect.PlayOneShot(pawn);
            }

            if (!colorsInitialized)
            {
                InitializeColors();
            }
        }

        public void ResetToZero()
        {
            scaleTimer = 0f;
            currentScaleForCore1AndBlade1 = Vector3.zero;
            currentScaleForCore2AndBlade2 = Vector3.zero;
            currentDrawOffset = Vector3.zero;
        }

        public void UpdateRotationForStance(float angle)
        {
            if (isFlipped)
            {
                float standardAngle = (angle - 45) % 360;
                if (standardAngle < 0) standardAngle += 360;

                float mirroredStandardAngle = (180 - standardAngle) % 360;
                if (mirroredStandardAngle < 0) mirroredStandardAngle += 360;

                float mirroredAngle = (mirroredStandardAngle + 45) % 360;
                if (mirroredAngle < 0) mirroredAngle += 360;
                stanceRotation = mirroredAngle;
            }
            else
            {
                stanceRotation = angle;
            }
        }

        public void UpdateDrawOffsetForStance(Vector3 offset)
        {
            drawOffset = new Vector3(isFlipped ? -offset.x : offset.x, offset.y, offset.z);
            targetDrawOffset = drawOffset;
        }

        public void UpdateScalingAndOffset()
        {
            if (targetScaleForCore1AndBlade1 == null) targetScaleForCore1AndBlade1 = Vector3.zero;
            if (targetScaleForCore2AndBlade2 == null) targetScaleForCore2AndBlade2 = Vector3.zero;

            scaleTimer += 5f / TicksPerGlowerUpdate;
            float t = Mathf.Clamp(scaleTimer, 0f, 1f);
            if (IsAnimatingNow)
            {
                currentScaleForCore1AndBlade1 = Vector3.Lerp(targetScaleForCore1AndBlade1, targetScaleForCore1AndBlade1, t);
                currentScaleForCore2AndBlade2 = Vector3.Lerp(targetScaleForCore2AndBlade2, targetScaleForCore2AndBlade2, t);
            }
            else
            {
                currentScaleForCore1AndBlade1 = Vector3.Lerp(Vector3.zero, targetScaleForCore1AndBlade1, t);
                currentScaleForCore2AndBlade2 = Vector3.Lerp(Vector3.zero, targetScaleForCore2AndBlade2, t);
            }
            currentDrawOffset = Vector3.Lerp(Vector3.zero, targetDrawOffset, t);
        }

        public void SetFlipped(bool flipped)
        {
            isFlipped = flipped;
        }

        #endregion
    }


    public class CompProperties_LightsaberBlade : CompProperties
    {
        public GraphicData bladeGraphicData;
        public float minBladeLength2 = 1f;
        public float maxBladeLength2 = 1f;
        public float minBladeLength1 = 1f;
        public float maxBladeLength1 = 1f;
        public FleckDef Fleck;
        public List<SoundDef> lightsaberSound;
        public List<HiltDef> availableHiltGraphics;

        public CompProperties_LightsaberBlade()
        {
            this.compClass = typeof(Comp_LightsaberBlade);
        }
    }
}