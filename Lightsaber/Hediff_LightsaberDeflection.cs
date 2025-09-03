using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    public class Hediff_LightsaberDeflection : HediffWithComps
    {
        private int _lastInterceptTicks = -999999;
        private float _lastInterceptAngle;
        private bool _drawInterceptCone;
        private Dictionary<Thing, Gizmo_LightsaberStance> _weaponStances = new Dictionary<Thing, Gizmo_LightsaberStance>();

        public float EntropyGain { get; set; }
        public float DeflectionMultiplier { get; set; }

        public virtual void DeflectProjectile(Projectile projectile)
        {
            try
            {
                // Early exit if critical components are null
                if (projectile == null || pawn == null || pawn.Map == null)
                {
                    return;
                }

                // Verify projectile is still valid
                if (projectile.Destroyed || !projectile.Spawned)
                {
                    return;
                }

                // Verify pawn is capable of deflecting
                if (pawn.Dead || pawn.Downed || !pawn.Spawned)
                {
                    return;
                }

                // Trigger visual effects
                LightsaberCombatUtility.TriggerDeflectionEffect(projectile);

                // Record deflection information
                _lastInterceptAngle = projectile.ExactPosition.AngleToFlat(pawn.TrueCenter());
                _lastInterceptTicks = Find.TickManager.TicksGame;
                _drawInterceptCone = true;

                // Process each lightsaber weapon
                bool foundLightsaber = false;
                foreach (var weapon in pawn.equipment?.AllEquipmentListForReading ?? Enumerable.Empty<ThingWithComps>())
                {
                    var lightsaber = weapon?.TryGetComp<Comp_LightsaberBlade>();
                    if (lightsaber == null) continue;

                    foundLightsaber = true;

                    try
                    {
                        // Calculate deflection parameters
                        float angleDifference = Mathf.Abs(projectile.ExactPosition.AngleToFlat(pawn.TrueCenter()) - _lastInterceptAngle);
                        int deflectionTicks = (int)Mathf.Clamp(
                            LightsaberCombatUtility.CalculateDeflectionTicks(
                                projectile.def.projectile.speed,
                                angleDifference,
                                pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0
                            ),
                            LightsaberCombatUtility.MinDeflectionTicks,
                            LightsaberCombatUtility.MaxDeflectionTicks
                        );

                        lightsaber.AnimationDeflectionTicks = deflectionTicks;
                        lightsaber.targetScaleForCore1AndBlade1 = new Vector3(lightsaber.bladeLength, 1f, lightsaber.bladeLength);
                        lightsaber.targetScaleForCore2AndBlade2 = new Vector3(lightsaber.bladeLength, 1f, lightsaber.bladeLength);

                        Vector3 deflectionDirection = Quaternion.Euler(0f, _lastInterceptAngle, 0f) * Vector3.forward;
                        Vector3 deflectionLocation = pawn.TrueCenter() + deflectionDirection * LightsaberCombatUtility.DefaultDeflectionDistance;
                        LightsaberCombatUtility.CreateDeflectionFleck(lightsaber, deflectionLocation, _lastInterceptAngle, pawn.Map);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error processing lightsaber {weapon}: {ex}");
                    }
                }

                LightsaberCombatUtility.RedirectProjectile(projectile, pawn);
                AddEntropy(projectile);
                LightsaberCombatUtility.CreateScorchMark(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"Critical error in DeflectProjectile: {ex}\n{ex.StackTrace}");
            }
        }

        public void AddEntropy(Projectile projectile)
        {
            float entropyGain = LightsaberCombatUtility.CalculateEntropyGain(projectile);
            if (LightsaberCombatUtility.CanGainEntropy(pawn, entropyGain))
            {
                pawn.psychicEntropy.TryAddEntropy(entropyGain, overLimit: false);
            }
        }

        public virtual bool ShouldDeflectProjectile(Projectile projectile)
        {
            return LightsaberCombatUtility.ShouldDeflectProjectile(pawn, projectile);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var weapon in pawn.equipment.AllEquipmentListForReading)
            {
                if (pawn.Drafted || !pawn.Drafted && ForceLightsabers_ModSettings.lightsaberStanceUndrafted)
                {
                    if (weapon.TryGetComp<Comp_LightsaberStance>() != null)
                    {
                        if (!_weaponStances.TryGetValue(weapon, out var stance))
                        {
                            stance = new Gizmo_LightsaberStance(pawn, this, weapon);
                            _weaponStances[weapon] = stance;
                        }
                        yield return stance;
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _lastInterceptTicks, "lastInterceptTicks");
            Scribe_Values.Look(ref _lastInterceptAngle, "lastInterceptAngle");
            Scribe_Values.Look(ref _drawInterceptCone, "drawInterceptCone");

            List<Thing> weapons = _weaponStances.Keys.ToList();
            List<List<StanceData>> stanceData = _weaponStances.Values.Select(gizmo => gizmo.stanceDataList).ToList();

            Scribe_Collections.Look(ref weapons, "weapons", LookMode.Reference);
            Scribe_Collections.Look(ref stanceData, "stanceData", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (weapons != null && stanceData != null && weapons.Count == stanceData.Count)
                {
                    _weaponStances = new Dictionary<Thing, Gizmo_LightsaberStance>();
                    for (int i = 0; i < weapons.Count; i++)
                    {
                        var gizmo = new Gizmo_LightsaberStance(pawn, this, weapons[i]);
                        gizmo.stanceDataList = stanceData[i];
                        _weaponStances[weapons[i]] = gizmo;
                    }
                }
            }
        }
    }
}