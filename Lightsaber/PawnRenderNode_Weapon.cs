using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class PawnRenderNodeProperties_Weapons : PawnRenderNodeProperties
    {
        public bool rotateFacing = true;
        public FloatRange scaleRange = FloatRange.One;
        public FloatRange rotationRange = FloatRange.Zero;
        public FloatRange offsetRangeX = FloatRange.Zero;
        public FloatRange offsetRangeZ = FloatRange.Zero;
        public IntRange animationDurationTicks = new IntRange(60, 60);
        public IntRange nextAnimationDelayTicks = new IntRange(60, 60);

        // New properties for circular motion
        public bool enableOrbit = false;
        public FloatRange orbitRadiusRange = new FloatRange(0.5f, 1f);
        public FloatRange orbitAngleChangeRange = new FloatRange(90f, 360f);
        public bool clockwiseOrbit = true;

        public PawnRenderNodeProperties_Weapons()
        {
            nodeClass = typeof(PawnRenderNode_AnimatedWeapon);
            workerClass = typeof(PawnRenderNodeWorker_AnimatedWeapon);
        }
    }

    public class PawnRenderNode_AnimatedWeapon : PawnRenderNode
    {
        public class AnimationData
        {
            public float rotationStart;
            public float rotationTarget;
            public float scaleStart;
            public float scaleTarget;
            public Vector3 offsetStart;
            public Vector3 offsetTarget;
            public int tickStart;
            public int nextAnimationTime;
            public float duration;

            // New orbit data
            public float orbitAngleStart;
            public float orbitAngleTarget;
            public float orbitRadiusStart;
            public float orbitRadiusTarget;

            public AnimationData()
            {
                duration = 1f;
                scaleStart = (scaleTarget = 1f);
                orbitRadiusStart = (orbitRadiusTarget = 1f);
            }
        }

        protected AnimationData animationData;

        public PawnRenderNode_AnimatedWeapon(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return new GraphicMeshSet(MeshPool.GridPlane(props.overrideMeshSize ?? props.drawSize));
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (pawn.equipment?.Primary == null)
                return base.GraphicFor(pawn);

            var lightsaberComp = pawn.equipment.Primary.TryGetComp<Comp_LightsaberBlade>();
            if (lightsaberComp != null)
            {
                // Store references but return a blank/transparent graphic
                this.HiltGraphic = lightsaberComp.HiltManager.SelectedHilt?.graphicData?.Graphic;
                this.BladeMaterial = lightsaberComp.BladeGraphic?.MatSingle;

                return GraphicDatabase.Get<Graphic_Single>(
                    "base",
                    ShaderDatabase.Transparent,
                    Vector2.one,
                    Color.clear
                );
            }

            return pawn.equipment.Primary.Graphic ?? base.GraphicFor(pawn);
        }


        public Graphic HiltGraphic { get; private set; }
        public Material BladeMaterial { get; private set; }


        public bool CheckAndUpdateAnimation(PawnDrawParms parms, out AnimationData data, out float progress)
        {
            if (parms.pawn.Dead || !(props is PawnRenderNodeProperties_Weapons weaponProps) || parms.Portrait || parms.Cache)
            {
                progress = 0f;
                data = null;
                return false;
            }

            animationData ??= new AnimationData();

            if (Find.TickManager.TicksGame >= animationData.nextAnimationTime)
            {
                animationData.tickStart = Find.TickManager.TicksGame;
                animationData.duration = GetAnimationDurationTicks();
                animationData.nextAnimationTime = GetNextAnimationTime();

                // Standard animation parameters
                animationData.rotationStart = animationData.rotationTarget;
                animationData.rotationTarget = weaponProps.rotationRange.RandomInRange;
                animationData.scaleStart = animationData.scaleTarget;
                animationData.scaleTarget = weaponProps.scaleRange.RandomInRange;
                animationData.offsetStart = animationData.offsetTarget;
                animationData.offsetTarget = new Vector3(
                    weaponProps.offsetRangeX.RandomInRange,
                    0f,
                    weaponProps.offsetRangeZ.RandomInRange);

                // Orbit parameters if enabled
                if (weaponProps.enableOrbit)
                {
                    animationData.orbitAngleStart = animationData.orbitAngleTarget;
                    float angleChange = weaponProps.orbitAngleChangeRange.RandomInRange *
                                      (weaponProps.clockwiseOrbit ? 1 : -1);
                    animationData.orbitAngleTarget = animationData.orbitAngleStart + angleChange;

                    animationData.orbitRadiusStart = animationData.orbitRadiusTarget;
                    animationData.orbitRadiusTarget = weaponProps.orbitRadiusRange.RandomInRange;
                }
            }

            progress = (float)(Find.TickManager.TicksGame - animationData.tickStart) / Mathf.Max(animationData.duration, 0.0001f);
            data = animationData;
            return true;
        }

        protected virtual int GetNextAnimationTime()
        {
            return props is PawnRenderNodeProperties_Weapons weaponProps
                ? animationData.tickStart + (int)animationData.duration + weaponProps.nextAnimationDelayTicks.RandomInRange
                : 0;
        }

        protected virtual int GetAnimationDurationTicks()
        {
            return props is PawnRenderNodeProperties_Weapons weaponProps
                ? weaponProps.animationDurationTicks.RandomInRange
                : 0;
        }
    }

    public class PawnRenderNodeWorker_AnimatedWeapon : PawnRenderNodeWorker
    {

        public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            if (!(node is PawnRenderNode_AnimatedWeapon weaponNode) ||
                parms.pawn?.equipment?.Primary == null)
            {
                base.PostDraw(node, parms, mesh, matrix);
                return;
            }

            var lightsaberComp = parms.pawn.equipment.Primary.TryGetComp<Comp_LightsaberBlade>();
            if (lightsaberComp == null)
            {
                base.PostDraw(node, parms, mesh, matrix);
                return;
            }
            MaterialPropertyBlock propertyBlock = Comp_LightsaberBlade.propertyBlock;
            
            // Draw blade using exact same matrix
            if (weaponNode.BladeMaterial != null)
            {
                // Extract position from matrix (column 3)
                Vector3 position = new Vector3(matrix.m03, matrix.m13, matrix.m23);

                // Apply Y offset
                position.y -= 0.001f;

                // Create new matrix with adjusted position
                Matrix4x4 bladeMatrix = Matrix4x4.TRS(
                    position,
                    matrix.rotation,
                    matrix.lossyScale
                );
                lightsaberComp.SetShaderProperties();
                Graphics.DrawMesh(
                    MeshPool.plane10,
                    bladeMatrix,
                    weaponNode.BladeMaterial,
                    0,
                    null,
                    0,
                    propertyBlock
                );
            }

            // Draw hilt
            if (weaponNode.HiltGraphic != null)
            {
                Graphics.DrawMesh(
                    mesh,
                    matrix,
                    weaponNode.HiltGraphic.MatSingle,
                    0,
                    null,
                    0,
                    lightsaberComp.HiltManager?.HiltMaterialPropertyBlock
                );
            }
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            if (node is PawnRenderNode_AnimatedWeapon animatedNode &&
                animatedNode.CheckAndUpdateAnimation(parms, out var data, out var progress))
            {
                // Apply standard offset
                result += Vector3.Lerp(data.offsetStart, data.offsetTarget, progress);

                // Apply circular motion if enabled
                if (node.Props is PawnRenderNodeProperties_Weapons { enableOrbit: true })
                {
                    float currentAngle = Mathf.Lerp(data.orbitAngleStart, data.orbitAngleTarget, progress);
                    float currentRadius = Mathf.Lerp(data.orbitRadiusStart, data.orbitRadiusTarget, progress);

                    result.x += Mathf.Cos(currentAngle * Mathf.Deg2Rad) * currentRadius;
                    result.z += Mathf.Sin(currentAngle * Mathf.Deg2Rad) * currentRadius;
                }
            }
            return result;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion quaternion = base.RotationFor(node, parms);
            if (node is not PawnRenderNode_AnimatedWeapon animatedNode)
                return quaternion;

            float angle = 0f;
            if (node.Props is PawnRenderNodeProperties_Weapons { rotateFacing: not false })
            {
                angle += parms.facing.AsAngle;
            }
            if (animatedNode.CheckAndUpdateAnimation(parms, out var data, out var progress))
            {
                angle += Mathf.Lerp(data.rotationStart, data.rotationTarget, progress);
            }
            return quaternion * angle.ToQuat();
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 result = base.ScaleFor(node, parms);
            if (node is PawnRenderNode_AnimatedWeapon animatedNode &&
                animatedNode.CheckAndUpdateAnimation(parms, out var data, out var progress))
            {
                result *= Mathf.Lerp(data.scaleStart, data.scaleTarget, progress);
                result.y = 1f;
            }
            return result;
        }
    }
}