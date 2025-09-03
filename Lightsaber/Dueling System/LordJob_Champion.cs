using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
	public abstract class LordJob_Champion : LordJob
	{
		public Pawn Champion;
		public List<Pawn> Guards;

		public LordJob_Champion()
		{
			
		}
		
		public LordJob_Champion(Pawn champion, List<Pawn> guards)
		{
			Champion = champion;
			Guards = guards;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref Champion, "champion");
			Scribe_Collections.Look(ref Guards, "guards", LookMode.Reference
			);
		}
	}
}