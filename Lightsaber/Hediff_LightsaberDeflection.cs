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
            if (projectile == null || pawn == null)
            {
                Log.Warning("Projectile or pawn is null in DeflectProjectile.");
                return;
            }

            try
            {
                LightsaberCombatUtility.TriggerDeflectionEffect(projectile);

                _lastInterceptAngle = projectile.ExactPosition.AngleToFlat(pawn.TrueCenter());
                _lastInterceptTicks = Find.TickManager.TicksGame;
                _drawInterceptCone = true;

                foreach (var weapon in pawn.equipment.AllEquipmentListForReading)
                {
                    if (weapon.TryGetComp<Comp_LightsaberBlade>() is Comp_LightsaberBlade lightsaber)
                    {
                        float angleDifference = Mathf.Abs(projectile.ExactPosition.AngleToFlat(pawn.TrueCenter()) - _lastInterceptAngle);
                        lightsaber.AnimationDeflectionTicks = (int)Mathf.Clamp(
                            LightsaberCombatUtility.CalculateDeflectionTicks(
                                projectile.def.projectile.speed,
                                angleDifference,
                                pawn.skills.GetSkill(SkillDefOf.Melee).Level
                            ),
                            LightsaberCombatUtility.MinDeflectionTicks,
                            LightsaberCombatUtility.MaxDeflectionTicks
                        );

                        lightsaber.targetScaleForCore1AndBlade1 = new Vector3(lightsaber.bladeLength, 1f, lightsaber.bladeLength);
                        lightsaber.targetScaleForCore2AndBlade2 = new Vector3(lightsaber.bladeLength, 1f, lightsaber.bladeLength);

                        Vector3 deflectionDirection = Quaternion.Euler(0f, _lastInterceptAngle, 0f) * Vector3.forward;
                        Vector3 deflectionLocation = pawn.TrueCenter() + deflectionDirection * LightsaberCombatUtility.DefaultDeflectionDistance;
                        LightsaberCombatUtility.CreateDeflectionFleck(lightsaber, deflectionLocation, _lastInterceptAngle, pawn.Map);
                    }
                }

                LightsaberCombatUtility.RedirectProjectile(projectile, pawn);
                AddEntropy(projectile);
                LightsaberCombatUtility.CreateScorchMark(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in DeflectProjectile: {ex.Message}");
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
                if (weapon.TryGetComp<Comp_LightsaberBlade>() != null)
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