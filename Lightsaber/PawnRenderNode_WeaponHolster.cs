using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace Lightsaber
{
    internal class PawnRenderNode_WeaponHolster : PawnRenderNode
    {
        private ThingWithComps primaryEquipment;
        private Comp_LightsaberBlade lightsaberComp;

        public PawnRenderNode_WeaponHolster(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            primaryEquipment = pawn.equipment?.Primary;
            if (primaryEquipment != null)
            {
                lightsaberComp = primaryEquipment.TryGetComp<Comp_LightsaberBlade>();
            }
        }

        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            if (lightsaberComp?.HiltManager?.SelectedHilt != null)
            {
                // Create a new graphic with both hilt colors
                var graphicData = lightsaberComp.HiltManager.SelectedHilt.graphicData;
                yield return graphicData.Graphic.GetColoredVersion(
                    graphicData.Graphic.Shader,
                    lightsaberComp.HiltManager.HiltColorOne,
                    lightsaberComp.HiltManager.HiltColorTwo
                );
            }
            else if (primaryEquipment?.def?.graphicData != null)
            {
                yield return primaryEquipment.Graphic;
            }
            else
            {
                foreach (var graphic in base.GraphicsFor(pawn))
                {
                    yield return graphic;
                }
            }
        }

        public override Color ColorFor(Pawn pawn)
        {
            // Return just the primary color (HiltColorOne) for compatibility
            return lightsaberComp?.HiltManager?.HiltColorOne ??
                   primaryEquipment?.DrawColor ??
                   base.ColorFor(pawn);
        }

        protected override string TexPathFor(Pawn pawn)
        {
            if (lightsaberComp?.HiltManager?.SelectedHilt != null)
            {
                return lightsaberComp.HiltManager.SelectedHilt.graphicData.texPath;
            }
            if (primaryEquipment?.def?.graphicData != null)
            {
                return primaryEquipment.def.graphicData.texPath;
            }
            return base.TexPathFor(pawn);
        }
    }

    public class PawnRenderNodeProperties_WeaponHolster : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_WeaponHolster()
        {
        }
    }

    public class PawnRenderNodeWorker_WeaponHolster : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (parms.pawn.equipment?.Primary == null || parms.pawn.Drafted || PawnRenderUtility.CarryWeaponOpenly(parms.pawn) || !ForceLightsabers_ModSettings.showLightsaberHolsters)
            {
                return false;
            }

            return base.CanDrawNow(node, parms);
        }

        protected override Graphic GetGraphic(PawnRenderNode node, PawnDrawParms parms)
        {
            ThingWithComps primary = parms.pawn.equipment?.Primary;
            if (primary != null && UnityData.IsInMainThread)
            {
                var lightsaberComp = primary.TryGetComp<Comp_LightsaberBlade>();
                if (lightsaberComp != null && lightsaberComp.HiltManager?.SelectedHilt != null)
                {
                    return lightsaberComp.HiltManager.SelectedHilt.graphicData.GraphicColoredFor(primary);
                }
                return primary.Graphic;
            }
            return base.GetGraphic(node, parms);
        }
    }
}