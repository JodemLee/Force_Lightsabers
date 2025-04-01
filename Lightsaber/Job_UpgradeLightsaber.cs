using Verse;
using Verse.AI;

namespace Lightsaber
{
    public class Job_UpgradeLightsaber : Job
    {
        public HiltPartDef selectedhiltPartDef;
        public HiltPartDef previoushiltPartDef;

        public new void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref selectedhiltPartDef, "selectedhiltPartDef");
            Scribe_Defs.Look(ref previoushiltPartDef, "previoushiltPartDef");
        }
    }
}
