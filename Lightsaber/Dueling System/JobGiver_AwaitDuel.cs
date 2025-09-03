using Lightsaber;
using RimWorld;
using Verse;
using Verse.AI;

//TODO
namespace Lightsaber
{
	public class JobGiver_AwaitDuel : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn) => JobMaker.MakeJob(LightsaberDefOf.Force_AwaitDuel);
	}
}