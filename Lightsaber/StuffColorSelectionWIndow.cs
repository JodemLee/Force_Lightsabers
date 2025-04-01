using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class StuffColorSelectionWindow : Window
    {
        private readonly Action<Color> onColorSelected;
        private readonly Dictionary<StuffCategoryDef, List<ThingDef>> categorizedStuffDefs;
        private readonly List<StuffCategoryDef> categories;
        private StuffCategoryDef selectedCategory;
        private string searchString = "";
        private Vector2 scrollPosition = Vector2.zero;

        // Custom RGBA values
        private float customRed = 1f;
        private float customGreen = 1f;
        private float customBlue = 1f;
        private float customAlpha = 1f;

        public StuffColorSelectionWindow(Action<Color> onColorSelected)
        {
            this.onColorSelected = onColorSelected;
            categorizedStuffDefs = new Dictionary<StuffCategoryDef, List<ThingDef>>();
            InitializeStuffDefs();
            categories = categorizedStuffDefs.Keys.ToList();
            selectedCategory = categories.FirstOrDefault();

            // Window properties
            forcePause = false;
            draggable = true;
            doCloseButton = false;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        private void InitializeStuffDefs()
        {
            foreach (var def in StuffColorUtility.GetAllStuffed())
            {
                if (def.stuffProps?.categories != null)
                {
                    foreach (var category in def.stuffProps.categories)
                    {
                        if (!categorizedStuffDefs.ContainsKey(category))
                        {
                            categorizedStuffDefs[category] = new List<ThingDef>();
                        }
                        categorizedStuffDefs[category].Add(def);
                    }
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Draw tabs at the top
            DrawTabs(new Rect(0f, 0f, inRect.width, 40f));

            if (selectedCategory != null)
            {
                if (selectedCategory.defName == "CustomRGBA")
                {
                    // Draw custom RGBA panel
                    DrawCustomRGBAPanel(new Rect(0f, 50f, inRect.width, inRect.height - 50f));
                }
                else
                {
                    // Draw search bar
                    DrawSearchBar(new Rect(0f, 50f, inRect.width - 20f, 30f));

                    // Draw content below the search bar
                    Rect contentRect = new Rect(0f, 90f, inRect.width, inRect.height - 90f); // Adjusted y-position for offset
                    DrawContent(contentRect);
                }
            }
        }

        private void DrawTabs(Rect rect)
        {
            // Add a new tab for custom RGBA
            var allCategories = new List<StuffCategoryDef>(categories);
            allCategories.Add(new StuffCategoryDef { defName = "CustomRGBA", label = "Custom RGBA" });

            float tabWidth = rect.width / allCategories.Count;
            for (int i = 0; i < allCategories.Count; i++)
            {
                var category = allCategories[i];
                var tabRect = new Rect(i * tabWidth, rect.y, tabWidth, 40f);
                var isActiveTab = selectedCategory == category;

                if (isActiveTab)
                {
                    Widgets.DrawHighlight(tabRect);
                }

                if (Widgets.ButtonText(tabRect, category.label))
                {
                    selectedCategory = category;
                    scrollPosition = Vector2.zero;
                }
            }
        }

        private void DrawSearchBar(Rect rect)
        {
            // Draw the text field
            searchString = Widgets.TextField(rect, searchString, 25);

            // Draw placeholder text if the search string is empty
            if (string.IsNullOrEmpty(searchString))
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.gray;
                Widgets.Label(rect, "Search materials...");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawContent(Rect rect)
        {
            if (selectedCategory == null) return;

            var filteredDefs = categorizedStuffDefs[selectedCategory]
                .Where(def => string.IsNullOrEmpty(searchString) || def.label.ToLower().Contains(searchString.ToLower()))
                .ToList();

            if (filteredDefs.Count == 0)
            {
                Widgets.Label(rect, "No materials found.");
                return;
            }

            // Adjust the viewRect to account for the offset
            var viewRect = new Rect(0f, 0f, rect.width - 16f, filteredDefs.Count * 35f + 10f); // Start at y = 0f inside the scroll view
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float yPos = 0f;
            foreach (var def in filteredDefs)
            {
                var defColor = StuffColorUtility.GetStuffColor(def);
                var defMaterial = def.graphic?.MatSingleFor(null) ?? BaseContent.BadMat;

                DrawThingDefRow(new Rect(10f, yPos, viewRect.width - 20f, 30f), def, defColor, defMaterial);
                yPos += 35f;
            }

            Widgets.EndScrollView();
        }

        private void DrawThingDefRow(Rect rect, ThingDef def, Color defColor, Material defMaterial)
        {
            // Draw graphic
            var graphicRect = new Rect(rect.x, rect.y + 5f, 30f, 30f);
            if (defMaterial != BaseContent.BadMat)
            {
                GUI.color = defColor;
                GUI.DrawTexture(graphicRect, defMaterial.mainTexture);
                GUI.color = Color.white;
            }
            else
            {
                Widgets.DrawTextureFitted(graphicRect, BaseContent.BadTex, 1f);
            }

            // Draw button
            var buttonRect = new Rect(graphicRect.xMax + 10f, rect.y, rect.width - 120f, 30f);
            if (Widgets.ButtonText(buttonRect, def.label))
            {
                onColorSelected(defColor);
            }

            // Draw color box
            var colorRect = new Rect(buttonRect.xMax + 5f, rect.y + 5f, 20f, 20f);
            Widgets.DrawBoxSolid(colorRect, defColor);
        }

        private void DrawCustomRGBAPanel(Rect rect)
        {
            // Draw sliders for RGBA values
            customRed = Widgets.HorizontalSlider(new Rect(rect.x + 10f, rect.y + 20f, rect.width - 20f, 30f), customRed, 0f, 1f, label: $"Red: {customRed:F2}");
            customGreen = Widgets.HorizontalSlider(new Rect(rect.x + 10f, rect.y + 60f, rect.width - 20f, 30f), customGreen, 0f, 1f, label: $"Green: {customGreen:F2}");
            customBlue = Widgets.HorizontalSlider(new Rect(rect.x + 10f, rect.y + 100f, rect.width - 20f, 30f), customBlue, 0f, 1f, label: $"Blue: {customBlue:F2}");
            customAlpha = Widgets.HorizontalSlider(new Rect(rect.x + 10f, rect.y + 140f, rect.width - 20f, 30f), customAlpha, 0f, 1f, label: $"Alpha: {customAlpha:F2}");

            // Draw preview of the custom color
            var previewColor = new Color(customRed, customGreen, customBlue, customAlpha);
            Widgets.DrawBoxSolid(new Rect(rect.x + 10f, rect.y + 180f, rect.width - 20f, 30f), previewColor);

            // Draw confirm button
            if (Widgets.ButtonText(new Rect(rect.x + 10f, rect.y + 220f, rect.width - 20f, 30f), "Confirm Custom Color"))
            {
                onColorSelected(previewColor);
            }
        }
    }
}