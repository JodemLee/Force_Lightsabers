using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Lightsaber
{
    internal class TraitDegreeColorData
    {
        public int degree;
        public Color color;
    }

    internal class ModExtension_TraitColor : DefModExtension
    {
        public List<TraitDegreeColorData> degreeColorData;

        public Color GetColorForDegree(int degree)
        {
            if (degreeColorData != null)
            {
                foreach (var data in degreeColorData)
                {
                    if (data.degree == degree)
                    {
                        return data.color;
                    }
                }
            }
            return Color.white;
        }
    }
}
