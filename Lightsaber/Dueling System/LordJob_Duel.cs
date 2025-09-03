using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

//TODO
namespace Lightsaber
{
    public class LordJob_Duel : LordJob_Champion
    {
        public LordJob_Duel(Pawn duelist, List<Pawn> guards) : base(duelist, guards)
        {
            point = duelist.PositionHeld;
        }

        public LordJob_Duel() { }

        public IntVec3 point;
        private float? wanderRadius;
        private float? defendRadius;
        private bool isCaravanSendable;
        private static float DuelStartRadius = 5;
        private HostilityResponseMode startingResponseMode = HostilityResponseMode.Flee;
        private int endAfterTick = -1;


        //Duel stuff imported from Ideo duel
        private Pawn playerDuelist;
        private bool duelStarted;
        private int attacksThisStage;
        private int movingTicks;
        private bool duelFinished;
        private static readonly IntRange AttacksPerStage = new IntRange(4, 8);
        private static readonly IntRange MovingTicksPerStage = new IntRange(360, 600);


        private StanceData aiCurrentStance;
        private StanceData playerCurrentStance;
        private int ticksUntilAiStanceChange;
        private int ticksUntilPlayerStanceChange;
        private const int MinStanceDuration = 180; // 3 seconds at normal speed
        private const int MaxStanceDuration = 600; // 10 seconds
        private const int StanceCooldown = 60; // 1 second

        public override bool IsCaravanSendable => isCaravanSendable;

        public LiveDuelBehaviorStage CurrentDuelStage
        {
            get => duelFinished
                ? LiveDuelBehaviorStage.BackOff
                : attacksThisStage <= 0 ? LiveDuelBehaviorStage.Move : LiveDuelBehaviorStage.Attack;
        }

        public override bool NeverInRestraints => true;


        public IEnumerable<Pawn> GetDuelists()
        {
            yield return Champion;
            if (playerDuelist != null)
                yield return playerDuelist;
        }

        public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason)
        {
            return reason != PawnLostCondition.Incapped && base.ShouldRemovePawn(p, reason);
        }

        public void StartDuelIfNotStartedYet()
        {
            if (duelStarted)
                return;

            foreach (Pawn pawn in GetDuelists())
            {
                if (pawn.drafter != null)
                    pawn.drafter.Drafted = false;
            }


            duelStarted = true;
            StartDuel();
        }

        private void InterruptDuelistJobs()
        {
            foreach (Pawn duelist in GetDuelists())
                duelist.jobs?.CheckForJobOverride();
        }

        private void StartDuel() => StartMoving();

        private void StartMoving()
        {
            attacksThisStage = 0;
            movingTicks = MovingTicksPerStage.RandomInRange;
            InterruptDuelistJobs();
        }
        private void StartAttacking()
        {
            movingTicks = 0;
            attacksThisStage = AttacksPerStage.RandomInRange;
            InterruptDuelistJobs();

            startingResponseMode = playerDuelist.playerSettings.hostilityResponse;
            playerDuelist.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
        }

        private void EndDuel()
        {
            if (!playerDuelist.Dead)
            {
                playerDuelist.playerSettings.hostilityResponse = startingResponseMode;
                playerDuelist.mindState.mentalStateHandler.Reset();
            }

            lord.Notify_SignalReceived(new Signal(lord.inSignalLeave));
        }

        public override void LordJobTick()
        {
            base.LordJobTick();

            if (duelStarted)
            {
                if (endAfterTick != -1 && endAfterTick < Find.TickManager.TicksGame)
                {
                    EndDuel();
                    return;
                }

                if (ticksUntilAiStanceChange-- <= 0 && !duelFinished)
                {
                    TrySwitchAiStance();
                    ticksUntilAiStanceChange = Rand.Range(MinStanceDuration, MaxStanceDuration);
                }

                if (ticksUntilPlayerStanceChange-- <= 0 && playerDuelist != null && !duelFinished)
                {
                    TrySwitchPlayerStance();
                    ticksUntilPlayerStanceChange = Rand.Range(MinStanceDuration, MaxStanceDuration);
                }

                Find.TickManager.slower.SignalForceNormalSpeedShort();

                foreach (Pawn duelist in GetDuelists())
                {
                    if (duelist.mindState != null)
                    {
                        if (duelist.mindState.lastAttackTargetTick == Find.TickManager.TicksGame)
                        {
                            NotifyMeleeAttack();
                            if (attacksThisStage <= 0)
                            {
                                StartMoving();
                                return;
                            }
                        }
                    }
                }
            }

            if (movingTicks <= 0)
                return;
            --movingTicks;
            if (movingTicks > 0)
                return;
            StartAttacking();
        }

