using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

//TODO
namespace Lightsaber
{
	public class JobDriver_GuardDuel : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil walkTo = Toils_Goto.GotoCell(job.targetA.Cell, PathEndMode.OnCell);
			yield return walkTo;
			
			
			Toil stand = Toils_General.Wait(int.MaxValue);
			stand.tickIntervalAction = delta =>
			{
				pawn.rotationTracker.FaceTarget(job.targetB);
				pawn.GainComfortFromCellIfPossible(delta);
				Pawn actor = stand.actor;
				if (!actor.IsHashIntervalTick(100))
					return;
				actor.jobs.CheckForJobOverride();
			};
			stand.defaultCompleteMode = ToilCompleteMode.Never;
			stand.handlingFacing = true;
			yield return stand;
		}
	}
}