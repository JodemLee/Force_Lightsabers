using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lightsaber
{
    public class HiltEffectors
    {
        public EffecterDef EffecterDef;
        public float minTime = 60;
        public float maxTime = 300;
        public bool shouldMaintain;
        public int ticksToMaintain => (int)Math.Round(maxTime - minTime);
    }
}
