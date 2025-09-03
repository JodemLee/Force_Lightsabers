using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace Lightsaber
{
    public struct FleckThrownSpark : IFleck
    {
        public FleckStatic baseData;

        public float airTimeLeft;
        public Vector3 velocity;
        public float rotationRate;
        public FleckAttachLink link;

        private Vector3 attacheeLastPosition;
        public float orbitSpeed;
        public float archHeight;
        public float? archDuration;
        public float orbitSnapStrength;

        private Vector3 position;

        public const float MinSpeed = 0.05f;
        public const float MinOrbitSpeed = 0.3f;

        public float orbitDistance;
        private float orbitAccum;
        public float skidSpeedMultiplierPerTick = 0.85f;  // Faster decay for sparks

        public FleckThrownSpark()
        {
        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public bool Flying => airTimeLeft > 0f;
        public bool Skidding => !Flying && Speed > 0.01f;
        public bool Orbiting => orbitSpeed != 0f;
        public bool Arching => archHeight != 0f;

        public Vector3 Velocity
        {
            get => velocity;
            set => velocity = value;
        }

        public float MoveAngle
        {
            get => velocity.AngleFlat();
            set => SetVelocity(value, Speed);
        }

        public float Speed
        {
            get => velocity.MagnitudeHorizontal();
            set
            {
                if (value <= 0f) velocity = Vector3.zero;
                else velocity = velocity.normalized * value;
            }
        }

        public void Setup(FleckCreationData creationData)
        {
            baseData = default(FleckStatic);
            baseData.Setup(creationData);

            airTimeLeft = creationData.airTimeLeft.GetValueOrDefault(1.2f);  // Short lifespan for sparks
            rotationRate = creationData.rotationRate;
            SetVelocity(creationData.velocityAngle, creationData.velocitySpeed);

            if (creationData.velocity.HasValue)
            {
                velocity += creationData.velocity.Value;
            }

            orbitSpeed = creationData.orbitSpeed;
            orbitSnapStrength = creationData.orbitSnapStrength;

            if (Orbiting)
            {
                orbitDistance = (link.Target.CenterVector3 - baseData.position.worldPosition).MagnitudeHorizontal();
                orbitAccum = Rand.Range(0f, (float)Math.PI * 2f);

                if (Mathf.Abs(orbitSpeed) < MinOrbitSpeed)
                {
                    orbitSpeed = MinOrbitSpeed * GenMath.Sign(orbitSpeed);
                }
            }

            archHeight = creationData.def.archHeight.RandomInRange;
            archDuration = creationData.def.archDuration.RandomInRange;
        }

        public bool TimeInterval(float deltaTime, Map map)
        {
            if (baseData.TimeInterval(deltaTime, map)) return true;

            if (!Flying && !Skidding) return false;

            if (baseData.def.rotateTowardsMoveDirection && velocity != Vector3.zero)
            {
                baseData.exactRotation = velocity.AngleFlat() + baseData.def.rotateTowardsMoveDirectionExtraAngle;
            }
            else
            {
                baseData.exactRotation += rotationRate * deltaTime;
            }

            velocity += baseData.def.acceleration * deltaTime;  // Apply acceleration (e.g., gravity)

            if (baseData.def.speedPerTime != FloatRange.Zero)
            {
                Speed = Mathf.Max(Speed + baseData.def.speedPerTime.RandomInRange * deltaTime, 0f);
            }

            if (airTimeLeft > 0f)
            {
                airTimeLeft -= deltaTime;
                if (airTimeLeft <= 0f) airTimeLeft = 0f;
            }

            if (Skidding)
            {
                Speed *= skidSpeedMultiplierPerTick;
                rotationRate *= skidSpeedMultiplierPerTick;
                if (Speed < MinSpeed) Speed = 0f;
            }

            FleckDrawPosition nextPosition = NextPosition(deltaTime);
            if (Orbiting)
            {
                ApplyOrbit(deltaTime, ref nextPosition.worldPosition);
            }

            IntVec3 intPos = new IntVec3(nextPosition.worldPosition);
            if (intPos != new IntVec3(baseData.position.worldPosition) && intPos.InBounds(map))
            {
                if (baseData.def.collide && intPos.Filled(map))
                {
                    WallHit();
                    return false;
                }
            }

            baseData.position = nextPosition;
            return false;
        }

        private FleckDrawPosition NextPosition(float deltaTime)
        {
            Vector3 worldPos = baseData.position.worldPosition + velocity * deltaTime;
            float height = 0f;

            if (Arching)
            {
                float progress = Mathf.Clamp01(baseData.ageSecs / archDuration.Value);
                height = archHeight * GenMath.InverseParabola(progress);
            }

            Vector3 anchorOffset = (new Vector3(0.5f, 0f, 0.5f) - baseData.def.scalingAnchor).ScaledBy(baseData.AddedScale);
            return new FleckDrawPosition(worldPos, height, anchorOffset, baseData.def.unattachedDrawOffset, Vector3.zero);
        }

        private void ApplyOrbit(float deltaTime, ref Vector3 nextPosition)
        {
            orbitAccum += deltaTime * orbitSpeed;
            Vector3 offset = new Vector3(Mathf.Cos(orbitAccum), 0f, Mathf.Sin(orbitAccum)) * orbitDistance;
            Vector3 targetPos = link.Target.CenterVector3 + offset;

            MoveAngle = (targetPos - baseData.position.worldPosition).AngleFlat();
            nextPosition = Vector3.Lerp(nextPosition, targetPos, orbitSnapStrength);
        }

        public void SetVelocity(float angle, float speed)
        {
            velocity = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * speed;
        }

        public void Draw(DrawBatch batch)
        {
            baseData.Draw(batch);
        }

        private void WallHit()
        {
            airTimeLeft = 0f;
            Speed = 0f;
            rotationRate = 0f;
        }
    }

    public class FleckSystemThrownSparks : FleckSystemBase<FleckThrownSpark>
    {
        public FleckSystemThrownSparks(FleckManager parent)
        : base(parent)
        {
        }
    }
}