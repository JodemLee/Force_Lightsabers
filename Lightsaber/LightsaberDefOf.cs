using RimWorld;
using Verse;
using Verse.AI;

namespace Lightsaber
{
    [DefOf]
    public class LightsaberDefOf
    {
        public static FleckDef Force_LightsaberScorch_Fleck;
        public static FleckDef Force_KyberBleedBubble;
        public static HediffDef Lightsaber_Stance;
        public static StatDef Force_Lightsaber_Deflection;
        public static JobDef Force_UpgradeLightsaber;
        public static EffecterDef Force_LClashOne;
        public static EffecterDef Force_SteamVapor;
        public static DamageDef Force_BluntLightsaber;

        public static HiltPartCategoryDef Force_PowerCell;
        public static HiltPartCategoryDef Force_Crystal;
        public static HiltPartCategoryDef Force_Lens;
        public static HiltPartCategoryDef Force_Emitter;
        public static HiltPartCategoryDef Force_Casing;
        public static SoundDef Force_KyberBleedSound;

        public static HiltPartDef Force_SyntheticKyberCrystalHiltPart;
        public static ThingDef Force_SyntheticCrystal;

        public static HediffDef Force_LightsaberShortCircuit;


        //Lightsaber Mechanics//
        public static JobDef Force_AwaitDuel;
        public static JobDef Force_GuardDuel;
        public static MentalStateDef Force_Dueling;
        public static HediffDef Force_Duelist;
        public static HediffDef Force_Champion;
        public static DutyDef Force_GuardDuelDuty;
        public static DutyDef Force_LiveDuel;
        public static DutyDef Force_PrepareToDuelDuty;
        public static LetterDef Force_DuelChallenge;

    }
}
