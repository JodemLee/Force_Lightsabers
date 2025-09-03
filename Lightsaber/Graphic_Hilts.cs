using UnityEngine;
using Verse;

namespace Lightsaber
{
    internal class Graphic_Hilts : Graphic
    {
        protected Material mat;
        private HiltManager hiltManager;
        private bool initialized = false;
        public static readonly string MaskSuffix = "_m";
        private string lastSelectedHiltGraphicPath;

        public override Material MatSingle => MatSingleFor(null);
        public override Material MatWest => MatSingle;
        public override Material MatSouth => MatSingle;
        public override Material MatEast => MatSingle;
        public override Material MatNorth => MatSingle;

        public override bool ShouldDrawRotated => data == null || data.drawRotated;

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            maskPath = req.maskPath;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;

            UpdateMaterial(req);
        }

        public void UpdateMaterial(GraphicRequest req)
        {
            // Get the actual colors from the hilt manager if available
            Color color1 = hiltManager?.HiltColorOne ?? req.color;
            Color color2 = hiltManager?.HiltColorTwo ?? req.colorTwo;

            MaterialRequest materialRequest = new MaterialRequest
            {
                mainTex = req.texture ?? ContentFinder<Texture2D>.Get(req.path),
                shader = req.shader,
                color = color1,  // Use HiltColorOne
                colorTwo = color2,  // Use HiltColorTwo
                renderQueue = req.renderQueue,
                shaderParameters = req.shaderParameters
            };

            if (req.shader.SupportsMaskTex())
            {
                materialRequest.maskTex = ContentFinder<Texture2D>.Get(
                    maskPath.NullOrEmpty() ? (path + MaskSuffix) : maskPath,
                    reportFailure: false);
            }

            mat = MaterialPool.MatFrom(materialRequest);
        }

        public void LinkToHiltManager(HiltManager manager)
        {
            if (manager == null) return;

            hiltManager = manager;

            if (hiltManager.SelectedHilt?.graphicData != null)
            {
                GraphicRequest request = new GraphicRequest(
                    typeof(Graphic_Hilts),
                    hiltManager.SelectedHilt.graphicData.texPath,
                    hiltManager.SelectedHilt.graphicData.Graphic.Shader,
                    hiltManager.SelectedHilt.graphicData.Graphic.drawSize,
                    hiltManager.HiltColorOne,  // Pass HiltColorOne
                    hiltManager.HiltColorTwo,   // Pass HiltColorTwo
                    hiltManager.SelectedHilt.graphicData.Graphic.data,
                    0,
                    null,
                    hiltManager.SelectedHilt.graphicData.maskPath
                );

                path = hiltManager.SelectedHilt.graphicData.texPath;
                mat = null;
                UpdateMaterial(request);
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {

            Color color1 = hiltManager?.HiltColorOne ?? newColor;
            Color color2 = hiltManager?.HiltColorTwo ?? newColorTwo;

            return GraphicDatabase.Get<Graphic_Hilts>(path, newShader, drawSize, color1, color2, data);
        }

        public override Material MatSingleFor(Thing thing)
        {
            if (thing != null && thing.TryGetComp<Comp_LightsaberBlade>() is Comp_LightsaberBlade comp)
            {
                LinkToHiltManager(comp.HiltManager);
            }
            return mat;
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            return MatSingleFor(thing);
        }

        public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
        {
            Texture2D mask = null;
            if (mat.HasProperty(ShaderPropertyIDs.MaskTex))
            {
                mask = (Texture2D)mat.GetTexture(ShaderPropertyIDs.MaskTex);
            }
            GlobalTextureAtlasManager.TryInsertStatic(groupKey, (Texture2D)mat.mainTexture, mask);
        }

        public override string ToString()
        {
            return string.Concat("Single(path=", path, ", color=", color, ", colorTwo=", colorTwo, ")");
        }
    }
}