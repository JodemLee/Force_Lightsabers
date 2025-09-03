using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class PawnRenderNodeWorker_Orbiting : PawnRenderNodeWorker
    {
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            if (node is PawnRenderNode_Orbiting pawnRenderNode_Orbiting &&
                pawnRenderNode_Orbiting.CheckAndDoOrbit(parms, out var dat, out var progress))
            {
                // Calculate circular motion
                float angle = Mathf.Lerp(dat.angleStart, dat.angleTarget, progress);
                float radius = Mathf.Lerp(dat.radiusStart, dat.radiusTarget, progress);

                // Convert polar coordinates to Cartesian
                result.x += Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                result.z += Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            }
            return result;
        }

        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion quaternion = base.RotationFor(node, parms);
            if (!(node is PawnRenderNode_Orbiting pawnRenderNode_Orbiting))
            {
                return quaternion;
            }

            float num = 0f;
            if (node.Props is PawnRenderNodeProperties_Orbiting { rotateFacing: not false })
            {
                num += parms.facing.AsAngle;
            }

            if (pawnRenderNode_Orbiting.CheckAndDoOrbit(parms, out var dat, out var progress))
            {
                // Add rotation if desired
                num += Mathf.Lerp(dat.rotationStart, dat.rotationTarget, progress);
            }

            return quaternion * num.ToQuat();
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 result = base.ScaleFor(node, parms);
            if (node is PawnRenderNode_Orbiting pawnRenderNode_Orbiting &&
                pawnRenderNode_Orbiting.CheckAndDoOrbit(parms, out var dat, out var progress))
            {
                result *= Mathf.Lerp(dat.scaleStart, dat.scaleTarget, progress);
                result.y = 1f;
            }
            return result;
        }
    }

    public class PawnRenderNode_Orbiting : PawnRenderNode
    {
        public class OrbitData
        {
            public float angleStart;
            public float angleTarget;
            public float radiusStart;
            public float radiusTarget;
            public float rotationStart;
            public float rotationTarget;
            public float scaleStart;
            public float scaleTarget;
            public int tickStart;
            public int nextOrbit;
            public float duration;

            public OrbitData()
            {
                duration = 1f;
                scaleStart = scaleTarget = 1f;
                radiusStart = radiusTarget = 1f;
            }
        }

        protected OrbitData orbitData;

        public PawnRenderNode_Orbiting(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return new GraphicMeshSet(MeshPool.GridPlane(props.overrideMeshSize ?? props.drawSize));
        }

        public bool CheckAndDoOrbit(PawnDrawParms parms, out OrbitData dat, out float progress)
        {
            if (parms.pawn.Dead || !(props is PawnRenderNodeProperties_Orbiting orbitProps) || parms.Portrait || parms.Cache)
            {
                progress = 0f;
                dat = null;
                return false;
            }

            if (orbitData == null)
            {
                orbitData = new OrbitData();
            }

            if (Find.TickManager.TicksGame >= orbitData.nextOrbit)
            {
                orbitData.tickStart = Find.TickManager.TicksGame;
                orbitData.duration = GetNextOrbitDurationTicks();
                orbitData.nextOrbit = GetNextOrbitTick();

                // Set new orbit parameters
                orbitData.angleStart = orbitData.angleTarget;
                orbitData.angleTarget = orbitData.angleStart + orbitProps.angleChangeRange.RandomInRange;

                orbitData.radiusStart = orbitData.radiusTarget;
                orbitData.radiusTarget = orbitProps.radiusRange.RandomInRange;

                orbitData.rotationStart = orbitData.rotationTarget;
                orbitData.rotationTarget = orbitProps.rotationRange.RandomInRange;

                orbitData.scaleStart = orbitData.scaleTarget;
                orbitData.scaleTarget = orbitProps.scaleRange.RandomInRange;
            }

            progress = (float)(Find.TickManager.TicksGame - orbitData.tickStart) / Mathf.Max(orbitData.duration, 0.0001f);
            dat = orbitData;
            return true;
        }

        protected virtual int GetNextOrbitTick()
        {
            if (props is PawnRenderNodeProperties_Orbiting orbitProps)
            {
                return orbitData.tickStart + (int)orbitData.duration + orbitProps.nextOrbitTicksRange.RandomInRange;
            }
            return 0;
        }

        protected virtual int GetNextOrbitDurationTicks()
        {
            if (props is PawnRenderNodeProperties_Orbiting orbitProps)
            {
                return orbitProps.durationTicksRange.RandomInRange;
            }
            return 0;
        }
    }

    public class PawnRenderNodeProperties_Orbiting : PawnRenderNodeProperties
    {
        public bool rotateFacing = true;
        public FloatRange scaleRange = FloatRange.One;
        public FloatRange rotationRange = FloatRange.Zero;

        // Orbital parameters
        public FloatRange angleChangeRange = new FloatRange(90f, 360f); // Degrees to move per orbit
        public FloatRange radiusRange = new FloatRange(0.5f, 2f); // Distance from center

        public IntRange durationTicksRange = new IntRange(60, 60);
        public IntRange nextOrbitTicksRange = new IntRange(60, 60);

        public PawnRenderNodeProperties_Orbiting()
        {
            nodeClass = typeof(PawnRenderNode_Orbiting);
            workerClass = typeof(PawnRenderNodeWorker_Orbiting);
        }
    }
}
