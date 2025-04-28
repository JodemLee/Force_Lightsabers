using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Lightsaber
{
    internal class JobDriver_UpgradeLightsaber : JobDriver
    {
        private Thing requiredComponent;
        private HiltPartDef selectedHiltPart;
        private HiltPartDef previousHiltPart;

        public const TargetIndex RequiredComponentInd = TargetIndex.A;
        public const int WorkTimeTicks = 1000;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref requiredComponent, "requiredComponent");
            Scribe_Defs.Look(ref selectedHiltPart, "selectedHiltPart");
            Scribe_Defs.Look(ref previousHiltPart, "previousHiltPart");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            requiredComponent = job.GetTarget(TargetIndex.A).Thing;

            if (requiredComponent == null)
            {
                Log.Warning("[TheForce_Psycast] JobDriver_UpgradeLightsaber: Missing required component.");
                return false;
            }

            selectedHiltPart = ((Job_UpgradeLightsaber)job).selectedhiltPartDef;
            previousHiltPart = ((Job_UpgradeLightsaber)job).previoushiltPartDef;

            if (selectedHiltPart == null)
            {
                Log.Warning("[TheForce_Psycast] JobDriver_UpgradeLightsaber: selectedHiltPart is null.");
                return false;
            }

            if (!pawn.CanReserveAndReach(requiredComponent, PathEndMode.ClosestTouch, Danger.None, 10 , -1, null, errorOnFailed))
            {
                Log.Warning($"Failed to reserve required component {requiredComponent.Label} for job {job.def}");
                return false;
            }

            Comp_LightsaberBlade lightsaberComp = pawn.equipment?.Primary?.GetComp<Comp_LightsaberBlade>();
            if (lightsaberComp == null)
            {
                Log.Warning($"Pawn {pawn.Name} does not have a lightsaber equipped. Job cannot proceed.");
                Messages.Message($"Pawn {pawn.Name} does not have a lightsaber equipped. Job cannot proceed.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            if (selectedHiltPart == null)
            {
                Log.Error("[TheForce_Psycast] JobDriver_UpgradeLightsaber: selectedHiltPart is null. Ending job.");
                EndJobWith(JobCondition.Incompletable);
                yield break;
            }

            Comp_LightsaberBlade lightsaberComp = pawn.equipment?.Primary?.GetComp<Comp_LightsaberBlade>();
            if (lightsaberComp == null)
            {
                Log.Error($"[TheForce_Psycast] Pawn {pawn.Name} does not have a lightsaber equipped. Ending job.");
                EndJobWith(JobCondition.Incompletable);
                yield break;
            }

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil waitToil = Toils_General.WaitWith(TargetIndex.A, WorkTimeTicks, useProgressBar: true);
            yield return waitToil;

            yield return Toils_General.Do(delegate
            {
                if (requiredComponent == null)
                {
                    Log.Error("[TheForce_Psycast] Required component is null during upgrade. Ending job.");
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (requiredComponent.stackCount > 1)
                {
                    requiredComponent.SplitOff(1).Destroy(DestroyMode.Vanish);
                }
                else
                {
                    requiredComponent.Destroy(DestroyMode.Vanish);
                }

                Messages.Message($"Pawn {pawn.Name} has upgraded their lightsaber with the {selectedHiltPart.label}.", MessageTypeDefOf.PositiveEvent, false);

                lightsaberComp.HiltManager.AddHiltPart(selectedHiltPart);

                if (previousHiltPart != null)
                {
                    lightsaberComp.HiltManager.RemoveHiltPart(previousHiltPart);

 
                    Thing droppedComponent = ThingMaker.MakeThing(previousHiltPart.droppedComponent);
                    if (droppedComponent != null)
                    {
                        GenPlace.TryPlaceThing(
                            droppedComponent,
                            lightsaberComp.Wearer.Position,
                            lightsaberComp.Wearer.Map,
                            ThingPlaceMode.Near);
                    }
                }
            });
        }
    }
}
