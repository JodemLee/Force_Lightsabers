using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static Verse.DamageWorker;

namespace Lightsaber
{
    [StaticConstructorOnStartup]
    public class LightSaberProjectile : Projectile
    {
        private float rotationAngle = 0f;
        public int ticksPerFrame = 8;
        private CompGlower compGlower;
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

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (DestinationIsValid() && DestinationIsMoving())
            {
                UpdateTrajectory();
            }
            else
            {
                return;
            }


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

            if (compLightsaberBlade != null)
            {

                var hiltGraphic = compLightsaberBlade.HiltManager.SelectedHilt?.graphicData?.Graphic;
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
                else
                {
                    Graphics.DrawMesh(
                        MeshPool.GridPlane(compLightsaberBlade.parent.Graphic.drawSize),
                        DrawPos,
                        rotation,
                        compLightsaberBlade.parent.Graphic.MatSingle,
                        0,
                        null,
                        0
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
            MaterialPropertyBlock propertyBlock = Comp_LightsaberBlade.propertyBlock;
            compLightsaberBlade.SetShaderProperties();

            // Draw the blade mesh
            Graphics.DrawMesh(
                MeshPool.GridPlane(compLightsaberBlade.BladeGraphic.drawSize),
                bladePos,
                rotation,
                bladeMat,
                0,
                null,
                0,
                propertyBlock
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
            try
            {
                if (this.Destroyed || !this.Spawned)
                {
                    Log.Warning("Trying to impact with destroyed or unspawned projectile");
                    return;
                }

                Map map = this.Map;
                if (map == null)
                {
                    Log.Warning("Projectile map is null during impact");
                    return;
                }


                Pawn effectiveLauncher = _originalLauncher ?? launcher as Pawn;

                base.Impact(hitThing, blockedByShield);

                if (!blockedByShield)
                {
                    HitImpact(hitThing, map);
                }

                if (effectiveLauncher != null &&
                    effectiveLauncher.Spawned &&
                    effectiveLauncher.equipment?.Primary != null)
                {
                    try
                    {
                        IntVec3 moteSpawnPos = hitThing?.Position ?? Position;

                        if (!moteSpawnPos.IsValid)
                        {
                            Log.Warning("Invalid mote spawn position");
                            return;
                        }

                        MoteLightSaberReturn mote = (MoteLightSaberReturn)ThingMaker.MakeThing(ThingDef.Named("Mote_LightSaberReturn"));
                        if (mote == null)
                        {
                            Log.Warning("Failed to create return mote");
                            return;
                        }

                        mote.exactPosition = moteSpawnPos.ToVector3Shifted();
                        mote.rotationRate = 0f;
                        mote.SetLauncher(effectiveLauncher, effectiveLauncher.equipment.Primary.Graphic);

                        Thing spawnedMote = GenSpawn.Spawn(mote, moteSpawnPos, map);
                        if (spawnedMote == null)
                        {
                            Log.Warning("Failed to spawn return mote");
                            return;
                        }


                        SoundDef soundDef = SoundDef.Named("Force_ForceThrow_Return");
                        if (soundDef != null)
                        {
                            soundDef.PlayOneShot(new TargetInfo(moteSpawnPos, map));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error creating return effect: {ex}");
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Critical error in LightSaberProjectile.Impact: {ex}\n{ex.StackTrace}");
            }
        }

        protected virtual void HitImpact(Thing hitThing, Map map)
        {
            try
            {
               
                if (map == null)
                {
                    return;
                }

                int damageAmount;
                float armorPenetration;
                DamageDef damageDef;

                if (_originalLauncher?.equipment?.Primary != null && !_originalLauncher.equipment.Primary.Destroyed)
                {
                    ThingWithComps weapon = _originalLauncher.equipment.Primary;

                    if (weapon.def?.tools != null && weapon.def.tools.Any())
                    {
                        Tool selectedTool = LightsaberCombatUtility.SelectWeightedTool(weapon.def.tools);
                        ToolCapacityDef mainCapacity = selectedTool?.capacities?.FirstOrDefault();

                        damageAmount = (int)(selectedTool?.power ?? def.projectile.GetDamageAmount(this, null));
                        armorPenetration = selectedTool?.armorPenetration ?? def.projectile.GetArmorPenetration(this, null);
                        damageDef = mainCapacity?.Maneuvers?.FirstOrDefault()?.verb?.meleeDamageDef ?? def.projectile.damageDef;

                    }
                    else
                    {
                        damageAmount = def.projectile.GetDamageAmount(this, null);
                        armorPenetration = def.projectile.GetArmorPenetration(this, null);
                        damageDef = def.projectile.damageDef;
                    }
                }
                else
                {
                    damageAmount = def.projectile.GetDamageAmount(this, null);
                    armorPenetration = def.projectile.GetArmorPenetration(this, null);
                    damageDef = def.projectile.damageDef;
                }

                var logEntry = new BattleLogEntry_RangedImpact(
                    launcher,
                    hitThing,
                    intendedTarget.Thing,
                    equipmentDef,
                    def,
                    targetCoverDef
                );
                Find.BattleLog?.Add(logEntry);

                if (hitThing != null)
                {

                    var dinfo = new DamageInfo(
                        damageDef ?? DamageDefOf.Blunt,
                        Mathf.Max(1, damageAmount),
                        armorPenetration,
                        ExactRotation.eulerAngles.y,
                        launcher
                    );


                    DamageResult result = hitThing.TakeDamage(dinfo);
                    if (logEntry != null) result.AssociateWithLog(logEntry);


                    if (hitThing is Pawn pawn &&
                        pawn.stances != null &&
                        !pawn.Downed &&
                        pawn.BodySize <= def.projectile.stoppingPower + 0.001f)
                    {
                        pawn.stances.stagger.StaggerFor(95);
                    }

                    if (def.projectile.extraDamages != null)
                    {
                        foreach (var extra in def.projectile.extraDamages)
                        {
                            if (Rand.Chance(extra.chance))
                            {
                                var extraDinfo = new DamageInfo(
                                    extra.def ?? damageDef,
                                    extra.amount,
                                    extra.AdjustedArmorPenetration(),
                                    ExactRotation.eulerAngles.y,
                                    launcher
                                );
                                var extraResult = hitThing.TakeDamage(extraDinfo);
                                extraResult.AssociateWithLog(logEntry);
                            }
                        }
                    }
                }
                else
                {
                    SoundDefOf.BulletImpact_Ground?.PlayOneShot(new TargetInfo(Position, map));

                    TerrainDef terrain = Position.GetTerrain(map);

                    if (terrain?.takeSplashes ?? false)
                    {
                        FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(3) * 1f, 4f);
                    }
                    else
                    {
                        FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Error in LightSaberProjectile.HitImpact: {ex}");
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

                Comp_LightsaberBlade compLightsaberBlade = equippedWeapon.TryGetComp<Comp_LightsaberBlade>();
                if (compLightsaberBlade != null && compLightsaberBlade.HiltManager?.SelectedHilt?.graphicData != null)
                {
                    // Use the selected hilt's graphic if available
                    selectedHiltGraphic = compLightsaberBlade.HiltManager.SelectedHilt.graphicData.Graphic;
                    this.instanceColor = selectedHiltGraphic.color;
                }
                else
                {
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