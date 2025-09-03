using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
	public class LordToilData_GuardDuel: LordToilData
	{
		public List<Pawn> guards;
		public Pawn duelist;

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref guards, "guards", LookMode.Reference);
			Scribe_References.Look(ref duelist, "duelist");
		}
	}
}