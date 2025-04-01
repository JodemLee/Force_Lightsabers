using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Lightsaber
{
    class GenStep_ScatterLightsaberCrystals : GenStep_ScatterGroup
    {

        private HashSet<IntVec3> rockCells = new HashSet<IntVec3>();
        private HashSet<IntVec3> possibleSpawnCells = new HashSet<IntVec3>();

        public override void Generate(Map map, GenStepParams parms)
        {
            MapGenFloatGrid caves = MapGenerator.Caves;
            MapGenFloatGrid elevation = MapGenerator.Elevation;

            float caveThreshold = 0.7f;
            int caveCellsCount = 0;

            rockCells.Clear(); // Clear HashSet before each use
            List<IntVec3> factionCells = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (elevation[cell] > caveThreshold)
                {
                    rockCells.Add(cell);
                }
                if (caves[cell] > 0f)
                {
                    caveCellsCount++;
                }
                if (map.thingGrid.ThingsListAtFast(cell).Any(thing => thing.Faction != null))
                {
                    factionCells.Add(cell);
                }
            }
            GenMorphology.Dilate(factionCells, 50, map);
            HashSet<IntVec3> factionCellSet = new HashSet<IntVec3>(factionCells);
            int numFormations = GenMath.RoundRandom((float)caveCellsCount / 1000f);
            GenMorphology.Erode(rockCells.ToList(), 10, map);
            possibleSpawnCells.Clear();
            foreach (IntVec3 cell in rockCells)
            {
                if (caves[cell] > 0f && !factionCellSet.Contains(cell))
                {
                    possibleSpawnCells.Add(cell);
                }
            }
            for (int i = 0; i < numFormations && possibleSpawnCells.Count > 0; i++)
            {

                IntVec3 result = possibleSpawnCells.RandomElement();
                possibleSpawnCells.Remove(result);
                ScatterAt(result, map, parms);
            }
        }
    }
}
