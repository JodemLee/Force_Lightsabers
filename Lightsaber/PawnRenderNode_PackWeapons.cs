using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class PawnRenderNode_PackWeapons : PawnRenderNode
    {
        private bool useHeadMesh;

        public PawnRenderNode_PackWeapons(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }
        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            foreach (var g in base.GraphicsFor(pawn))
            {
                var graphic = GraphicDatabase.Get<Graphic_Multi>(
                    props.texPath + "_" + pawn?.story.bodyType.defName,
                    GetShader(),
                    Vector2.one,
                    GetColor());

                yield return graphic;
            }

        }

        private Color GetColor()
        {
            return (Color)props.color;
        }

        private Shader GetShader()
        {
            Shader shader = ShaderDatabase.Cutout;
            if (props.shaderTypeDef != null)
            {
                shader = props.shaderTypeDef.Shader;
            }

            return shader;
        }
    }
}