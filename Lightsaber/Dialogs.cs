using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public class Dialog_LightsaberCustomization : Window
    {
        // Constants
        private const int MinValue = 0;
        private const int MaxValue = 255;
        private const float TitleHeight = 32f;
        private const float TabHeight = 30f;
        private const float SectionPadding = 10f;
        private const float ElementPadding = 5f;
        private const float PreviewSize = 500f;
        private const float PreviewRotation = -135f;
        private static readonly Color SectionBackground = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        private static readonly Color HighlightColor = new Color(0.2f, 0.4f, 0.6f, 0.5f);

        // Fields
        private readonly Pawn pawn;
        private readonly Comp_LightsaberBlade lightsaberBlade;
        private readonly Action<Color, Color, Color, Color> confirmAction;

        private int bladeColorRed, bladeColorGreen, bladeColorBlue, bladeColorAlpha = 255;
        private int coreColorRed, coreColorGreen, coreColorBlue, coreColorAlpha = 255;
        private int bladeColor2Red, bladeColor2Green, bladeColor2Blue, bladeColor2Alpha = 255;
        private int coreColor2Red, coreColor2Green, coreColor2Blue, coreColor2Alpha = 255;

        private Color bladeColor = Color.white;
        private Color coreColor = Color.white;
        private Color bladeColor2 = Color.white;
        private Color coreColor2 = Color.white;

        private enum Tab { RGBSelector, Hilts }
        private Tab currentTab = Tab.RGBSelector;
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 hiltComponentsScrollPos = Vector2.zero;

        public Dialog_LightsaberCustomization(Pawn pawn, Comp_LightsaberBlade lightsaberBlade, Action<Color, Color, Color, Color> confirmAction)
        {
            this.pawn = pawn;
            this.lightsaberBlade = lightsaberBlade;
            this.confirmAction = confirmAction;

            SyncBladeColor(lightsaberBlade.bladeColor);
            SyncCoreColor(lightsaberBlade.coreColor);
            SyncBlade2Color(lightsaberBlade.bladeColor2);
            SyncCore2Color(lightsaberBlade.coreColor2);

            UpdateCurrentColor();

            // Window settings
            forcePause = false;
            absorbInputAroundWindow = true;
            preventCameraMotion = true;
            draggable = true;
            resizeable = true;
            doCloseX = true;
            doCloseButton = true;
        }

        public override Vector2 InitialSize => new Vector2(1350f, 900f);

        public override void DoWindowContents(Rect inRect)
        {
            // Draw title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, TitleHeight);
            Widgets.Label(titleRect, "Force_LightsaberCustomization".Translate());
            Text.Font = GameFont.Small;

            // Main content area
            Rect mainRect = new Rect(inRect.x, inRect.y + TitleHeight, inRect.width, inRect.height - TitleHeight - CloseButSize.y);

            // Draw tabs
            Rect tabRect = new Rect(mainRect.x, mainRect.y, mainRect.width, TabHeight);
            DrawTabs(tabRect);

            // Content area below tabs
            Rect contentRect = new Rect(mainRect.x, tabRect.yMax + SectionPadding, mainRect.width, mainRect.height - tabRect.height - SectionPadding);

            // Draw preview on left, controls on right
            float previewWidth = Mathf.Min(PreviewSize, contentRect.width * 0.45f);
            Rect previewRect = new Rect(contentRect.x + SectionPadding, contentRect.y, previewWidth, contentRect.height - SectionPadding);
            Rect controlsRect = new Rect(previewRect.xMax + SectionPadding * 2, contentRect.y, contentRect.width - previewWidth - SectionPadding * 3, contentRect.height);

            // Draw with section backgrounds
            Widgets.DrawBoxSolid(previewRect, SectionBackground);
            Widgets.DrawBoxSolid(controlsRect, SectionBackground);
            Widgets.DrawBox(previewRect);
            Widgets.DrawBox(controlsRect);

            // Draw the lightsaber preview
            DrawLightsaberPreview(previewRect);

            // Draw the appropriate controls
            switch (currentTab)
            {
                case Tab.RGBSelector:
                    DrawRGBControls(controlsRect);
                    break;
                case Tab.Hilts:
                    DrawHiltControls(controlsRect);
                    break;
            }
        }

        private void DrawTabs(Rect rect)
        {
            float tabWidth = rect.width / Enum.GetValues(typeof(Tab)).Length;

            foreach (Tab tab in Enum.GetValues(typeof(Tab)))
            {
                Rect tabRect = new Rect(rect.x + (float)tab * tabWidth, rect.y, tabWidth, TabHeight);

                if (currentTab == tab)
                {
                    Widgets.DrawHighlightSelected(tabRect);
                }
                else if (Mouse.IsOver(tabRect))
                {
                    Widgets.DrawHighlight(tabRect);
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tabRect, tab == Tab.RGBSelector ? "Force_BladeColor".Translate() : "Force_HiltAttribute".Translate());
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(tabRect))
                {
                    currentTab = tab;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }
        }

        private void DrawLightsaberPreview(Rect rect)
        {
            // Draw the white bounding box
            Widgets.DrawBoxSolid(rect, SectionBackground);
            Widgets.DrawBox(rect);

            // Create a group for the preview content
            GUI.BeginGroup(rect);
            try
            {
                // Calculate available space with padding
                Rect previewArea = rect.ContractedBy(20f);

                // Calculate maximum possible size while maintaining aspect ratio
                float maxHeight = previewArea.height;
                float maxWidth = previewArea.width;
                float aspectRatio = 1; // Height/width ratio (lightsabers are taller than wide)

                // Calculate final size
                float scaledHeight = Mathf.Min(maxHeight, maxWidth * aspectRatio);
                float scaledWidth = scaledHeight / aspectRatio;

                // Center the preview
                Rect drawRect = new Rect(
                    (rect.width - scaledWidth) / 2,
                    (rect.height - scaledHeight) / 2,
                    scaledWidth,
                    scaledHeight
                );

                // Draw blade with current colors
                if (lightsaberBlade?.bladeGraphic != null)
                {
                    Material bladeMaterial = lightsaberBlade.bladeGraphic.MatSingle;
                    bladeMaterial.SetColor("_Color", bladeColor);
                    bladeMaterial.SetColor("_CoreColor1", coreColor);
                    bladeMaterial.SetColor("_ColorTwo", bladeColor2);
                    bladeMaterial.SetColor("_CoreColor2", coreColor2);

                    // Draw with glow effect
                    GUI.color = Color.white * 1.5f;
                    Graphics.DrawTexture(drawRect, bladeMaterial.mainTexture, bladeMaterial);
                    GUI.color = Color.white;
                }

                // Draw hilt
                if (lightsaberBlade?.HiltManager?.SelectedHilt != null)
                {
                    var hiltGraphic = lightsaberBlade.HiltManager.GetHiltGraphic();
                    var mat = lightsaberBlade.HiltManager.SelectedHilt.graphicData.Graphic.MatSingle;
                    GUI.color = Color.white * 2;

                    MaterialRequest materialRequest = new MaterialRequest
                    {
                        mainTex = mat.mainTexture,
                        maskTex = mat.GetMaskTexture(),
                        shader = mat.shader,
                        color = lightsaberBlade.HiltManager.HiltColorOne,
                        colorTwo = lightsaberBlade.HiltManager.HiltColorTwo
                    };

                    Material material = MaterialPool.MatFrom(materialRequest);
                    GenUI.DrawTextureWithMaterial(drawRect, hiltGraphic.MatSingle.mainTexture, material);
                    GUI.color = Color.white;
                }

                // Draw current hilt name below preview
                if (lightsaberBlade?.HiltManager?.SelectedHilt != null)
                {
                    Text.Anchor = TextAnchor.UpperCenter;
                    Text.Font = GameFont.Medium;
                    string hiltName = lightsaberBlade.HiltManager.SelectedHilt.label;
                    Rect labelRect = new Rect(0, rect.height - 30f, rect.width, 30f);
                    Widgets.Label(labelRect, hiltName);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawRGBControls(Rect rect)
        {
            // Organize in two columns
            float columnWidth = rect.width / 2 - SectionPadding;
            Rect leftColumn = new Rect(rect.x + SectionPadding, rect.y + SectionPadding, columnWidth, rect.height - SectionPadding * 2);
            Rect rightColumn = new Rect(leftColumn.xMax + SectionPadding, rect.y + SectionPadding, columnWidth, rect.height - SectionPadding * 2);

            // Left column - blade settings and presets
            GUI.BeginGroup(leftColumn);
            try
            {
                float yPos = 0;

                // Blade length section
                yPos = DrawSectionWithHeader(ref yPos, leftColumn.width, "Blade Settings", () =>
                {
                    yPos = DrawBladeLengthSlider("Force_VariableBlade1".Translate(), ref lightsaberBlade.bladeLength, leftColumn.width, yPos);
                    yPos = DrawVibrationSlider("Force_Vibration1".Translate(), ref lightsaberBlade.vibrationrate, leftColumn.width, yPos);

                    if (HasSecondBlade)
                    {
                        yPos = DrawBladeLengthSlider("Force_VariableBlade2".Translate(), ref lightsaberBlade.bladeLength2, leftColumn.width, yPos);
                        yPos = DrawVibrationSlider("Force_Vibration2".Translate(), ref lightsaberBlade.vibrationrate2, leftColumn.width, yPos);
                    }

                    return yPos;
                });

                // Sound selector
                yPos = DrawSectionWithHeader(ref yPos, leftColumn.width, "Force_SoundSelector".Translate(), () =>
                {
                    DrawSoundSelector(new Rect(0, yPos, leftColumn.width, 150f), yPos);
                    return yPos + 160f;
                });

                // Color presets moved here
                yPos = DrawSectionWithHeader(ref yPos, leftColumn.width, "Force_Presets".Translate(), () =>
                {
                    DrawPresets(new Rect(0, yPos, leftColumn.width, 120f));
                    return yPos + 130f;
                });
            }
            finally
            {
                GUI.EndGroup();
            }

            // Right column - color pickers only
            GUI.BeginGroup(rightColumn);
            try
            {
                float yPos = 0;

                // Primary blade colors
                yPos = DrawSectionWithHeader(ref yPos, rightColumn.width, "Force_PrimaryColor".Translate(), () =>
                {
                    yPos = DrawColorPicker(ref yPos, rightColumn.width, ref bladeColorRed, ref bladeColorGreen, ref bladeColorBlue, ref bladeColorAlpha, "Blade");
                    yPos = DrawColorPicker(ref yPos, rightColumn.width, ref coreColorRed, ref coreColorGreen, ref coreColorBlue, ref coreColorAlpha, "Core");
                    return yPos;
                });

                // Secondary blade colors (if applicable)
                if (HasSecondBlade)
                {
                    yPos = DrawSectionWithHeader(ref yPos, rightColumn.width, "Force_SecondaryColor".Translate(), () =>
                    {
                        yPos = DrawColorPicker(ref yPos, rightColumn.width, ref bladeColor2Red, ref bladeColor2Green, ref bladeColor2Blue, ref bladeColor2Alpha, "Secondary Blade");
                        yPos = DrawColorPicker(ref yPos, rightColumn.width, ref coreColor2Red, ref coreColor2Green, ref coreColor2Blue, ref coreColor2Alpha, "Secondary Core");
                        return yPos;
                    });
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private float DrawSectionWithHeader(ref float yPos, float width, string header, Func<float> contentDrawer)
        {
            float headerHeight = Text.CalcHeight(header, width);
            Rect headerRect = new Rect(0, yPos, width, headerHeight + ElementPadding * 2);

            // Draw header
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, header);
            Text.Font = GameFont.Small;

            yPos += headerRect.height;

            // Draw content
            yPos = contentDrawer();

            // Draw divider
            Widgets.DrawLineHorizontal(0, yPos, width);
            yPos += ElementPadding * 2;

            return yPos;
        }

        private float DrawColorPicker(ref float yPos, float width, ref int red, ref int green, ref int blue, ref int alpha, string label)
        {
            const float colorPreviewSize = 50f;
            const float randomizeButtonWidth = 100f;
            const float elementSpacing = 5f;

            // Label row
            Rect labelRect = new Rect(0, yPos, width - randomizeButtonWidth - colorPreviewSize - elementSpacing * 2, 22f);
            Widgets.Label(labelRect, label);

            // Randomize button
            if (Widgets.ButtonText(new Rect(labelRect.xMax + elementSpacing, yPos, randomizeButtonWidth, labelRect.height), "Force_Randomize".Translate()))
            {
                RandomizeRGBValues(ref red, ref green, ref blue);
                UpdateCurrentColor();
                UpdateLightsaberColors();
            }

            yPos += labelRect.height + elementSpacing;

            // Draw sliders
            yPos = DrawColorSlider(width, ref red, "Red", yPos);
            yPos = DrawColorSlider(width, ref green, "Green", yPos);
            yPos = DrawColorSlider(width, ref blue, "Blue", yPos);
            yPos = DrawColorSlider(width, ref alpha, "Alpha", yPos);

            // Add extra spacing between color pickers
            yPos += elementSpacing * 2;

            return yPos;
        }

        private float DrawColorSlider(float width, ref int value, string label, float yPos)
        {
            Rect labelRect = new Rect(0, yPos, 40f, 22f);
            Rect sliderRect = new Rect(45f, yPos, width - 110f, 22f);
            Rect fieldRect = new Rect(width - 60f, yPos, 50f, 22f);

            Widgets.Label(labelRect, label);

            // Directly update the value from slider
            value = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, value, MinValue, MaxValue));

            // Convert and update colors immediately
            UpdateCurrentColor();
            UpdateLightsaberColors();

            string buffer = value.ToString();
            Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, MinValue, MaxValue);

            return yPos + 22f + ElementPadding;
        }

        private void UpdateLightsaberColors()
        {
            if (lightsaberBlade?.bladeGraphic?.MatSingle == null)
                return;

            // Update the Comp_LightsaberBlade properties
            lightsaberBlade.bladeColor = bladeColor;
            lightsaberBlade.coreColor = coreColor;
            lightsaberBlade.bladeColor2 = bladeColor2;
            lightsaberBlade.coreColor2 = coreColor2;

            lightsaberBlade.SetShaderProperties();

            // Force graphic refresh
            lightsaberBlade.parent.Notify_ColorChanged();
        }


        private float DrawBladeLengthSlider(string label, ref float bladeLength, float width, float yPos)
        {
            // Draw label
            Widgets.Label(new Rect(0f, yPos, 200f, 22f), label);

            // Draw slider
            float oldLength = bladeLength;
            bladeLength = Widgets.HorizontalSlider(
                new Rect(0f, yPos + 25f, width - 80f, 20f),
                bladeLength,
                lightsaberBlade.Props.minBladeLength1,
                lightsaberBlade.Props.maxBladeLength1
            );

            // Display current value
            Widgets.Label(new Rect(width - 100f, yPos + 45f, 100f, 22f), $"{bladeLength:F2} m");

            // Update shader properties if changed
            if (Math.Abs(oldLength - bladeLength) > 0.001f)
            {
                lightsaberBlade.SetShaderProperties();
            }

            return yPos + 70f;
        }

        private float DrawVibrationSlider(string label, ref float vibrationRate, float width, float yPos)
        {
            // Draw label
            Widgets.Label(new Rect(0f, yPos, 200f, 22f), label);

            // Draw slider
            float oldRate = vibrationRate;
            vibrationRate = Widgets.HorizontalSlider(
                new Rect(0f, yPos + 25f, width - 80f, 20f),
                vibrationRate,
                0.00f,
                500f
            );

            // Display current value
            Widgets.Label(new Rect(width - 100f, yPos + 45f, 100f, 22f), $"{vibrationRate:F2} Hz");

            // Update shader properties if changed
            if (Math.Abs(oldRate - vibrationRate) > 0.001f)
            {
                lightsaberBlade.SetShaderProperties();
            }

            return yPos + 70f;
        }

        private void DrawSoundSelector(Rect inRect, float rowOffset)
        {
            if (lightsaberBlade?.lightsaberSound == null || lightsaberBlade.lightsaberSound.Count == 0)
                return;

            List<SoundDef> soundOptions = lightsaberBlade.lightsaberSound;

            // Draw the menu section background
            Widgets.DrawMenuSection(inRect);

            // Draw the header
            Text.Font = GameFont.Small;
            Rect headerRect = new Rect(inRect.x + SectionPadding, inRect.y, inRect.width - SectionPadding * 2, 22f);

            // Calculate sizes for scroll view
            float optionHeight = 25f;
            float totalContentHeight = soundOptions.Count * optionHeight;
            float visibleHeight = inRect.height - headerRect.height - SectionPadding;

            // Set up the scroll view area
            Rect scrollViewRect = new Rect(
                inRect.x + SectionPadding,
                headerRect.yMax,
                inRect.width - SectionPadding * 2, // Narrower to account for scrollbar
                visibleHeight
            );

            Rect scrollContentRect = new Rect(
                0f,
                0f,
                scrollViewRect.width - 16f, // Reserve space for scrollbar
                totalContentHeight
            );

            // Begin the scroll view
            Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, scrollContentRect);

            // Draw each sound option
            for (int i = 0; i < soundOptions.Count; i++)
            {
                Rect optionRect = new Rect(
                    0f,
                    i * optionHeight,
                    scrollContentRect.width,
                    optionHeight
                );

                bool isSelected = lightsaberBlade.selectedSoundIndex == i;

                // Draw the radio button
                if (Widgets.RadioButtonLabeled(optionRect, soundOptions[i].label, isSelected))
                {
                    if (!isSelected)
                    {
                        lightsaberBlade.selectedSoundIndex = i;
                        soundOptions[i].PlayOneShot(lightsaberBlade.parent);
                        lightsaberBlade.SetSoundEffect(soundOptions[i]);
                    }
                }

                // Highlight on mouseover
                if (Mouse.IsOver(optionRect))
                {
                    Widgets.DrawHighlight(optionRect);
                }
            }

            Widgets.EndScrollView();
        }

        private Vector2 presetScrollPos; // Add this with other scroll position fields

        private void DrawPresets(Rect inRect)
        {
            if (lightsaberBlade?.HiltManager?.SelectedHilt == null)
            {
                Widgets.Label(inRect, "No hilt selected");
                return;
            }

            // Initialize presets with basic colors
            var presets = new Dictionary<string, Color>
                {
                    { "Blue", new Color(0, 0, 1) },
                    { "Red", new Color(1, 0, 0) },
                    { "Green", new Color(0, 1, 0) },
                    { "Yellow", new Color(1, 1, 0) },
                    { "Purple", new Color(0.5f, 0, 0.5f) },
                    { "Orange", new Color(1, .64f, 0) },
                    { "White", new Color(1, 1, 1) },
                    { "Cyan", new Color(0, 1, 1) },
                    { "Magenta", new Color(1, 0, 1) }
                };


            var colorPart = lightsaberBlade.HiltManager.SelectedHiltParts
                            .FirstOrDefault(p => p.colorGenerator != null &&
                                                p.category != null &&
                                                p.category.canChangeColor);

            if (colorPart != null)
            {
                presets.Add($"{colorPart.category.label} Color", colorPart.colorGenerator.NewRandomizedColor());
            }

            if (ModsConfig.IdeologyActive && pawn != null)
            {
                if (pawn.Ideo != null) presets.Add("Ideology", pawn.Ideo.Color);
                if (pawn.story?.favoriteColor != null) presets.Add("Favorite", pawn.story.favoriteColor.Value);
            }

            // Layout parameters
            const float buttonWidth = 90f;
            const float buttonHeight = 25f;
            const float colorSwatchSize = 20f;
            const float horizontalSpacing = 8f;
            const float verticalSpacing = 8f;
            const float columnPadding = 5f;

            // Calculate columns
            float elementWidth = buttonWidth + colorSwatchSize + horizontalSpacing;
            int columns = Mathf.FloorToInt((inRect.width - columnPadding) / elementWidth);
            columns = Mathf.Clamp(columns, 2, 6); // Minimum 2, maximum 6 columns

            // Calculate required height
            int rows = Mathf.CeilToInt((float)presets.Count / columns);
            float totalHeight = rows * (buttonHeight + verticalSpacing);

            // Begin scroll view if needed
            bool useScroll = totalHeight > inRect.height;
            Rect viewRect = useScroll
                ? new Rect(0, 0, inRect.width - 20f, totalHeight)
                : inRect;

            if (useScroll)
            {
                Widgets.BeginScrollView(inRect, ref presetScrollPos, viewRect);
            }

            // Draw presets in grid
            int index = 0;
            foreach (var preset in presets)
            {
                int row = index / columns;
                int col = index % columns;

                float x = viewRect.x + col * elementWidth;
                float y = viewRect.y + row * (buttonHeight + verticalSpacing);

                // Button
                Rect buttonRect = new Rect(x, y, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(buttonRect, preset.Key))
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    confirmAction(preset.Value, coreColor, preset.Value, coreColor2);
                    SyncToColor(preset.Value, coreColor, preset.Value, coreColor2);
                    UpdateCurrentColor();
                    lightsaberBlade.SetShaderProperties();
                }

                // Color swatch
                Rect swatchRect = new Rect(
                    buttonRect.xMax + 2f,
                    buttonRect.y + (buttonHeight - colorSwatchSize) / 2,
                    colorSwatchSize,
                    colorSwatchSize
                );
                Widgets.DrawBoxSolid(swatchRect, preset.Value);
                Widgets.DrawBox(swatchRect);

                // Tooltip
                TooltipHandler.TipRegion(buttonRect, $"RGB: {preset.Value.r:F2}, {preset.Value.g:F2}, {preset.Value.b:F2}");

                index++;
            }

            if (useScroll)
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawHiltControls(Rect rect)
        {
            // Split into top and bottom sections with better proportions
            Rect topSection = new Rect(rect.x, rect.y, rect.width, 150f); // Increased height
            Rect bottomSection = new Rect(rect.x, topSection.yMax + SectionPadding, rect.width, rect.height - topSection.height - SectionPadding);

            // Top section - hilt selection and colors
            GUI.BeginGroup(topSection);
            try
            {
                // Draw section background
                Widgets.DrawBoxSolid(new Rect(0, 0, topSection.width, topSection.height), SectionBackground);
                Widgets.DrawBox(new Rect(0, 0, topSection.width, topSection.height));

                // Add padding inside the section
                Rect innerTopRect = new Rect(SectionPadding, SectionPadding, topSection.width - SectionPadding * 2, topSection.height - SectionPadding * 2);
                GUI.BeginGroup(innerTopRect);
                try
                {
                    float yPos = 0;

                    // Hilt selection
                    yPos = DrawHiltSelectionControls(innerTopRect.width, yPos);

                    // Add some spacing between sections
                    yPos += 15f;

                    // Color controls
                    yPos = DrawHiltColorControls(innerTopRect.width, yPos);
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            finally
            {
                GUI.EndGroup();
            }

            // Bottom section - hilt parts
            GUI.BeginGroup(bottomSection);
            try
            {
                // Draw section background
                Widgets.DrawBoxSolid(new Rect(0, 0, bottomSection.width, bottomSection.height), SectionBackground);
                Widgets.DrawBox(new Rect(0, 0, bottomSection.width, bottomSection.height));

                // Add padding inside the section
                Rect innerBottomRect = new Rect(SectionPadding, SectionPadding, bottomSection.width - SectionPadding * 2, bottomSection.height - SectionPadding * 2);
                DrawHiltPartSelection(innerBottomRect);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private float DrawHiltSelectionControls(float width, float yPos)
        {
            const float buttonWidth = 120f;
            const float spacing = 10f;
            const float controlHeight = 40f;

            // Calculate total width needed for all three buttons
            float totalWidth = (buttonWidth * 3) + (spacing * 2);
            float startX = (width - totalWidth) / 2; // Center the group of buttons

            // Previous button (leftmost)
            if (Widgets.ButtonText(new Rect(startX, yPos, buttonWidth, controlHeight), "Force_Previous".Translate()))
            {
                CycleHilt(-1);
            }


            // Current hilt button (middle)
            string hiltLabel = lightsaberBlade?.HiltManager?.SelectedHilt?.label ?? "Force_SelectHilt".Translate();
            if (Widgets.ButtonText(new Rect(startX + buttonWidth + spacing, yPos, buttonWidth, controlHeight), hiltLabel))
            {
                ShowHiltSelectionMenu();
            }

            // Next button (rightmost)
            if (Widgets.ButtonText(new Rect(startX + (buttonWidth + spacing) * 2, yPos, buttonWidth, controlHeight), "Force_Next".Translate()))
            {
                CycleHilt(1);
            }

            return yPos + controlHeight;
        }

        private float DrawHiltColorControls(float width, float yPos)
        {
            const float buttonWidth = 120f;
            const float colorBoxSize = 30f;
            const float spacing = 10f;
            const float controlHeight = 40f;
            const float verticalSpacing = 15f; // Added vertical spacing between sections

            // First move down by the spacing
            yPos += verticalSpacing;

            // Calculate total width needed
            float totalWidth = (buttonWidth * 2) + (colorBoxSize * 2) + (spacing * 3);
            float startX = (width - totalWidth) / 2;

            // Primary color
            Rect primaryColorRect = new Rect(startX, yPos, colorBoxSize, colorBoxSize);
            Widgets.DrawBoxSolid(primaryColorRect, lightsaberBlade.HiltManager.HiltColorOne);
            Widgets.DrawBox(primaryColorRect);
            TooltipHandler.TipRegion(primaryColorRect, "Primary hilt color");

            Rect primaryButtonRect = new Rect(primaryColorRect.xMax + spacing, yPos, buttonWidth, controlHeight);
            if (Widgets.ButtonText(primaryButtonRect, "Primary Color"))
            {
                ShowColorPicker(true);
            }

            // Secondary color
            Rect secondaryColorRect = new Rect(primaryButtonRect.xMax + spacing, yPos, colorBoxSize, colorBoxSize);
            Widgets.DrawBoxSolid(secondaryColorRect, lightsaberBlade.HiltManager.HiltColorTwo);
            Widgets.DrawBox(secondaryColorRect);
            TooltipHandler.TipRegion(secondaryColorRect, "Secondary hilt color");

            Rect secondaryButtonRect = new Rect(secondaryColorRect.xMax + spacing, yPos, buttonWidth, controlHeight);
            if (Widgets.ButtonText(secondaryButtonRect, "Secondary Color"))
            {
                ShowColorPicker(false);
            }

            return yPos + controlHeight + verticalSpacing; // Return position including the bottom spacing
        }

        private void DrawHiltPartSelection(Rect rect)
        {
            // Constants for consistent spacing
            const float headerTopPadding = 5f;
            const float sectionPadding = 5f;
            const float categorySpacing = 8f;

            // Draw section header
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(headerTopPadding, headerTopPadding, rect.width - headerTopPadding * 2, 32f);
            Widgets.Label(headerRect, "Hilt Components");
            Text.Font = GameFont.Small;

            // Calculate content area (below header)
            float contentHeight = rect.height - headerRect.height - headerTopPadding * 2;
            Rect contentRect = new Rect(0, headerRect.yMax + sectionPadding, rect.width, contentHeight);

            List<HiltPartCategoryDef> orderedCategories = lightsaberBlade.Props.allowedCategories ?? new List<HiltPartCategoryDef>();
    
    // Get all hilt parts first
    var allHiltParts = DefDatabase<HiltPartDef>.AllDefsListForReading;
    
    // Find categories that actually have parts available
    var availableCategories = orderedCategories
        .Where(oc => oc != null && allHiltParts.Any(p => p?.category == oc)) // Compare def references directly
        .ToList();

    Log.Message($"Ordered Categories: {orderedCategories.Count}, Available Categories: {availableCategories.Count}");
    
    // Debug output to see what's happening
    foreach (var cat in orderedCategories)
    {
        int partCount = allHiltParts.Count(p => p?.category == cat);
        Log.Message($"Category {cat?.defName ?? "NULL"} has {partCount} parts");
    }



            float totalHeight = CalculateHiltPartsTotalHeight(availableCategories);
            Widgets.BeginScrollView(contentRect, ref hiltComponentsScrollPos, new Rect(0, 0, rect.width - 16f, totalHeight));
            try
            {
                float yPos = 0;
                foreach (HiltPartCategoryDef category in availableCategories)
                {
                    yPos = DrawHiltCategoryCard(rect.width - 16f, yPos, category);
                    yPos += categorySpacing;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            foreach (var cat in availableCategories)
            {
                Log.Message($"Found category: {cat.defName}");
            }
        }

        private float DrawHiltCategoryCard(float width, float yPos, HiltPartCategoryDef category)
        {
            // Spacing constants
            const float cardPadding = 10f;
            const float lineHeight = 22f;
            const float buttonHeight = 30f;
            const float statIndent = 15f;
            const float elementSpacing = 6f;
            if (category == null) return yPos;

            HiltPartDef currentPart = lightsaberBlade.HiltManager.GetHiltPartByCategory(category);
            float cardHeight = EstimateCategoryHeight(category);
            Rect cardRect = new Rect(0, yPos, width, cardHeight);
            Widgets.DrawBoxSolid(cardRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Widgets.DrawBox(cardRect);


            Rect headerRect = new Rect(
                cardPadding,
                yPos + cardPadding,
                width - cardPadding * 2 - 120f - cardPadding,
                lineHeight
            );

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect, category.label.SplitCamelCase());
            Text.Anchor = TextAnchor.UpperLeft;

            // Browse button (aligned with header)
            Rect browseButtonRect = new Rect(
                width - 120f - cardPadding,
                yPos + cardPadding,
                120f,
                buttonHeight
            );

            if (Widgets.ButtonText(browseButtonRect, "Browse Options"))
            {
                Find.WindowStack.Add(new HiltPartSelectionWindow(category, pawn, lightsaberBlade));
            }

            // Current part info
            float contentY = headerRect.yMax + elementSpacing;
            Rect currentLabelRect = new Rect(
                cardPadding,
                contentY,
                width - cardPadding * 2,
                lineHeight
            );

            Widgets.Label(currentLabelRect, currentPart != null ? $"Current: {currentPart.label}" : "Current: None");
            contentY += lineHeight;

            // Current part stats
            if (currentPart?.equippedStatOffsets != null)
            {
                foreach (var statMod in currentPart.equippedStatOffsets)
                {
                    Rect statRect = new Rect(
                        cardPadding + statIndent,
                        contentY,
                        width - cardPadding * 2 - statIndent,
                        lineHeight
                    );

                    Widgets.Label(statRect, $"{statMod.stat.label}: {statMod.value.ToStringWithSign("0.##")}");
                    contentY += lineHeight;
                }
            }

            // Update final card height based on actual content
            cardRect.height = contentY - yPos + cardPadding; // Add bottom padding

            return cardRect.yMax;
        }

        private float EstimateCategoryHeight(HiltPartCategoryDef category)
        {
            const float baseHeight = 70f; // Header + button + current label
            const float statHeight = 22f;
            const float bottomPadding = 10f;

            HiltPartDef part = lightsaberBlade.HiltManager.GetHiltPartByCategory(category);
            float height = baseHeight;

            if (part?.equippedStatOffsets != null)
            {
                height += part.equippedStatOffsets.Count * statHeight;
            }

            return height + bottomPadding;
        }

        private float CalculateHiltPartsTotalHeight(List<HiltPartCategoryDef> categories)
        {
            const float categorySpacing = 8f;
            float height = 0f;

            foreach (HiltPartCategoryDef category in categories)
            {
                height += EstimateCategoryHeight(category) + categorySpacing;
            }

            return height + 10f; // Extra bottom buffer
        }


        private void ShowHiltSelectionMenu()
        {
            if (lightsaberBlade?.Props?.availableHiltGraphics == null) return;

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (HiltDef hiltGraphic in lightsaberBlade.Props.availableHiltGraphics)
            {
                HiltDef localHiltGraphic = hiltGraphic;

                // Create a tooltip with more information
                string tooltip = $"<b>{localHiltGraphic.label}</b>";
                if (!localHiltGraphic.description.NullOrEmpty())
                {
                    tooltip += $"\n\n{localHiltGraphic.description}";
                }

                // Create the option with the tooltip
                options.Add(new FloatMenuOption(
                    label: localHiltGraphic.label,
                    action: () =>
                    {
                        lightsaberBlade.HiltManager.SelectedHilt = localHiltGraphic;
                        lightsaberBlade.HiltManager.UpdateHiltGraphic();
                        lightsaberBlade.parent.Notify_ColorChanged();
                    },
                    extraPartWidth: 24f,
                    extraPartOnGUI: (Rect rect) =>
                    {
                        return false;
                    }

                ));
            }

            // Create a scrollable float menu if there are many options
            if (options.Count > 10)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
            else
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void CycleHilt(int direction)
        {
            if (lightsaberBlade?.Props?.availableHiltGraphics == null) return;

            int currentIndex = lightsaberBlade.Props.availableHiltGraphics.IndexOf(lightsaberBlade.HiltManager.SelectedHilt);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = (currentIndex + direction) % lightsaberBlade.Props.availableHiltGraphics.Count;
            if (newIndex < 0) newIndex = lightsaberBlade.Props.availableHiltGraphics.Count - 1;

            lightsaberBlade.HiltManager.SelectedHilt = lightsaberBlade.Props.availableHiltGraphics[newIndex];
            lightsaberBlade.HiltManager.UpdateHiltGraphic();
            lightsaberBlade.parent.Notify_ColorChanged();
        }

        private void ShowColorPicker(bool isPrimaryColor)
        {
            Find.WindowStack.Add(new StuffColorSelectionWindow((Color selectedColor) =>
            {
                if (isPrimaryColor)
                    lightsaberBlade.HiltManager.HiltColorOne = selectedColor;
                else
                    lightsaberBlade.HiltManager.HiltColorTwo = selectedColor;

                lightsaberBlade.HiltManager.UpdateHiltGraphic();
                lightsaberBlade.parent.Notify_ColorChanged();
            }));
        }

        private bool HasSecondBlade
        {
            get
            {
                if (lightsaberBlade?.bladeGraphic?.MatSingle == null)
                    return false;

                return lightsaberBlade.bladeGraphic.MatSingle.HasProperty("_ColorTwo") ||
                       lightsaberBlade.bladeGraphic.MatSingle.HasProperty("_CoreColor2");
            }
        }

        private void RandomizeRGBValues(ref int red, ref int green, ref int blue)
        {
            red = UnityEngine.Random.Range(0, 256);
            green = UnityEngine.Random.Range(0, 256);
            blue = UnityEngine.Random.Range(0, 256);
        }

        private void SyncToColor(Color bladeColor, Color coreColor, Color bladeColor2, Color coreColor2)
        {
            SyncBladeColor(bladeColor);
            SyncCoreColor(coreColor);
            SyncBlade2Color(bladeColor2);
            SyncCore2Color(coreColor2);
        }

        private void SyncBladeColor(Color color)
        {
            bladeColorRed = Mathf.RoundToInt(color.r * 255f);
            bladeColorGreen = Mathf.RoundToInt(color.g * 255f);
            bladeColorBlue = Mathf.RoundToInt(color.b * 255f);
            bladeColorAlpha = Mathf.RoundToInt(color.a * 255f);
        }

        private void SyncCoreColor(Color color)
        {
            coreColorRed = Mathf.RoundToInt(color.r * 255f);
            coreColorGreen = Mathf.RoundToInt(color.g * 255f);
            coreColorBlue = Mathf.RoundToInt(color.b * 255f);
            coreColorAlpha = Mathf.RoundToInt(color.a * 255f);
        }

        private void SyncBlade2Color(Color color)
        {
            bladeColor2Red = Mathf.RoundToInt(color.r * 255f);
            bladeColor2Green = Mathf.RoundToInt(color.g * 255f);
            bladeColor2Blue = Mathf.RoundToInt(color.b * 255f);
            bladeColor2Alpha = Mathf.RoundToInt(color.a * 255f);
        }

        private void SyncCore2Color(Color color)
        {
            coreColor2Red = Mathf.RoundToInt(color.r * 255f);
            coreColor2Green = Mathf.RoundToInt(color.g * 255f);
            coreColor2Blue = Mathf.RoundToInt(color.b * 255f);
            coreColor2Alpha = Mathf.RoundToInt(color.a * 255f);
        }

        private void UpdateCurrentColor()
        {
            bladeColor = new Color(bladeColorRed / 255f, bladeColorGreen / 255f, bladeColorBlue / 255f);
            coreColor = new Color(coreColorRed / 255f, coreColorGreen / 255f, coreColorBlue / 255f);
            bladeColor2 = new Color(bladeColor2Red / 255f, bladeColor2Green / 255f, bladeColor2Blue / 255f);
            coreColor2 = new Color(coreColor2Red / 255f, coreColor2Green / 255f, coreColor2Blue / 255f);
        }

    }

    public static class StringExtensions
    {
        public static string SplitCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            StringBuilder result = new StringBuilder();
            result.Append(str[0]);

            for (int i = 1; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]) && !char.IsUpper(str[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(str[i]);
            }

            return result.ToString();
        }
    }

    public class HiltPartSelectionWindow : Window
    {
        private readonly HiltPartCategoryDef category;
        private readonly Pawn pawn;
        private readonly Comp_LightsaberBlade lightsaberBlade;
        private Vector2 scrollPosition;
        private HiltPartDef selectedPart;

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public HiltPartSelectionWindow(HiltPartCategoryDef category, Pawn pawn, Comp_LightsaberBlade lightsaberBlade)
        {
            this.category = category;
            this.pawn = pawn;
            this.lightsaberBlade = lightsaberBlade;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Window title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0, 0, inRect.width, 35f);
            Widgets.Label(titleRect, $"{category.label.SplitCamelCase()} Options"); // Updated to use string category
            Text.Font = GameFont.Small;

            // Install button positioned above close button
            float installButtonHeight = 35f;
            float closeButtonHeight = CloseButSize.y;
            Rect installButtonRect = new Rect(
                inRect.width / 2 - 100f,
                inRect.height - installButtonHeight - closeButtonHeight - 15f, // 15f spacing
                200f,
                installButtonHeight
            );

            // Main content area
            Rect contentRect = new Rect(
                0,
                titleRect.yMax + 10f,
                inRect.width,
                installButtonRect.y - titleRect.yMax - 15f
            );

            // Get all parts for this category
            List<HiltPartDef> availableParts = DefDatabase<HiltPartDef>.AllDefsListForReading
                .Where(p => p.category == category)
                .OrderBy(p => p.label)
                .ToList();

            // Calculate total content height
            float totalHeight = availableParts.Sum(p => CalculatePartHeight(p) + 8f) + 10f;

            // Scroll view
            Widgets.BeginScrollView(contentRect, ref scrollPosition, new Rect(0, 0, contentRect.width - 16f, totalHeight));
            try
            {
                float yPos = 0;
                foreach (HiltPartDef part in availableParts)
                {
                    // Draw white outline background
                    Rect outlineRect = new Rect(0, yPos, contentRect.width - 16f, CalculatePartHeight(part));
                    Widgets.DrawBoxSolid(outlineRect, Color.white);

                    // Draw actual part card
                    float cardHeight = DrawPartCard(contentRect.width - 16f, yPos, part);
                    yPos += cardHeight + 8f;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            // Install button
            if (Widgets.ButtonText(installButtonRect, "Install Selected") && selectedPart != null)
            {
                TryInstallPart(selectedPart);
                Close();
            }

            // Disable button if nothing selected
            if (selectedPart == null)
            {
                GUI.color = Color.gray;
                Widgets.DrawBoxSolid(installButtonRect, new Color(0.3f, 0.3f, 0.3f, 0.8f));
                GUI.color = Color.white;
            }
        }

        private float DrawPartCard(float width, float yPos, HiltPartDef part)
        {
            const float padding = 8f;
            const float lineHeight = 20f;
            const float indent = 15f;
            const float elementSpacing = 6f;

            float cardHeight = CalculatePartHeight(part);

            // Main card background (drawn inside white outline)
            Rect cardRect = new Rect(1, yPos + 1, width - 2, cardHeight - 2); // Shrink to fit inside outline
            bool isSelected = selectedPart == part;
            bool isCurrent = lightsaberBlade.HiltManager.GetHiltPartByCategory(category) == part;

            Widgets.DrawBoxSolid(cardRect,
                isSelected ? new Color(0.15f, 0.25f, 0.35f, 0.7f) :  // Darker blue with more opacity
                   isCurrent ? new Color(0.1f, 0.3f, 0.1f, 0.7f) :      // Darker green with more opacity
                   new Color(0.1f, 0.1f, 0.1f, 1f));

            // Part name
            Rect nameRect = new Rect(padding + 1, yPos + 1 + padding, width - padding * 2, lineHeight);
            Text.Font = GameFont.Small;
            Widgets.Label(nameRect, part.label.CapitalizeFirst());

            // Requirements
            float reqWidth = Text.CalcSize(part.requiredComponent?.label ?? "No requirements").x + 50f;
            Rect reqRect = new Rect(width - reqWidth - padding - 1, yPos + 1 + padding, reqWidth, lineHeight);
            Text.Font = GameFont.Tiny;
            string reqText = part.requiredComponent != null ?
                $"Req: {part.requiredComponent.label}" :
                "No requirements";
            Widgets.Label(reqRect, reqText);
            Text.Font = GameFont.Small;

            // Stats
            float statY = nameRect.yMax + elementSpacing;
            if (part.equippedStatOffsets != null && part.equippedStatOffsets.Count > 0)
            {
                Text.Font = GameFont.Tiny;
                foreach (var statMod in part.equippedStatOffsets)
                {
                    Rect statRect = new Rect(padding + indent + 1, statY, width - padding * 2 - indent, lineHeight);
                    Widgets.Label(statRect, $"• {statMod.stat.label}: {statMod.value.ToStringWithSign("0.#")}");
                    statY += lineHeight + elementSpacing * 0.5f;
                }
                Text.Font = GameFont.Small;
                statY += elementSpacing;
            }

            // Description
            if (!part.description.NullOrEmpty())
            {
                Text.Font = GameFont.Tiny;
                Rect descRect = new Rect(
                    padding + 1,
                    statY + elementSpacing,
                    width - padding * 2,
                    Text.CalcHeight(part.description, width - padding * 2)
                );
                Widgets.Label(descRect, part.description.Colorize(new Color(0.8f, 0.8f, 0.8f)));
                statY += descRect.height + elementSpacing;
                Text.Font = GameFont.Small;
            }

            // Click target
            if (Widgets.ButtonInvisible(new Rect(0, yPos, width, statY - yPos + padding)))
            {
                selectedPart = part;
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            return cardHeight;
        }

        private float CalculatePartHeight(HiltPartDef part)
        {
            const float baseHeight = 44f;
            const float statHeight = 20f;
            const float descLineHeight = 16f;
            const float minHeight = 54f;

            float height = baseHeight;

            if (part.equippedStatOffsets != null)
            {
                height += part.equippedStatOffsets.Count * (statHeight + 3f);
            }

            if (!part.description.NullOrEmpty())
            {
                float descWidth = InitialSize.x - 40f;
                int descLines = Mathf.CeilToInt(Text.CalcHeight(part.description, descWidth) / descLineHeight);
                height += descLines * descLineHeight + 10f;
            }

            return Mathf.Max(height, minHeight);
        }

        private void TryInstallPart(HiltPartDef part)
        {
            Thing requiredComponent = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(part.requiredComponent),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                999f
            );

            if (requiredComponent == null)
            {
                Messages.Message($"No {part.requiredComponent.label} found for installation!", MessageTypeDefOf.RejectInput, false);
                return;
            }

            HiltPartDef previousPart = lightsaberBlade.HiltManager.GetHiltPartByCategory(part.category);
            var job = new Job_UpgradeLightsaber
            {
                def = LightsaberDefOf.Force_UpgradeLightsaber,
                selectedhiltPartDef = part,
                previoushiltPartDef = previousPart,
                targetA = requiredComponent
            };

            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, true);
        }
    }
}