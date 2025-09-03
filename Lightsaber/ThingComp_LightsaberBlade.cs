using Lightsaber;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using static RimWorld.EffecterMaintainer;

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
        public bool isRGB = false;
        private CompGlower compGlower;

        private List<Tuple<Effecter, TargetInfo, TargetInfo>> maintainedEffecters = new List<Tuple<Effecter, TargetInfo, TargetInfo>>();

        private bool dirty;
        private Map map;
        private float curRadius;
        private IntVec3 prevPosition;


        // Blade Lengths
        public float bladeLength = 1;
        public float bladeLength2 = 1;

        public float BladeLength
        {
            get => bladeLength;
            set
            {
                bladeLength = value;
                targetScaleForCore1AndBlade1 = new Vector3(bladeLength, 1f, bladeLength);
                SetShaderProperties();
            }
        }

        public float BladeLength2
        {
            get => bladeLength2;
            set
            {
                bladeLength2 = value;
                targetScaleForCore2AndBlade2 = new Vector3(bladeLength2, 1f, bladeLength2);
                SetShaderProperties();
            }
        }

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
        public float scaleTimer;
        public static readonly MaterialPropertyBlock propertyBlock = new();
        public MaterialPropertyBlock PropertyBlock => propertyBlock;
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

        private Pawn cachedWearer;

        public Pawn Wearer
        {
            get
            {
                if (cachedWearer != null && parent.ParentHolder != cachedWearer)
                {
                    cachedWearer = null;
                }
                if (cachedWearer != null && cachedWearer.Spawned)
                    return cachedWearer;

                if (parent?.ParentHolder is Pawn_EquipmentTracker tracker)
                    cachedWearer = tracker.pawn;
                else if (parent?.ParentHolder is Pawn_InventoryTracker inventory)
                    cachedWearer = inventory.pawn;
                else
                    cachedWearer = null;

                return cachedWearer;
            }
        }

        public bool IsAnimatingNow => animationDeflectionTicks > 0;
        public float CurrentRotation => stanceRotation;
        public Vector3 CurrentDrawOffset => drawOffset;
        public CompProperties_LightsaberBlade Props => (CompProperties_LightsaberBlade)props;

        public HiltManager HiltManager => _hiltManager;
        public List<HiltPartCategoryDef> AllowedCategories => Props?.allowedCategories ?? new List<HiltPartCategoryDef>();



        #endregion

        #region Initialization and Setup

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            _hiltManager = new HiltManager();
            if (props is CompProperties_LightsaberBlade lightsaberProps)
            {
                _hiltManager.AvailableHilts = lightsaberProps.availableHiltGraphics;
                minBladeLength = lightsaberProps.minBladeLength1;
                maxBladeLength = lightsaberProps.maxBladeLength1;
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

        private float hue = 0f;
        private const float HueChangeRate = 1f;

       
        public override void CompTickInterval(int delta)
        {
            if (parent.MapHeld != null)
            {
                if (map == null)
                {
                    map = parent.MapHeld;
                    dirty = true;
                }
                if (prevPosition != parent.PositionHeld)
                {
                    prevPosition = parent.PositionHeld;
                    dirty = true;
                }
                bool shouldGlow = ShouldGlow();
                if ((compGlower == null && shouldGlow) || !shouldGlow)
                {
                    dirty = true;
                }
                if (dirty)
                {
                    UpdateGlower();
                    dirty = false;
                }

                if (Wearer != null && Wearer.MapHeld != null)
                {
                    if (parent.MapHeld.weatherManager.curWeather.rainRate > 0 && PawnRenderUtility.CarryWeaponOpenly(Wearer))
                    {
                        Vector3 spawnOffset = Wearer.Position.ToVector3();
                        Vector3 spawnPos = spawnOffset;
                        spawnPos = new Vector3(
                                   Mathf.Round(spawnPos.x),
                                   Mathf.Round(spawnPos.y),
                                   Mathf.Round(spawnPos.z)
                               );

                        float steamAngle = CurrentRotation + Rand.Range(-15f, 15f);
                        if (Find.TickManager.TicksGame % Rand.RangeInclusive(60, 300) == 0)
                        {
                            Effecter effecter = new Effecter(LightsaberDefOf.Force_SteamVapor);
                            effecter.scale = 0.25f;
                            effecter.offset = CurrentDrawOffset;
                            effecter.Trigger(new TargetInfo(spawnPos.ToIntVec3(), Wearer.Map), TargetInfo.Invalid);
                            effecter.Cleanup();

                            if (DebugSettings.godMode)
                                Log.Message($"Lightning at {spawnPos} (Rounded)");
                        }
                    }
                    if (isRGB)
                    {
                        hue += HueChangeRate / 360f;
                        if (hue >= 1f) hue = 0f;
                        bladeColor = Color.HSVToRGB(hue, 1f, 1f);
                        bladeColor2 = bladeColor;
                        // Update shader properties with new colors
                        SetShaderProperties();
                    }

                }

                if (Wearer != null && Wearer.MapHeld != null && PawnRenderUtility.CarryWeaponOpenly(Wearer) && !IsThrowingWeapon)
                {
                    if (HiltManager.SelectedHiltParts != null)
                    {
                        foreach (var hiltPart in HiltManager.SelectedHiltParts)
                        {
                            if (hiltPart?.effects == null) continue;

                            Vector3 spawnOffset = Wearer.Position.ToVector3();
                            Vector3 spawnPos = spawnOffset;
                            spawnPos = new Vector3(
                                Mathf.Round(spawnPos.x),
                                Mathf.Round(spawnPos.y),
                                Mathf.Round(spawnPos.z)
                            );

                            float steamAngle = CurrentRotation + Rand.Range(-15f, 15f);

                            foreach (var hiltEffect in hiltPart.effects)
                            {
                                if (hiltEffect?.EffecterDef == null) continue;

                                if (hiltEffect.shouldMaintain && Find.TickManager.TicksGame % Rand.RangeInclusive(
    hiltEffect.EffecterDef.maintainTicks != 0 ? hiltEffect.EffecterDef.maintainTicks : (int)hiltEffect.minTime,
    hiltEffect.EffecterDef.maintainTicks != 0 ? hiltEffect.EffecterDef.maintainTicks : (int)hiltEffect.maxTime) == 0)
                                {
                                    Effecter effecter = new Effecter(hiltEffect.EffecterDef);
                                    effecter.offset = CurrentDrawOffset;
                                    effecter.offset.RotatedBy(CurrentRotation);
                                    effecter.def.SpawnMaintained(spawnPos.ToIntVec3(), Wearer.Map);
                                    effecter?.EffectTick(parent, parent);
                                    if (DebugSettings.godMode)
                                    {
                                        Log.Message($"[Hilt Effects] Spawning {hiltPart.label}'s " +
                                                   $"{hiltEffect.EffecterDef.defName} at {spawnPos} " +
                                                   $"(Interval: {hiltEffect.ticksToMaintain} ticks)");
                                    }
                                }


                                else if (Find.TickManager.TicksGame % Rand.RangeInclusive(
                                     Mathf.RoundToInt(hiltEffect.minTime),
                                     Mathf.RoundToInt(hiltEffect.maxTime)) == 0 && !hiltEffect.shouldMaintain)
                                {
                                    Effecter effecter = new Effecter(hiltEffect.EffecterDef);
                                    effecter.offset = CurrentDrawOffset;
                                    effecter.offset.RotatedBy(CurrentRotation);

                                    effecter.Trigger(
                                        new TargetInfo(spawnPos.ToIntVec3(), Wearer.Map),
                                        new TargetInfo(spawnPos.ToIntVec3(), Wearer.Map)
                                    );

                                    effecter.Cleanup();

                                    if (DebugSettings.godMode)
                                    {
                                        Log.Message($"[Hilt Effects] Spawning {hiltPart.label}'s " +
                                                   $"{hiltEffect.EffecterDef.defName} at {spawnPos} " +
                                                   $"(Interval: {hiltEffect.minTime}-{hiltEffect.maxTime} ticks)");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddEffecterToMaintain(Effecter eff, IntVec3 posA, IntVec3 posB, int ticks, Map map = null)
        {
            eff.ticksLeft = ticks;
            TargetInfo item = new TargetInfo(posA, map ?? Wearer.Map);
            TargetInfo item2 = new TargetInfo(posB, map ?? Wearer.Map);
            maintainedEffecters.Add(new Tuple<Effecter, TargetInfo, TargetInfo>(eff, item, item2));
        }


        private IntVec3 GetPosition() => Wearer != null ? Wearer.Position : parent.PositionHeld;

        public bool ShouldGlow()
        {
            if (Wearer != null && parent.MapHeld != null)
            {
                IntVec3 positionHeld = GetPosition();
                if (positionHeld.InBounds(parent.MapHeld) && PawnRenderUtility.CarryWeaponOpenly(Wearer) && !IsThrowingWeapon && ForceLightsabers_ModSettings.shouldGlow)
                {
                    return true;
                }
            }
            RemoveGlower(parent.MapHeld);
            return false;
        }

        private void UpdateGlower()
        {
            IntVec3 position = GetPosition();
            position.y += (int)1f;
            Map mapHeld = parent.MapHeld;
            if (mapHeld == null || !position.IsValid || mapHeld.glowGrid == null)
            {
                RemoveGlower(mapHeld);
                return;
            }
            RemoveGlower(mapHeld);
            if (ShouldGlow())
            {
                try
                {
                    compGlower = new CompGlower();
                    var glowerProps = new CompProperties_Glower
                    {
                        glowColor = new ColorInt(bladeColor),
                        glowRadius = 1.5f,
                        overlightRadius = 1.5f
                    };

                    compGlower.parent = Wearer ?? parent;
                    compGlower.Initialize(glowerProps);

                    mapHeld.glowGrid.RegisterGlower(compGlower);
                    mapHeld.mapDrawer.MapMeshDirty(position, MapMeshFlagDefOf.Things);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to update lightsaber glower: {ex}");
                    compGlower = null;
                }
            }
        }


        private void RemoveGlower(Map prevMap)
        {
            if (prevMap != null && compGlower != null && prevMap.glowGrid != null)
            {
                try
                {
                    prevMap.glowGrid.DeRegisterGlower(compGlower);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to remove lightsaber glower: {ex}");
                }
                finally
                {
                    compGlower = null;
                }
            }
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

                // Filter by allowed categories
                HiltManager.SelectedHiltParts = AllowedCategories
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



            Color? crystalColor = null;

            HiltPartDef crystalPart = HiltManager.SelectedHiltParts.FirstOrDefault(p => p.colorGenerator != null);
            if (crystalPart == LightsaberDefOf.Force_SyntheticKyberCrystalHiltPart)
            {
                crystalColor = ColorUtility.GetSyntheticCrystalColor(Wearer);
            }
            else
            {
                crystalColor = crystalPart?.colorGenerator?.NewRandomizedColor();
            }

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

            HiltPartDef crystalPart2 = HiltManager.SelectedHiltParts.FirstOrDefault(p => p.colorGenerator2 != null);
            Color? crystalColor2 = null;

            if (crystalPart == LightsaberDefOf.Force_SyntheticKyberCrystalHiltPart)
            {
                crystalColor2 = ColorUtility.GetSyntheticCrystalColor(Wearer);
            }
            else
            {
                crystalColor2 = crystalPart?.colorGenerator?.NewRandomizedColor();
            }
            if (modExt?.coreColors != null && modExt.coreColors.Any())
            {
                coreColor = modExt.coreColors.RandomElement();
            }
            else if (crystalColor2.HasValue)
            {
                coreColor2 = crystalColor2.Value;
            }
            else
            {
                coreColor = GetBlackOrWhiteCore();
            }

            if (modExt != null)
            {
                if (modExt.defaultBladeLength1 != null)
                {
                    BladeLength = Mathf.Clamp(modExt.defaultBladeLength1, minBladeLength, maxBladeLength);
                }
                if (modExt.defaultBladeLength2 != null)
                {
                    BladeLength2 = Mathf.Clamp(modExt.defaultBladeLength2, minBladeLength, maxBladeLength);
                }
            }
            else
            {
                BladeLength = Mathf.Lerp(minBladeLength, maxBladeLength, 0.5f);
                BladeLength2 = Mathf.Lerp(minBladeLength, maxBladeLength, 0.5f);
            }


            bladeColor2 = bladeColor;
            coreColor2 = coreColor;
            if (modExt != null)
            {
                if (modExt.hiltColorOne != null)
                {
                    HiltManager.HiltColorOne = (Color)modExt.hiltColorOne;
                }
                else if (modExt.validStuffCategoriesHiltColorOne != null)
                {
                    HiltManager.HiltColorOne = StuffColorUtility.GetRandomColorFromStuffCategories(modExt.validStuffCategoriesHiltColorOne);
                }
                if (modExt.hiltColorTwo != null)
                {
                    HiltManager.HiltColorTwo = (Color)modExt.hiltColorTwo;
                }
                else if (modExt.validStuffCategoriesHiltColorTwo != null)
                {
                    HiltManager.HiltColorTwo = StuffColorUtility.GetRandomColorFromStuffCategories(modExt.validStuffCategoriesHiltColorTwo);
                }
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
            if (HiltManager.SelectedHilt == null && Props.availableHiltGraphics != null && Props.availableHiltGraphics.Any())
            {
                HiltManager.SelectedHilt = Props.availableHiltGraphics.RandomElement();
            }

            CompUniqueWeapon uniqueWeaponComp = null;
            CompBladelinkWeapon bladelinkWeaponComp = null;

            bool hasUniqueWeapon = ModLister.CheckOdyssey("Unique Weapons") && parent.TryGetComp(out uniqueWeaponComp);
            bool hasBladelinkWeapon = ModLister.CheckRoyalty("Persona weapon") && parent.TryGetComp(out bladelinkWeaponComp);

            if (ModLister.CheckOdyssey("Unique Weapons") &&
            hasUniqueWeapon || ModLister.CheckRoyalty("Persona weapon") && hasBladelinkWeapon)
            {
                // Check if any traits are LightsaberPresetDef
                LightsaberPresetDef lightsaberPresetTrait = null;

                if (uniqueWeaponComp != null)
                {
                    lightsaberPresetTrait = uniqueWeaponComp.TraitsListForReading
                        .FirstOrDefault(trait => trait is LightsaberPresetDef) as LightsaberPresetDef;
                }

                if (lightsaberPresetTrait == null && bladelinkWeaponComp != null)
                {
                    lightsaberPresetTrait = bladelinkWeaponComp.TraitsListForReading?
                        .FirstOrDefault(trait => trait is LightsaberPresetDef) as LightsaberPresetDef;
                }

                if (lightsaberPresetTrait?.LightsaberPreset != null)
                {
                    var presetHiltParts = lightsaberPresetTrait.LightsaberPreset.preferredHiltParts;
                    if (presetHiltParts != null && presetHiltParts.Count > 0)
                    {
                        var allHiltParts = DefDatabase<HiltPartDef>.AllDefsListForReading;

                        // Filter only valid parts that exist in the database
                        var validPresetParts = presetHiltParts
                            .Where(part => part != null && allHiltParts.Contains(part))
                            .Distinct()
                            .ToList();

                        if (validPresetParts.Any())
                        {
                            HiltManager.SelectedHiltParts = AllowedCategories
                                .Select(category =>
                                    validPresetParts.FirstOrDefault(def => def.category == category))
                                .Where(part => part != null)
                                .ToList();
                        }
                    }

                    var matchingHilts = lightsaberPresetTrait.LightsaberPreset.preferredHilts
               .Where(hilt => Props.availableHiltGraphics.Contains(hilt))
               .ToList();

                    if (matchingHilts.Any())
                    {
                        HiltManager.SelectedHilt = matchingHilts.FirstOrDefault();
                    }

                    HiltManager.HiltColorOne = lightsaberPresetTrait?.LightsaberPreset.HiltColor1 ?? default;
                    HiltManager.HiltColorTwo = lightsaberPresetTrait?.LightsaberPreset.HiltColor2 ?? default;
                    bladeColor = lightsaberPresetTrait?.LightsaberPreset.BladeColor1 ?? default;
                    bladeColor2 = lightsaberPresetTrait?.LightsaberPreset?.BladeColor2 ?? default;
                    coreColor = lightsaberPresetTrait?.LightsaberPreset.CoreColor1 ?? default;
                    coreColor2 = lightsaberPresetTrait?.LightsaberPreset.CoreColor2 ?? default;
                }
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

            Scribe_Values.Look(ref stanceRotation, "stanceRotation", 0f);
            Scribe_Values.Look(ref drawOffset, "drawOffset", Vector3.zero);

            Pawn wearer = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                wearer = Wearer;
            }
            Scribe_References.Look(ref wearer, "wearer");
        }

        #endregion

        #region Gizmos

        public IEnumerable<Gizmo> EquippedGizmos()
        {
            // Return if critical objects are null
            if (Wearer == null || parent == null || Props == null)
            {
                yield break;
            }

            // Safely return base gizmos
            var baseGizmos = base.CompGetGizmosExtra();
            if (baseGizmos != null)
            {
                foreach (Gizmo gizmo in baseGizmos)
                {
                    if (gizmo != null) yield return gizmo;
                }
            }

            bool showCustomization = (Wearer.Drafted && (!ForceLightsabers_ModSettings.lightsaberCustomizationUndrafted)) ||
                                   (!Wearer.Drafted && (ForceLightsabers_ModSettings.lightsaberCustomizationUndrafted));

            if (showCustomization)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Customize Lightsaber",
                    defaultDesc = "Customize the blade lengths, colors, and hilt of the lightsaber.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/LightsaberCustomization", false) ?? BaseContent.BadTex,
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

            if (Wearer != null && Prefs.DevMode && DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Spin Lightsaber",
                    defaultDesc = "Perform a spinning lightsaber animation that can deflect incoming projectiles.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/LightsaberSpin", false) ?? BaseContent.BadTex,
                    action = () =>
                    {
                        // Start spinning animation
                        StartSpinAnimation();
                    },
                    hotKey = KeyBindingDefOf.Misc2
                };

                yield return new Command_Toggle
                {
                    defaultLabel = "RGB Mode",
                    defaultDesc = "Toggle rainbow color cycling mode",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmo/LightsaberRGB", false) ?? BaseContent.BadTex,
                    isActive = () => isRGB,
                    toggleAction = () =>
                    {
                        isRGB = !isRGB;
                        if (!isRGB) hue = 0f;
                    },
                    hotKey = KeyBindingDefOf.Misc3
                };
            }
        }


        private void StartSpinAnimation()
        {
            if (Wearer == null) return;
            AnimationDeflectionTicks = 6000;
        }


        #endregion

        #region Combat and Effects

        public List<PawnRenderNode> activeRenderNodes;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            targetScaleForCore1AndBlade1 = new Vector3(BladeLength, 1f, BladeLength);
            targetScaleForCore2AndBlade2 = new Vector3(BladeLength2, 1f, BladeLength2);
            ResetToZero();

            if (lightsaberSound != null && lightsaberSound.Count > 0 && selectedSoundEffect != null)
            {
                selectedSoundEffect.PlayOneShot(pawn);
            }

            if (!colorsInitialized)
            {
                InitializeColors();
            }

            if (!UnityData.IsInMainThread)
            {
                return;
            }
            else
            {
                activeRenderNodes = CompRenderNodes();
                pawn.Drawer?.renderer?.renderTree?.SetDirty();
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);

            if (cachedWearer == pawn)
            {
                cachedWearer = null;
            }

            RemoveGlower(pawn.MapHeld);
            activeRenderNodes = null;
        }

        public override List<PawnRenderNode> CompRenderNodes()
        {
            try
            {
                if (!Props.renderNodeProperties.NullOrEmpty() &&
                    parent != null &&
                    parent.ParentHolder != null &&
                    parent.ParentHolder.ParentHolder is Pawn pawn)
                {
                    List<PawnRenderNode> list = new List<PawnRenderNode>();
                    foreach (PawnRenderNodeProperties renderNodeProperty in Props.renderNodeProperties)
                    {
                        if (renderNodeProperty?.nodeClass != null && pawn.Drawer?.renderer?.renderTree != null)
                        {
                            try
                            {
                                PawnRenderNode node = (PawnRenderNode)Activator.CreateInstance(
                                    renderNodeProperty.nodeClass,
                                    pawn,
                                    renderNodeProperty,
                                    pawn.Drawer.renderer.renderTree
                                );
                                if (node != null)
                                {
                                    list.Add(node);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed to create render node: {ex}");
                            }
                        }
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CompRenderNodes: {ex}");
            }

            return base.CompRenderNodes();
        }



        public void ForceUpdateScaling()
        {
            currentScaleForCore1AndBlade1 = targetScaleForCore1AndBlade1;
            currentScaleForCore2AndBlade2 = targetScaleForCore2AndBlade2;
            scaleTimer = 1f; // Mark interpolation as "complete"
            SetShaderProperties(); // Update shader
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
        public List<HiltPartCategoryDef> allowedCategories;
        public List<PawnRenderNodeProperties> renderNodeProperties;
        public CompProperties_LightsaberBlade()
        {
            this.compClass = typeof(Comp_LightsaberBlade);
        }
    }
}