        private void TrySwitchAiStance()
        {
            // Get the champion's current stance hediff
            var currentStanceHediff = Champion?.health?.hediffSet?.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance);

            // Get the player's current stance hediff
            var playerStanceHediff = playerDuelist?.health?.hediffSet?.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance);
            var playerStanceData = playerStanceHediff?.def.GetModExtension<DefStanceAngles>()?
                .GetStanceDataForSeverity(playerStanceHediff.Severity);

            // Get all possible stances from the hediff def
            var stanceExtension = LightsaberDefOf.Lightsaber_Stance.GetModExtension<DefStanceAngles>();
            if (stanceExtension == null) return;

            // Select optimal counter stance
            var newStance = SelectOptimalStance(stanceExtension, playerStanceData);
            if (newStance == null) return;
           
            if (duelFinished) return;
            ApplyStanceToPawn(Champion, newStance);
            aiCurrentStance = newStance;


            ShowStanceChangeMote(Champion, newStance, true);
        }

        private void TrySwitchPlayerStance()
        {
            if (playerDuelist == null || playerDuelist.Destroyed) return;

            var currentStanceHediff = playerDuelist?.health?.hediffSet?.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance);

            var aiStanceHediff = Champion?.health?.hediffSet?.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance);
            var aiStanceData = aiStanceHediff?.def.GetModExtension<DefStanceAngles>()?
                .GetStanceDataForSeverity(aiStanceHediff.Severity);

            var stanceExtension = LightsaberDefOf.Lightsaber_Stance.GetModExtension<DefStanceAngles>();
            if (stanceExtension == null) return;

            var newStance = SelectOptimalStance(stanceExtension, aiStanceData);
            if (newStance == null) return;

            ApplyStanceToPawn(playerDuelist, newStance);
            playerCurrentStance = newStance;
            ShowStanceChangeMote(playerDuelist, newStance, false);
        }

        private void ApplyStanceToPawn(Pawn pawn, StanceData stance)
        {
            // Remove existing stance hediff if any
            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(LightsaberDefOf.Lightsaber_Stance);
            if (existingHediff != null) 
            
            { existingHediff.Severity = stance.MinSeverity; }

            var lightsaberComp = pawn.equipment.Primary?.GetComp<Comp_LightsaberBlade>();
            if (lightsaberComp != null)
            {
                lightsaberComp.UpdateRotationForStance(stance.Angle);
                lightsaberComp.UpdateDrawOffsetForStance(stance.Offset);
            }
        }

        private void ShowStanceChangeMote(Pawn pawn, StanceData stance, bool isAi)
        {
            string text = isAi ? "AI switches to " : "You switch to ";
            text += stance.ShortLabel ?? stance.StanceID;

            MoteMaker.ThrowText(pawn.DrawPos + new Vector3(0, 0, 0.5f),
                pawn.Map,
                text,
                isAi ? Color.red : Color.green,
                2.5f);
        }

        private StanceData SelectOptimalStance(DefStanceAngles stanceExtension, StanceData opponentStance)
        {
            if (stanceExtension?.stanceData == null || !stanceExtension.stanceData.Any())
                return null;

            if (opponentStance == null)
                return stanceExtension.stanceData.RandomElement();

            var counterStances = stanceExtension.stanceData
                .Where(s => s.StrongAgainst.Contains(opponentStance.StanceID))
                .ToList();

            if (counterStances.Any())
                return counterStances.RandomElementByWeight(s =>
                    s.StrongAgainst.Count(so => so == opponentStance.StanceID));

            var safeStances = stanceExtension.stanceData
                .Where(s => !opponentStance.StrongAgainst.Contains(s.StanceID))
                .ToList();

            return safeStances.Any() ? safeStances.RandomElement() : stanceExtension.stanceData.RandomElement();
        }


        public Pawn GetDuelistPawn()
        {
            if (playerDuelist == null)
            {
                if (!FindDuelist())
                {
                    Log.Error("Couldn't find a duelist.");
                }
            }
            return playerDuelist;
        }

        public Pawn Opponent(Pawn duelist)
        {
            return GetDuelists().ToList()[GetDuelists().ToList().IndexOf(duelist) == 0 ? 1 : 0];
        }

        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            if (p == Champion)
            {
                //Champion dies
                playerDuelist.lord.RemovePawn(playerDuelist);

                //TODO here is how you woul do the logic for rewarding a successful duel
            }
            else if (p == playerDuelist)
            {
                //Player dies
            }
            else
            {
                return;
            }
            duelFinished = true;
            endAfterTick = Find.TickManager.TicksGame + 240;
            InterruptDuelistJobs();
        }

        private void NotifyMeleeAttack()
        {
            --attacksThisStage;
        }

        public override bool BlocksSocialInteraction(Pawn pawn) => GetDuelists().Contains(pawn);

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            LordToil_IdleBeforeDuel idle = new LordToil_IdleBeforeDuel(Guards, Champion);
            graph.AddToil(idle);
            LordToil_GuardDuel guard = new LordToil_GuardDuel(Guards, Champion);
            graph.AddToil(guard);
            LordToilLiveDuel duel = new LordToilLiveDuel(Guards, Champion);
            graph.AddToil(duel);


            Transition beginGuarding = new Transition(idle, guard);
            beginGuarding.AddTrigger(new Trigger_HostilePawnNearby());
            graph.AddTransition(beginGuarding);


            Transition beginDueling = new Transition(guard, duel);
            beginDueling.AddTrigger(new Trigger_ValidDuelistNear(point, DuelStartRadius));
            graph.AddTransition(beginDueling);



            return graph;
        }

        public bool FindDuelist()
        {
            foreach (Pawn pawn1 in lord.Map.mapPawns.AllHumanlikeSpawned)
            {
                //TODO the "GWPA_Duelist" hediff is used to both mark the pawn as duelist internally, as well as make the duel more exciting by giving both pawns damage reduction.
                //Replace this hediff with your own effects (or just create a hidden hediff with no effect to make it work)
                if (pawn1.health.hediffSet.TryGetHediff(LightsaberDefOf.Force_Duelist, out Hediff _))
                {
                    playerDuelist = pawn1;
                    //TODO the "GWPA_Dueling mental state just exists to prevent player from controlling the pawn
                    playerDuelist.mindState.mentalStateHandler.TryStartMentalState(LightsaberDefOf.Force_Dueling);

                    if (!lord.ownedPawns.Contains(playerDuelist))
                        lord.AddPawn(playerDuelist);
                    return true;
                }
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
            Scribe_Values.Look(ref wanderRadius, "wanderRadius");
            Scribe_Values.Look(ref defendRadius, "defendRadius");
            Scribe_Values.Look(ref isCaravanSendable, "isCaravanSendable");
            Scribe_References.Look(ref playerDuelist, "duelists");
            Scribe_Values.Look(ref movingTicks, "movingTicks");
            Scribe_Values.Look(ref attacksThisStage, "attacksThisStage");
            Scribe_Values.Look(ref duelStarted, "duelStarted");
            Scribe_Values.Look(ref startingResponseMode, "startingResponseMode");
            Scribe_Values.Look(ref endAfterTick, "endAfterTick");
        }
    }

    public enum LiveDuelBehaviorStage
    {
        Attack,
        Move,
        BackOff
    }
}