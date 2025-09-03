using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Lightsaber
{
    public class HediffComp_LaunchProjectile : HediffComp
    {
        public int ticksUntilNextUse;

        public HediffCompProperties_LaunchProjectile Props =>
            (HediffCompProperties_LaunchProjectile)props;

        public bool CanLaunchProjectile =>
            ticksUntilNextUse <= 0 &&
            Pawn.Drafted &&
            Pawn.Spawned;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksUntilNextUse, "ticksUntilNextUse", 0);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (ticksUntilNextUse > 0)
            {
                ticksUntilNextUse--;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (!Pawn.Drafted) yield break;

            yield return new Command_Action
            {
                defaultLabel = Props.labelKey.TranslateSimple(),
                defaultDesc = Props.descriptionKey.TranslateSimple(),
                icon = Props.icon,
                action = () => Find.Targeter.BeginTargeting(
                    new TargetingParameters
                    {
                        canTargetLocations = true,
                        canTargetPawns = true,
                        canTargetBuildings = true,
                        mapObjectTargetsMustBeAutoAttackable = true,
                        validator = (target) =>
                            target.Cell.DistanceTo(Pawn.Position) <= Props.range
                    },
                    delegate (LocalTargetInfo target)
                    {
                        LaunchProjectileAt(target.Cell, target);
                    },
                    highlightAction: (LocalTargetInfo target) =>
                    {
                        GenDraw.DrawRadiusRing(Pawn.Position, Props.range);
                    },
                    targetValidator: (LocalTargetInfo target) =>
                    {
                        GenDraw.DrawTargetHighlight(target);
                        return target.Cell.DistanceTo(Pawn.Position) <= Props.range;
                    }
                ),
                Disabled = !CanLaunchProjectile,
                disabledReason = ticksUntilNextUse > 0
                    ? "AbilityOnCooldown".Translate(ticksUntilNextUse.ToStringTicksToPeriod())
                    : "MustBeDrafted".Translate()
            };
        }

        public void LaunchProjectileAt(IntVec3 targetCell, LocalTargetInfo targetInfo)
        {
            if (!CanLaunchProjectile || !targetCell.IsValid) return;

            Projectile projectile = (Projectile)GenSpawn.Spawn(
                Props.projectileDef,
                Pawn.Position,
                Pawn.Map);

            projectile.Launch(
                launcher: Pawn,
                origin: Pawn.DrawPos,
                usedTarget: targetInfo,
                intendedTarget: targetInfo,
                hitFlags: ProjectileHitFlags.All,
                equipment: null,
                preventFriendlyFire: false);

            ticksUntilNextUse = Mathf.RoundToInt(
                Props.cooldownTicks * Pawn.GetStatValue(StatDefOf.MeleeCooldownFactor));

            if (Props.soundCast != null)
            {
                Props.soundCast.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
            }
        }
    }

    public class HediffCompProperties_LaunchProjectile : HediffCompProperties
    {
        public float range = 30f;
        public ThingDef projectileDef;
        public int cooldownTicks = 600; // 10 seconds
        public SoundDef soundCast;
        public string labelKey = "LaunchProjectile";
        public string descriptionKey = "Launch a projectile at target location";
        public Texture2D icon;

        public HediffCompProperties_LaunchProjectile()
        {
            compClass = typeof(HediffComp_LaunchProjectile);
        }
    }
}
