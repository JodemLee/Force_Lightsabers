using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class HiltManager
    {
        private Graphic _cachedHiltGraphic;
        private bool _needsUpdate = true;
        public MaterialPropertyBlock HiltMaterialPropertyBlock { get; } = new MaterialPropertyBlock();

        public List<HiltDef> AvailableHilts { get; set; } = new List<HiltDef>();

        private HiltDef _selectedHilt;
        private List<HiltPartDef> _selectedHiltParts = new List<HiltPartDef>();

        public HiltDef SelectedHilt
        {
            get => _selectedHilt;
            set
            {
                if (_selectedHilt != value)
                {
                    _selectedHilt = value;
                    _needsUpdate = true; // Mark the hilt graphic as needing an update
                }
            }
        }

        public List<HiltPartDef> SelectedHiltParts
        {
            get => _selectedHiltParts;
            set
            {
                if (_selectedHiltParts != value)
                {
                    _selectedHiltParts = value;
                    _needsUpdate = true; // Mark the hilt graphic as needing an update
                }
            }
        }

        public Color _hiltColorOne;
        public Color _hiltColorTwo;

        public Color HiltColorOne
        {
            get => _hiltColorOne;
            set
            {
                if (_hiltColorOne != value)
                {
                    _hiltColorOne = value;
                    _needsUpdate = true;
                }
            }
        }

        public Color HiltColorTwo
        {
            get => _hiltColorTwo;
            set
            {
                if (_hiltColorTwo != value)
                {
                    _hiltColorTwo = value;
                    _needsUpdate = true;
                }
            }
        }

        public void UpdateHiltGraphic()
        {
            _needsUpdate = true;
        }

        public Graphic GetHiltGraphic()
        {
            if (_needsUpdate)
            {
                _cachedHiltGraphic = CreateHiltGraphic();
                _needsUpdate = false;
            }
            return _cachedHiltGraphic;
        }

        private Graphic CreateHiltGraphic()
        {
            if (SelectedHilt == null || SelectedHilt.graphicData == null)
            {
                return DefaultHiltGraphic();
            }

            // Create the hilt graphic with the specified colors
            var hiltGraphic = SelectedHilt.graphicData.Graphic.GetColoredVersion(
                SelectedHilt.graphicData.Graphic.Shader,
                HiltColorOne,
                HiltColorTwo
            );

            // Apply the MaterialPropertyBlock to the hilt's material
            ApplyMaterialPropertyBlock(hiltGraphic);

            return hiltGraphic;
        }

        private void ApplyMaterialPropertyBlock(Graphic hiltGraphic)
        {
            if (hiltGraphic?.MatSingle == null)
            {
                return;
            }

            // Set properties in the MaterialPropertyBlock
            HiltMaterialPropertyBlock.SetColor("_Color", HiltColorOne);
            HiltMaterialPropertyBlock.SetColor("_ColorTwo", HiltColorTwo);
        }

        private Graphic DefaultHiltGraphic()
        {
            // Fallback to a default hilt graphic if no hilt is selected
            return GraphicDatabase.Get<Graphic_Single>("DefaultHiltPath", ShaderDatabase.Cutout, Vector2.one, Color.white, Color.gray);
        }

        public void AddHiltPart(HiltPartDef hiltPart)
        {
            if (!_selectedHiltParts.Contains(hiltPart))
            {
                _selectedHiltParts.Add(hiltPart);
                _needsUpdate = true;
            }
        }

        public void RemoveHiltPart(HiltPartDef hiltPart)
        {
            if (_selectedHiltParts.Contains(hiltPart))
            {
                _selectedHiltParts.Remove(hiltPart);
                _needsUpdate = true;
            }
        }

        public HiltPartDef GetHiltPartByCategory(HiltPartCategory category)
        {
            return _selectedHiltParts.FirstOrDefault(part => part.category == category);
        }
    }
}