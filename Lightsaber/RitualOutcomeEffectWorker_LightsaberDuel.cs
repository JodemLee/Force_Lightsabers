using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace Lightsaber
{
    internal class RitualOutcomeEffectWorker_LightsaberDuel : RitualOutcomeEffectWorker_FromQuality
    {
        // Constants renamed to better reflect sparring context
        private const float RecreationGainGood = 0.25f;
        private const float RecreationGainBest = 0.5f;
        private const float MeleeXPGainParticipantsGood = 2500f;
        private const float MeleeXPGainSpectatorsGood = 1000f;
        private const float MeleeXPGainParticipantsBest = 5000f;
        private const float MeleeXPGainSpectatorsBest = 2000f;
        private const float MeleeXPLossOnDeath = -1000f; // Penalty for fatal outcome

        public RitualOutcomeEffectWorker_LightsaberDuel() { }

        public RitualOutcomeEffectWorker_LightsaberDuel(RitualOutcomeEffectDef def)
            : base(def) { }

        protected override bool OutcomePossible(RitualOutcomePossibility chance, LordJob_Ritual ritual)
        {
            // For sparring, death automatically means worst outcome
            if (ritual is LordJob_Ritual_LightsaberDuel duelRitual && duelRitual.duelists.Any(d => d.Dead))
            {
                return !chance.BestPositiveOutcome(ritual);
            }
            return true;
        }

        protected override void ApplyExtraOutcome(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual,
            RitualOutcomePossibility outcome, out string extraOutcomeDesc, ref LookTargets letterLookTargets)
        {
            extraOutcomeDesc = null;

            if (!(jobRitual is LordJob_Ritual_LightsaberDuel duelRitual))
            {
                return;
            }

            // Handle fatal outcome first
            if (duelRitual.duelists.Any(d => d.Dead))
            {
                ApplyDeathPenalties(duelRitual.duelists, totalPresence.Keys);
                extraOutcomeDesc = "RitualOutcomeExtraDesc_DuelEndedInDeath".Translate();
                return;
            }

            // Normal sparring outcomes
            if (outcome.Positive)
            {
                bool isBestOutcome = outcome.BestPositiveOutcome(jobRitual);
                float recreationAmount = isBestOutcome ? RecreationGainBest : RecreationGainGood;
                float participantXP = isBestOutcome ? MeleeXPGainParticipantsBest : MeleeXPGainParticipantsGood;
                float spectatorXP = isBestOutcome ? MeleeXPGainSpectatorsBest : MeleeXPGainSpectatorsGood;

                foreach (Pawn pawn in totalPresence.Keys)
                {
                    if (duelRitual.duelists.Contains(pawn))
                    {
                        pawn.skills.Learn(SkillDefOf.Melee, participantXP);
                    }
                    else
                    {
                        pawn.skills.Learn(SkillDefOf.Melee, spectatorXP);
                        if (pawn.needs.joy != null)
                        {
                            pawn.needs.joy.GainJoy(recreationAmount, JoyKindDefOf.Social);
                        }
                    }
                }
            }
        }

        private void ApplyDeathPenalties(List<Pawn> duelists, IEnumerable<Pawn> allParticipants)
        {
            foreach (Pawn pawn in allParticipants)
            {
                float xpLoss = duelists.Contains(pawn) ? MeleeXPLossOnDeath * 2 : MeleeXPLossOnDeath;
                pawn.skills.Learn(SkillDefOf.Melee, xpLoss);

                if (pawn.needs.joy != null && !duelists.Contains(pawn))
                {
                    pawn.needs.joy.CurLevel -= 0.15f;
                }
            }
        }
    }
}