using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public class LightSaberProjectile : Projectile
    {
        private float rotationAngle = 0f;
        public int ticksPerFrame = 8;
        public int TicksToImpact => ticksToImpact;
        Projectile projectile;
        private Pawn _originalLauncher;
        public Pawn OriginalLauncher => _originalLauncher;

        public float spinRate { get; set; }

        private static readonly Material shadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            if (_originalLauncher == null)
            {
                _originalLauncher = launcher as Pawn; // Store original launcher
            }

            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire);
        }
        private float ArcHeightFactor
        {
            get
            {
                float num = def.projectile.arcHeightFactor;
                float num2 = (destination - origin).MagnitudeHorizontalSquared();
                if (num * num > num2 * 0.2f * 0.2f)
                {
                    num = Mathf.Sqrt(num2) * 0.2f;
                }

                return num;
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (DestinationIsValid() && DestinationIsMoving())
            {
                UpdateTrajectory();
            }
            else
            {
                return;
            }
        }

        private bool DestinationIsValid()
        {
            return usedTarget.HasThing && usedTarget.Thing.Map == Map;
        }

        private bool DestinationIsMoving()
        {
            if (usedTarget == null || !usedTarget.HasThing) return false;

            var thing = usedTarget.Thing;
            if (thing is not Pawn pawn) return false;

            return pawn.pather?.MovingNow ?? false;
        }

        private void DestroyProjectile()
        {
            Destroy();
        }

        private void UpdateTrajectory()
        {
            if (!DestinationIsValid() || usedTarget.Thing == null)
            {
                return;
            }

            Vector3 targetPosition = usedTarget.CenterVector3;
            Vector3 currentPosition = ExactPosition;
            Vector3 directionToTarget = (targetPosition - currentPosition).normalized;
            float adjustedSpeed = def.projectile.speed * 0.5f;
            origin += directionToTarget * adjustedSpeed * Time.deltaTime;
            destination = targetPosition;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float arcHeight = ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFractionArc);
            if (def.projectile.shadowSize > 0f)
            {
                DrawShadow(DrawPos, arcHeight);
            }

            Vector3 direction = (destination - origin).normalized;
            Quaternion rotation = ExactRotation;
            Graphic graphicToDraw = def.graphic;
            Comp_LightsaberBlade compLightsaberBlade = null;
            float spinRate = ForceLightsabers_ModSettings.spinRate;
            float rotationSpeed = 100f * spinRate;
            float rotationAngle = Time.time * rotationSpeed;
            rotation *= Quaternion.Euler(0f, rotationAngle, 0f);

            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
            {
                spinRate = 0f;
                rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            if (_originalLauncher is Pawn pawn && pawn.equipment?.Primary != null)
            {
                ThingWithComps equippedWeapon = pawn.equipment.Primary;
                graphicToDraw = equippedWeapon.Graphic;
                compLightsaberBlade = equippedWeapon.TryGetComp<Comp_LightsaberBlade>();
            }

            if (graphicToDraw == null)
            {
                return;
            }

            // Draw the default projectile/hilt graphic
            if (def.projectile.useGraphicClass)
            {
                graphicToDraw.Draw(DrawPos, Rotation, this, rotationSpeed);
            }
            else if (compLightsaberBlade == null)
            {
                Graphics.DrawMesh(MeshPool.GridPlane(graphicToDraw.drawSize), DrawPos, rotation, graphicToDraw.MatSingle, 0);
            }

            // Handle lightsaber-specific rendering (blade + hilt if available)
            if (compLightsaberBlade != null)
            {

                Graphic hiltGraphic = compLightsaberBlade.HiltManager.SelectedHilt?.graphicData?.Graphic;
                if (hiltGraphic != null)
                {
                    Graphics.DrawMesh(
                        MeshPool.GridPlane(hiltGraphic.drawSize),
                        DrawPos,
                        rotation,
                        hiltGraphic.MatSingle,
                        0,
                        null,
                        0,
                        compLightsaberBlade.HiltManager.HiltMaterialPropertyBlock
                    );
                }

                DrawLightsaberGraphics(compLightsaberBlade, DrawPos, rotation, rotationSpeed);
                compLightsaberBlade.IsThrowingWeapon = true;
            }

            Comps_PostDraw();
        }

        private void DrawLightsaberGraphics(Comp_LightsaberBlade compLightsaberBlade, Vector3 drawLoc, Quaternion rotation, float rotationspeed, float drawSize = 1.5f)
        {
            if (compLightsaberBlade?.BladeGraphic?.MatSingle == null)
            {
                return;
            }
            float bladeYOffset = -0.002f;

            // Create the blade position with the offset
            Vector3 bladePos = drawLoc + new Vector3(0f, bladeYOffset, 0f);

            // Get the blade graphic's material
            Material bladeMat = compLightsaberBlade.BladeGraphic.MatSingle;

            // Draw the blade mesh
            Graphics.DrawMesh(
                MeshPool.GridPlane(compLightsaberBlade.BladeGraphic.drawSize),
                bladePos,
                rotation,
                bladeMat,
                0,
                null,
                0,
                compLightsaberBlade.propertyBlock
            );

        }

        private void DrawShadow(Vector3 drawLoc, float height)
        {
            if (shadowMaterial != null)
            {
                float num = def.projectile.shadowSize * Mathf.Lerp(1f, 0.6f, height);
                Vector3 s = new Vector3(num, 1f, num);
                Vector3 vector = new Vector3(0f, -0.01f, 0f);
                Matrix4x4 matrix = Matrix4x4.TRS(drawLoc + vector, Quaternion.identity, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (launcher == null || _originalLauncher == null)
            {
                return;
            }

            Map map = this.Map;
            if (map == null)
            {
                return;
            }

            base.Impact(hitThing, blockedByShield);

            Impact(hitThing, false);

            if (launcher is Pawn cachedLauncher && cachedLauncher.equipment?.Primary != null)
            {
                IntVec3 moteSpawnPos = hitThing != null ? hitThing.Position : Position;

                MoteLightSaberReturn mote = (MoteLightSaberReturn)ThingMaker.MakeThing(ThingDef.Named("Mote_LightSaberReturn"));
                if (mote == null)
                {
                    return;
                }

                mote.exactPosition = moteSpawnPos.ToVector3Shifted();
                mote.rotationRate = 0f;
                mote.SetLauncher(_originalLauncher, _originalLauncher.equipment.Primary.Graphic);

                GenSpawn.Spawn(mote, moteSpawnPos, map);

                SoundDef soundDef = SoundDef.Named("Force_ForceThrow_Return");
                if (soundDef != null)
                {
                    soundDef.PlayOneShot(new TargetInfo(moteSpawnPos, map));
                }
            }
        }
    }

    public class MoteLightSaberReturn : MoteThrown
    {
        private Pawn launcher;
        private Pawn originalLauncher;
        private Graphic originalWeaponGraphic;
        private Graphic selectedHiltGraphic; // Store the hilt graphic separately
        public int ticksPerFrame = 8;

        public void SetLauncher(Pawn pawn, Graphic weaponGraphic)
        {
            launcher = pawn;
            if (launcher != null && launcher.equipment?.Primary != null)
            {
                ThingWithComps equippedWeapon = launcher.equipment.Primary;
                originalLauncher = launcher;

                // Check if the weapon has a lightsaber blade component
                Comp_LightsaberBlade compLightsaberBlade = equippedWeapon.TryGetComp<Comp_LightsaberBlade>();
                if (compLightsaberBlade != null && compLightsaberBlade.HiltManager?.SelectedHilt?.graphicData != null)
                {
                    // Use the selected hilt's graphic if available
                    selectedHiltGraphic = compLightsaberBlade.HiltManager.SelectedHilt.graphicData.Graphic;
                    this.instanceColor = selectedHiltGraphic.color;
                }
                else
                {
                    // Fall back to the weapon's default graphic
                    originalWeaponGraphic = equippedWeapon.Graphic;
                    this.instanceColor = equippedWeapon.Graphic?.color ?? this.instanceColor;
                }
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (selectedHiltGraphic != null)
                {
                    return selectedHiltGraphic;
                }
                else if (originalWeaponGraphic != null)
                {
                    return originalWeaponGraphic;
                }
                else
                {
                    return base.Graphic;
                }
            }
        }

        protected override void TimeInterval(float deltaTime)
        {
            base.TimeInterval(deltaTime);

            if (originalLauncher == null || originalLauncher.Destroyed)
            {
                return;
            }

            if (Graphic == null) // Now checks the correct graphic via the property
            {
                Log.Warning("Graphic is null in MoteLightSaberReturn.TimeInterval");
                return;
            }

            Vector3 directionToLauncher = (originalLauncher.Position.ToVector3Shifted() - this.exactPosition).normalized;

            float verticalVelocity = ticksPerFrame * 1.1f;
            float horizontalVelocity = verticalVelocity * directionToLauncher.AngleFlat();

            SetVelocity(horizontalVelocity, verticalVelocity);
            base.exactPosition += base.velocity * deltaTime;

            if (CheckCollisionWithLauncher())
            {
                this.Destroy(DestroyMode.Vanish);
                Comp_LightsaberBlade compLightsaberBlade = originalLauncher.equipment.Primary.TryGetComp<Comp_LightsaberBlade>();
                if (compLightsaberBlade != null)
                {
                    compLightsaberBlade.IsThrowingWeapon = false;
                    SoundDef soundDef = compLightsaberBlade.selectedSoundEffect ?? null;
                    SoundStarter.PlayOneShot(soundDef, this);
                }
            }
        }

        private bool CheckCollisionWithLauncher()
        {
            if (originalLauncher == null)
                return false;

            Vector2 moteSize = this.Graphic.drawSize; // Now uses the correct graphic

            Rect moteRect = new Rect(this.exactPosition.x, this.exactPosition.z, moteSize.x, moteSize.y);
            Rect launcherRect = new Rect(originalLauncher.Position.x, originalLauncher.Position.z, originalLauncher.def.size.x, originalLauncher.def.size.z);

            return moteRect.Overlaps(launcherRect);
        }
    }
}