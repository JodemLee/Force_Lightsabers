using Lightsaber;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

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
                _needsUpdate = true;
            }
        }
    }

    public List<HiltPartDef> SelectedHiltParts
    {
        get => _selectedHiltParts;
        set
        {
            if (value == null)
            {
                _selectedHiltParts.Clear();
                _needsUpdate = true;
                return;
            }

            // Filter out null parts and check if the new list is different
            var filteredNewList = value.Where(part => part != null).ToList();
            var currentFilteredList = _selectedHiltParts.Where(part => part != null).ToList();

            if (!currentFilteredList.SequenceEqual(filteredNewList))
            {
                _selectedHiltParts = filteredNewList;
                _needsUpdate = true;
            }
        }
    }

    public Color _hiltColorOne = Color.white;
    public Color _hiltColorTwo = Color.gray;

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

        var hiltGraphic = SelectedHilt.graphicData.Graphic.GetColoredVersion(
            SelectedHilt.graphicData.Graphic.Shader,
            HiltColorOne,
            HiltColorTwo
        );

        ApplyMaterialPropertyBlock(hiltGraphic);

        return hiltGraphic;
    }

    private void ApplyMaterialPropertyBlock(Graphic hiltGraphic)
    {
        if (hiltGraphic?.MatSingle == null)
        {
            return;
        }

        HiltMaterialPropertyBlock.SetColor("_Color", HiltColorOne);
        HiltMaterialPropertyBlock.SetColor("_ColorTwo", HiltColorTwo);
    }

    private Graphic DefaultHiltGraphic()
    {
        return GraphicDatabase.Get<Graphic_Single>("DefaultHiltPath", ShaderDatabase.Cutout, Vector2.one, Color.white, Color.gray);
    }

    public void AddHiltPart(HiltPartDef hiltPart)
    {
        if (hiltPart == null) return;

        if (!_selectedHiltParts.Contains(hiltPart))
        {
            _selectedHiltParts.Add(hiltPart);
            _needsUpdate = true;
        }
    }

    public void RemoveHiltPart(HiltPartDef hiltPart)
    {
        if (hiltPart == null) return;

        if (_selectedHiltParts.Contains(hiltPart))
        {
            _selectedHiltParts.Remove(hiltPart);
            _needsUpdate = true;
        }
    }

    public HiltPartDef GetHiltPartByCategory(HiltPartCategoryDef category)
    {
        if (category == null) return null;
        return _selectedHiltParts.FirstOrDefault(part => part != null && part.category == category);
    }

    public void CleanNullHiltParts()
    {
        int removed = _selectedHiltParts.RemoveAll(part => part == null);
        if (removed > 0)
        {
            _needsUpdate = true;
        }
    }

    public bool HasHiltPartOfCategory(HiltPartCategoryDef category)
    {
        if (category == null) return false;
        return _selectedHiltParts.Any(part => part != null && part.category == category);
    }


    public IEnumerable<HiltPartDef> GetHiltPartsByCategory(HiltPartCategoryDef category)
    {
        if (category == null) return Enumerable.Empty<HiltPartDef>();
        return _selectedHiltParts.Where(part => part != null && part.category == category);
    }
}