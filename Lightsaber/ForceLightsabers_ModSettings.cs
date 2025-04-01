using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
// ReSharper disable All

namespace Lightsaber
{
    public class ForceLightsabers_ModSettings : ModSettings
    {
        public static float spinRate = 1f;
        public static float entropyGain = 1f;
        public static float deflectionMultiplier = 1f;
        public static bool DeflectionSpin = true;
        public static bool customStance = false;
        public static bool projectileDeflectionSelector = false;
        public static List<string> deflectableProjectileNames = new List<string>();
        public static HashSet<int> deflectableProjectileHashes = new HashSet<int>();
        public static bool lightsaberParryEnabled = true;
        public static bool lightsaberCustomizationUndrafted = false;
        public ForceLightsabers_ModSettings()
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref spinRate, "spinRate", 9f);
            Scribe_Values.Look(ref entropyGain, "entropyGain", 20f);
            Scribe_Values.Look(ref deflectionMultiplier, "deflectionMultiplier", 1f);
            Scribe_Values.Look(ref DeflectionSpin, "deflectionspin", false);
            Scribe_Values.Look(ref customStance, "customStance", false);
            Scribe_Values.Look(ref projectileDeflectionSelector, "projectileDeflectionSelector", false);
            Scribe_Values.Look(ref lightsaberParryEnabled, "lightsaberparryEnabled", false);
            Scribe_Values.Look(ref lightsaberCustomizationUndrafted, "lightsaberCustomizationUndrafted", false);
            Scribe_Collections.Look(ref deflectableProjectileHashes, "deflectableProjectileHashes", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                deflectableProjectileHashes = deflectableProjectileHashes ?? new HashSet<int>();

                foreach (int hash in deflectableProjectileHashes)
                {
                    ThingDef projectileDef = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d => d.shortHash == hash);
                    if (projectileDef != null)
                    {
                        deflectableProjectileHashes.Add(projectileDef.shortHash);
                    }
                }
            }
        }

        public static void AddDeflectableProjectile(ThingDef def)
        {
            if (!deflectableProjectileHashes.Contains(def.shortHash))
            {
                deflectableProjectileHashes.Add(def.shortHash);
            }
        }

        public static void RemoveDeflectableProjectile(ThingDef def)
        {
            deflectableProjectileHashes.Remove(def.shortHash);
        }
    }

    public class TheForce_Mod : Mod
    {
        private enum Tab
        {
            General,
            Projectiles
        }

        private Tab currentTab = Tab.General;
        ForceLightsabers_ModSettings settings;
        Vector2 scrollPosition = Vector2.zero;

        public TheForce_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ForceLightsabers_ModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Adjusted the y position of the tab buttons to raise them higher
            Rect tabRect = new Rect(inRect.x, inRect.y - 30f, inRect.width, 30);  // Raised by subtracting 10 from y
            Rect generalTabRect = tabRect.LeftHalf().ContractedBy(4f);
            Rect projectilesTabRect = tabRect.RightHalf().ContractedBy(4f);

            if (Widgets.ButtonText(generalTabRect, "General"))
            {
                currentTab = Tab.General;
            }
            if (Widgets.ButtonText(projectilesTabRect, "Projectiles"))
            {
                if (ForceLightsabers_ModSettings.projectileDeflectionSelector)
                {
                    currentTab = Tab.Projectiles;
                }
                else
                {
                    Messages.Message("Force.projectileDeflectionSelectorDisabled".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            // Space after tabs
            listingStandard.Gap(36f);

            switch (currentTab)
            {
                case Tab.General:
                    DrawGeneralSettings(inRect);
                    break;
                case Tab.Projectiles:
                    if(ForceLightsabers_ModSettings.projectileDeflectionSelector)
                    {
                        DrawProjectileSettings(inRect);
                        if (ForceLightsabers_ModSettings.deflectableProjectileNames == null)
                        {
                            ForceLightsabers_ModSettings.deflectableProjectileNames = new List<string>();
                        }

                        if (ForceLightsabers_ModSettings.deflectableProjectileHashes == null)
                        {
                            ForceLightsabers_ModSettings.deflectableProjectileHashes = new HashSet<int>();
                        }

                        // Sync hashes if not yet synced after load
                        if (Scribe.mode == LoadSaveMode.PostLoadInit)
                        {
                            ForceLightsabers_ModSettings.deflectableProjectileHashes.Clear();
                            foreach (var defName in ForceLightsabers_ModSettings.deflectableProjectileNames)
                            {
                                var projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                                if (projectileDef != null)
                                {
                                    ForceLightsabers_ModSettings.deflectableProjectileHashes.Add(projectileDef.shortHash);
                                }
                            }
                        }
                    }
                    break;
            }

            listingStandard.End();
        }

        private void DrawGeneralSettings(Rect inRect)
        {
            // Begin ScrollView
            Rect scrollRect = new Rect(0, 0, inRect.width - 16f, 1000); // Height is a large enough value to contain the content.
            Widgets.BeginScrollView(inRect, ref scrollPosition, scrollRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(scrollRect);

            listingStandard.Label("Force_Spin_Rate".Translate() + ": " + ForceLightsabers_ModSettings.spinRate);
            ForceLightsabers_ModSettings.spinRate = listingStandard.Slider(ForceLightsabers_ModSettings.spinRate, 1f, 100f);

            listingStandard.Label("Force_DeflectionChance".Translate() + ": " + ForceLightsabers_ModSettings.deflectionMultiplier);
            ForceLightsabers_ModSettings.deflectionMultiplier = listingStandard.Slider(ForceLightsabers_ModSettings.deflectionMultiplier, 0.1f, 20f);

            listingStandard.Label("Force_Entropy_Gain".Translate() + ": " + ForceLightsabers_ModSettings.entropyGain);
            ForceLightsabers_ModSettings.entropyGain = listingStandard.Slider(ForceLightsabers_ModSettings.entropyGain, 1f, 20f);

            listingStandard.CheckboxLabeled("Force.DeflectionSpin".Translate(), ref ForceLightsabers_ModSettings.DeflectionSpin, "Force.DeflectionSpinDesc".Translate());
            listingStandard.CheckboxLabeled("Force.CustomStance".Translate(), ref ForceLightsabers_ModSettings.customStance, "Force.CustomStanceDesc".Translate());
            listingStandard.CheckboxLabeled("Force.projectileDeflectionSelector".Translate(), ref ForceLightsabers_ModSettings.projectileDeflectionSelector, "Force.projectileDeflectionSelector".Translate());
            listingStandard.CheckboxLabeled("Force.lightsaberparry".Translate(), ref ForceLightsabers_ModSettings.lightsaberParryEnabled, "Force.lightsaberparryDesc".Translate());
            listingStandard.CheckboxLabeled("Force.lightsaberCustomizationUndrafted".Translate(), ref ForceLightsabers_ModSettings.lightsaberCustomizationUndrafted, "Force.lightsaberCustomizationUndrafted".Translate());


            listingStandard.Gap();
            if (listingStandard.ButtonText("Force_ResettoDefault".Translate()))
            {
                ResetToDefaultValues();
            }

            listingStandard.End();
            Widgets.EndScrollView();
        }

        private Dictionary<string, bool> modExpandedStates = new Dictionary<string, bool>();
        private void DrawProjectileSettings(Rect inRect)
        {
            // Get all projectiles
            List<ThingDef> projectiles = ProjectileUtility.GetAllProjectiles();

            // Group projectiles by mod name
            var projectileGroups = new Dictionary<string, List<ThingDef>>();
            foreach (var projectileDef in projectiles)
            {
                string modName = projectileDef.modContentPack?.Name ?? "Vanilla"; // Default to "Vanilla" if no mod

                if (!projectileGroups.ContainsKey(modName))
                {
                    projectileGroups[modName] = new List<ThingDef>();
                }
                projectileGroups[modName].Add(projectileDef);
            }

            // Buttons for enabling/disabling all projectiles
            Rect enableAllButtonRect = new Rect(inRect.x, inRect.y, 120f, 30f);
            Rect disableAllButtonRect = new Rect(inRect.x + 130f, inRect.y, 120f, 30f);

            if (Widgets.ButtonText(enableAllButtonRect, "Enable All"))
            {
                foreach (var group in projectileGroups)
                {
                    foreach (var projectileDef in group.Value)
                    {
                        if (!ForceLightsabers_ModSettings.deflectableProjectileHashes.Contains(projectileDef.shortHash))
                        {
                            ForceLightsabers_ModSettings.AddDeflectableProjectile(projectileDef);
                        }
                    }
                }
            }

            if (Widgets.ButtonText(disableAllButtonRect, "Disable All"))
            {
                foreach (var group in projectileGroups)
                {
                    foreach (var projectileDef in group.Value)
                    {
                        if (ForceLightsabers_ModSettings.deflectableProjectileHashes.Contains(projectileDef.shortHash))
                        {
                            ForceLightsabers_ModSettings.RemoveDeflectableProjectile(projectileDef);
                        }
                    }
                }
            }

            // Calculate the height of the scroll view
            float scrollViewHeight = CalculateScrollViewHeight(projectileGroups, inRect.width);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, scrollViewHeight);

            // Begin scrollable view
            Rect outRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, inRect.height - 36f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (var group in projectileGroups)
            {
                // Draw mod group header with background
                Rect headerRect = new Rect(0f, y, viewRect.width, 40f);
                Widgets.DrawBoxSolid(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.6f)); // Header background
                bool isExpanded = modExpandedStates.TryGetValue(group.Key, out bool expanded) && expanded;

                if (Widgets.ButtonText(new Rect(headerRect.x + 5f, headerRect.y + 5f, 30f, 30f), isExpanded ? "▼" : "▶"))
                {
                    modExpandedStates[group.Key] = !isExpanded;
                }

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(headerRect.x + 40f, headerRect.y + 5f, headerRect.width - 45f, 30f), group.Key);
                Text.Font = GameFont.Small;

                y += 40f; // Increase y for the next group

                if (isExpanded)
                {
                    bool isOddRow = false;

                    foreach (var projectileDef in group.Value)
                    {
                        // Alternate row background colors
                        Rect rowRect = new Rect(0f, y, viewRect.width, 40f);
                        Widgets.DrawBoxSolid(rowRect, isOddRow ? new Color(0.9f, 0.9f, 0.9f, 0.2f) : new Color(0.8f, 0.8f, 0.8f, 0.2f));
                        isOddRow = !isOddRow;

                        // Draw icon if available
                        Rect iconRect = new Rect(rowRect.x + 5f, rowRect.y + 5f, 30f, 30f);
                        if (projectileDef.uiIcon != null)
                        {
                            GUI.DrawTexture(iconRect, projectileDef.uiIcon);
                        }
                        else
                        {
                            Widgets.DrawBoxSolid(iconRect, new Color(0.5f, 0.5f, 0.5f)); // Placeholder box for missing icon
                        }

                        // Checkbox and label
                        Rect labelRect = new Rect(iconRect.xMax + 10f, rowRect.y, rowRect.width - iconRect.width - 20f, 40f);
                        bool isDeflectable = ForceLightsabers_ModSettings.deflectableProjectileHashes.Contains(projectileDef.shortHash);
                        bool checkboxValue = isDeflectable;

                        Widgets.CheckboxLabeled(labelRect, projectileDef.label ?? projectileDef.defName, ref checkboxValue);

                        // Add or remove the projectile based on checkbox state
                        if (checkboxValue && !isDeflectable)
                        {
                            ForceLightsabers_ModSettings.AddDeflectableProjectile(projectileDef);
                        }
                        else if (!checkboxValue && isDeflectable)
                        {
                            ForceLightsabers_ModSettings.RemoveDeflectableProjectile(projectileDef);
                        }

                        y += 40f; // Increase y for the next projectile
                    }
                }

                y += 10f; // Space between groups
            }

            Widgets.EndScrollView();
        }

        private float CalculateScrollViewHeight(Dictionary<string, List<ThingDef>> projectileGroups, float viewWidth)
        {
            float height = 0f;
            height += 40f * projectileGroups.Count; // Space for mod headers

            foreach (var group in projectileGroups)
            {
                if (modExpandedStates.TryGetValue(group.Key, out bool isExpanded) && isExpanded)
                {
                    height += 600f * group.Value.Count; // Space for projectiles if expanded
                }
            }

            return height;
        }

        // Method to reset settings to default values
        private void ResetToDefaultValues()
        {
            // Set default values for each setting

            ForceLightsabers_ModSettings.spinRate = 9f; 
            ForceLightsabers_ModSettings.entropyGain = 10f;
            ForceLightsabers_ModSettings.deflectionMultiplier = 1f;
            ForceLightsabers_ModSettings.DeflectionSpin = true;
            ForceLightsabers_ModSettings.customStance = false;
            ForceLightsabers_ModSettings.lightsaberParryEnabled = true;
            ForceLightsabers_ModSettings.lightsaberCustomizationUndrafted = false;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ApplySettings();
        }

        private void ApplySettings()
        {
            LightSaberProjectile projectile = new LightSaberProjectile(); // Instantiate a LightSaberProjectile object
            projectile.spinRate = ForceLightsabers_ModSettings.spinRate; // Set the spinRate property
        }

        public override string SettingsCategory()
        {
            return "StarWars_TheForce_Lightsabers".Translate();
        }
    }
}