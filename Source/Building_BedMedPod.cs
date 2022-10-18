using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System;
using Verse.Sound;
using Verse.AI;

namespace MedPod
{
    public class Building_BedMedPod : Building_Bed
    {
        public CompPowerTrader powerComp;

        public CompMedPodSettings medpodSettings;

        public CompTreatmentRestrictions treatmentRestrictions;

        private List<Hediff> patientTreatableHediffs;

        private static float patientSavedFoodNeed;

        private static float patientSavedDbhThirstNeed;

        private static List<Trait> patientTraitsToRemove;

        private float totalNormalizedSeverities = 0;

        public int DiagnosingTicks = 0;

        public int MaxDiagnosingTicks;

        public int PatientBodySizeScaledMaxDiagnosingTicks;

        public int HealingTicks = 0;

        public int MaxHealingTicks;

        public int PatientBodySizeScaledMaxHealingTicks;

        public float DiagnosingPowerConsumption;

        public float HealingPowerConsumption;

        public List<HediffDef> AlwaysTreatableHediffs;

        public List<HediffDef> NeverTreatableHediffs;

        public List<HediffDef> NonCriticalTreatableHediffs;

        public List<HediffDef> UsageBlockingHediffs;

        public List<TraitDef> UsageBlockingTraits;

        public List<TraitDef> AlwaysTreatableTraits;

        public List<string> DisallowedRaces;

        public int ProgressHealingTicks = 0;

        public int TotalHealingTicks = 0;

        public int gantryPositionPercentInt = 0;

        public bool gantryDirectionForwards = true;

        public bool Aborted = false;

        public enum MedPodStatus
        {
            Idle = 0,
            DiagnosisStarted,
            DiagnosisFinished,
            HealingStarted,
            HealingFinished,
            PatientDischarged,
            Error
        }

        public MedPodStatus status = MedPodStatus.Idle;

        private Sustainer wickSustainer;

        private IntVec3 InvisibleBlockerPosition
        {
            get
            {
                IntVec3 position;
                if (Rotation == Rot4.North)
                {
                    position = new IntVec3(Position.x + 0, Position.y, Position.z - 1);
                }
                else if (Rotation == Rot4.East)
                {
                    position = new IntVec3(Position.x - 1, Position.y, Position.z + 0);
                }
                else if (Rotation == Rot4.South)
                {
                    position = new IntVec3(Position.x + 0, Position.y, Position.z + 1);
                }
                else // Default: West
                {
                    position = new IntVec3(Position.x + 1, Position.y, Position.z + 0);
                }

                return position;
            }
        }

        private Thing resultingBlocker;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            medpodSettings = GetComp<CompMedPodSettings>();
            treatmentRestrictions = GetComp<CompTreatmentRestrictions>();

            MaxDiagnosingTicks = GenTicks.SecondsToTicks(medpodSettings.MaxDiagnosisTime);
            MaxHealingTicks = GenTicks.SecondsToTicks(medpodSettings.MaxPerHediffHealingTime);
            DiagnosingPowerConsumption = medpodSettings.DiagnosisModePowerConsumption;
            HealingPowerConsumption = medpodSettings.HealingModePowerConsumption;

            AlwaysTreatableHediffs = treatmentRestrictions.AlwaysTreatableHediffs;
            NeverTreatableHediffs = treatmentRestrictions.NeverTreatableHediffs;
            NonCriticalTreatableHediffs = treatmentRestrictions.NonCriticalTreatableHediffs;
            UsageBlockingHediffs = treatmentRestrictions.UsageBlockingHediffs;
            UsageBlockingTraits = treatmentRestrictions.UsageBlockingTraits;
            AlwaysTreatableTraits = treatmentRestrictions.AlwaysTreatableTraits;
            DisallowedRaces = treatmentRestrictions.DisallowedRaces;

            // Add a blocker region for the MedPod main machinery, if required
            if (!medpodSettings.DisableInvisibleBlocker)
            { 
                // (If one already exists, then we are probably loading a save with an existing MedPod)
                Thing something = Map.thingGrid.ThingsListAtFast(InvisibleBlockerPosition).FirstOrDefault(x => x.def.Equals(MedPodDef.MedPodInvisibleBlocker));

                if (something != null)
                {
                    something.DeSpawn();
                }

                Thing t = ThingMaker.MakeThing(MedPodDef.MedPodInvisibleBlocker);
                GenPlace.TryPlaceThing(t, InvisibleBlockerPosition, Map, ThingPlaceMode.Direct, out resultingBlocker, null, null, Rotation);
            }
        }

        private Pawn PatientPawn
        {
            get
            {
                if (GetCurOccupant(0) != null)
                {
                    return GetCurOccupant(0);
                }
                return null;
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (powerComp.PowerOn && ((status == MedPodStatus.DiagnosisFinished) || (status == MedPodStatus.HealingStarted) || (status == MedPodStatus.HealingFinished)))
            {
                WakePatient(PatientPawn, false);
            }
            ForPrisoners = false;
            Medical = false;

            // Remove the blocker region, if required
            if (!medpodSettings.DisableInvisibleBlocker)
            {
                resultingBlocker.DeSpawn();
            }

            District district = this.GetDistrict();
            base.DeSpawn(mode);
            if (district != null)
            {
                district.Notify_RoomShapeOrContainedBedsChanged();
                district.Room.Notify_RoomShapeChanged();
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            string inspectorStatus = null;

            if (ParentHolder != null && !(ParentHolder is Map))
            {
                // If minified, don't show computer and feedstock check Inspector messages
            }
            else
            {
                stringBuilder.AppendInNewLine(powerComp.CompInspectStringExtra());

                if (def.building.bed_humanlike)
                {
                    switch (ForOwnerType)
                    {
                        case BedOwnerType.Prisoner:
                            stringBuilder.AppendInNewLine("ForPrisonerUse".Translate());
                            break;
                        case BedOwnerType.Slave:
                            stringBuilder.AppendInNewLine("ForSlaveUse".Translate());
                            break;
                        case BedOwnerType.Colonist:
                            stringBuilder.AppendInNewLine("ForColonistUse".Translate());
                            break;
                        default:
                            Log.Error($"Unknown bed owner type: {ForOwnerType}");
                            break;
                    }
                }

                if (!powerComp.PowerOn)
                {
                    inspectorStatus = "MedPod_InspectorStatus_NoPower".Translate();
                }
                else
                {
                    switch (status)
                    {
                        case MedPodStatus.DiagnosisStarted:
                            float diagnosingProgress = (float)(PatientBodySizeScaledMaxDiagnosingTicks - DiagnosingTicks) / PatientBodySizeScaledMaxDiagnosingTicks * 100;
                            inspectorStatus = "MedPod_InspectorStatus_DiagnosisProgress".Translate((int)diagnosingProgress);
                            break;
                        case MedPodStatus.DiagnosisFinished:
                            inspectorStatus = "MedPod_InspectorStatus_DiagnosisComplete".Translate();
                            break;
                        case MedPodStatus.HealingStarted:
                        case MedPodStatus.HealingFinished:
                            float healingProgress = (float)ProgressHealingTicks / TotalHealingTicks * 100;
                            inspectorStatus = "MedPod_InspectorStatus_HealingProgress".Translate((int)healingProgress);
                            break;
                        case MedPodStatus.PatientDischarged:
                            inspectorStatus = "MedPod_InspectorStatus_PatientDischarged".Translate();
                            break;
                        case MedPodStatus.Idle:
                        default:
                            inspectorStatus = "MedPod_InspectorStatus_Idle".Translate();
                            break;
                    }
                }

                stringBuilder.AppendInNewLine(inspectorStatus);
            }

            return stringBuilder.ToString();
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            if (myPawn.RaceProps.Humanlike && !ForPrisoners && Medical && !myPawn.Drafted && Faction == Faction.OfPlayer && RestUtility.CanUseBedEver(myPawn, def))
            {
                if (!MedPodHealthAIUtility.IsValidRaceForMedPod(myPawn, DisallowedRaces))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_RaceNotAllowed".Translate(myPawn.def.label.CapitalizeFirst()) + ")", null);
                    yield break;
                }
                if (!MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, AlwaysTreatableHediffs, NeverTreatableHediffs, NonCriticalTreatableHediffs))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "NotInjured".Translate() + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, AlwaysTreatableHediffs, NeverTreatableHediffs, NonCriticalTreatableHediffs) && !powerComp.PowerOn)
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_Unpowered".Translate() + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, AlwaysTreatableHediffs, NeverTreatableHediffs, NonCriticalTreatableHediffs) && this.IsForbidden(myPawn))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "ForbiddenLower".Translate() + ")", null);
                    yield break;
                }
                Action action = delegate
                {
                    if (!ForPrisoners && Medical && myPawn.CanReserveAndReach(this, PathEndMode.ClosestTouch, Danger.Deadly, SleepingSlotsCount, -1, null, ignoreOtherReservations: true))
                    {
                        if (myPawn.CurJobDef == JobDefOf.LayDown && myPawn.CurJob.GetTarget(TargetIndex.A).Thing == this)
                        {
                            myPawn.CurJob.restUntilHealed = true;
                        }
                        else
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.LayDown, this);
                            job.restUntilHealed = true;
                            myPawn.jobs.TryTakeOrderedJob(job);
                        }
                        myPawn.mindState.ResetLastDisturbanceTick();
                    }
                };
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("UseMedicalBed".Translate(), action), myPawn, this, (AnyUnoccupiedSleepingSlot ? "ReservedBy" : "SomeoneElseSleeping").CapitalizeFirst());
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            string medicalToggleStr = "CommandBedSetAsMedicalLabel".Translate();
            string flickablePowerToggleStr = "CommandDesignateTogglePowerLabel".Translate();
            string allowToggleStr = "CommandAllow".Translate();
            string forPrisonersToggleStr = "CommandBedSetForPrisonersLabel".Translate();

            var gizmosToDisableWhileInUse = new string[] 
            {
                flickablePowerToggleStr,
                allowToggleStr,
                forPrisonersToggleStr
            };

            foreach (Gizmo g in base.GetGizmos())
            {
                if (g is Command_Toggle act && (act.defaultLabel == medicalToggleStr))
                {
                    continue; // Hide the Medical bed toggle, as MedPods are always Medical beds
                }

                if ((g is Command_Toggle act2 && (gizmosToDisableWhileInUse.Contains(act2.defaultLabel)) || g is Command_SetBedOwnerType) && PatientPawn != null)
                {
                    g.Disable("MedPod_CommandGizmoDisabled_MedPodInUse".Translate(def.LabelCap)); // Disable various gizmos while MedPod is in use
                }

                yield return g;
            }

            yield return new Command_Action
            {
                defaultLabel = "MedPod_CommandGizmoAbortTreatment_Label".Translate(),
                defaultDesc = "MedPod_CommandGizmoAbortTreatment_Desc".Translate(),
                disabled = (PatientPawn == null),
                action = delegate
                {
                    if (PatientPawn != null)
                    {
                        // If the patient is incapable of walking after being kicked off the MedPod, physically push them off
                        if (PatientPawn.Downed)
                        {
                            int offsetX = 0;
                            int offsetZ = 0;
                            if (Rotation == Rot4.North)
                            {
                                offsetX++;
                            }
                            else if (Rotation == Rot4.East)
                            {
                                offsetZ--;
                            }
                            else if (Rotation == Rot4.South)
                            {
                                offsetX--;
                            }
                            else // Default: West
                            {
                                offsetZ++;
                            }
                            PatientPawn.Position += new IntVec3(offsetX, 0, offsetZ);
                        }
                        Aborted = true;
                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/AbortTreatment", true)
            };
        }

        private void SwitchState()
        {
            switch (status)
            {
                case MedPodStatus.Idle:
                    status = MedPodStatus.DiagnosisStarted;
                    break;

                case MedPodStatus.DiagnosisStarted:
                    status = MedPodStatus.DiagnosisFinished;
                    break;

                case MedPodStatus.DiagnosisFinished:
                    status = MedPodStatus.HealingStarted;
                    break;

                case MedPodStatus.HealingStarted:
                    status = MedPodStatus.HealingFinished;
                    break;

                case MedPodStatus.HealingFinished:
                    status = MedPodStatus.PatientDischarged;
                    break;

                case MedPodStatus.PatientDischarged:
                    status = MedPodStatus.Idle;
                    break;

                default:
                    status = MedPodStatus.Error;
                    break;
            }
        }

        public bool GantryMoving()
        {
            bool result;
            switch (status)
            {
                case MedPodStatus.DiagnosisStarted:
                case MedPodStatus.DiagnosisFinished:
                case MedPodStatus.HealingStarted:
                case MedPodStatus.HealingFinished:
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }

        private void DiagnosePatient(Pawn patientPawn)
        {
            // List all of the patient's hediffs/injuries, sorted by body part hierarchy then severity
            // Hediffs with no body part defined (i.e. "Whole Body" hediffs) are moved to the bottom of the list)
            patientTreatableHediffs = patientPawn.health.hediffSet.hediffs.OrderBy((Hediff x) => x.Part == null ? 9999 : x.Part.Index).ThenByDescending((Hediff x) => x.Severity).ToList();

            // Ignore missing child parts of limbs and other body parts that have been replaced with
            // implants or prosthetics
            // This is a multi-step process:
            // - Find the hediffs (and the associated body parts) corresponding to implants/prosthetics
            // - Identify the child parts affected by the implants/prosthetics
            // - Remove the hediffs from the treatment list by body part

            List<Hediff> artificialPartHediffs = patientTreatableHediffs.FindAll((Hediff x) => x.def.hediffClass.Equals(typeof(Hediff_AddedPart)));

            List<BodyPartRecord> childPartsToSkip = new List<BodyPartRecord>();

            foreach (Hediff currentArtificialPartHediff in artificialPartHediffs)
            {
                childPartsToSkip.AddRange(GetBodyPartDescendants(currentArtificialPartHediff.Part));
            }

            // Only ignore Missing part Hediffs from body parts that have been replaced
            patientTreatableHediffs.RemoveAll((Hediff x) => childPartsToSkip.Any(p => x.Part == p) && x.def.hediffClass == typeof(Hediff_MissingPart));

            // Ignore hediffs/injuries that are:
            // - Not explicitly whitelisted as always treatable
            // - Blacklisted as never treatable
            // - Not explicitly greylisted as non-critical but treatable
            // - Not bad (i.e isBad = false) and not treatable
            patientTreatableHediffs.RemoveAll((Hediff x) =>
                !AlwaysTreatableHediffs.Contains(x.def) && (NeverTreatableHediffs.Contains(x.def) || (!NonCriticalTreatableHediffs.Contains(x.def) && !x.def.isBad && !x.TendableNow())));

            // Induce coma in the patient so that they don't run off during treatment
            // (Pawns tend to get up as soon as they are "no longer incapable of walking")
            AnesthesizePatient(patientPawn);

            // Calculate individual and total cumulative treatment time for each hediff/injury
            foreach (Hediff currentHediff in patientTreatableHediffs)
            {
                float currentSeverity = currentHediff.Severity;

                // currentHediff.Part will throw an error if a hediff is applied to the whole body (e.g. malnutrition), as part == null
                float currentBodyPartMaxHealth = (currentHediff.Part != null) ? EbfCompatibilityWrapper.GetMaxHealth(currentHediff.Part.def, patientPawn, currentHediff.Part) : 1;

                float currentNormalizedSeverity = (currentSeverity < 1) ? currentSeverity : currentSeverity / currentBodyPartMaxHealth;

                totalNormalizedSeverities += currentNormalizedSeverity;

                TotalHealingTicks += (int)Math.Ceiling(GetHediffNormalizedSeverity(currentHediff) * PatientBodySizeScaledMaxHealingTicks);

                // Tend all bleeding hediffs immediately so the pawn doesn't die after being anesthetized by the MedPod
                // The Hediff will be completely removed once the Medpod is done with the Healing process
                if (currentHediff.Bleeding)
                {
                    currentHediff.Tended(1,1); // TODO - Replace with new method name once it no longer has a temporary name
                }
            }

            // Identify treatable traits for removal
            patientTraitsToRemove = patientPawn.story?.traits.allTraits.FindAll(x => AlwaysTreatableTraits.Contains(x.def));
        }

        private float GetHediffNormalizedSeverity(Hediff specificHediff = null)
        {
            Hediff currentHediff = (specificHediff == null) ? patientTreatableHediffs.First() : specificHediff;

            float currentHediffSeverity = currentHediff.Severity;

            float currentHediffBodyPartMaxHealth = (currentHediff.Part != null) ? EbfCompatibilityWrapper.GetMaxHealth(currentHediff.Part.def, PatientPawn, currentHediff.Part) : 1;

            float currentHediffNormalizedSeverity = (currentHediffSeverity < 1) ? currentHediffSeverity : currentHediffSeverity / currentHediffBodyPartMaxHealth;

            return currentHediffNormalizedSeverity;
        }

        private List<BodyPartRecord> GetBodyPartDescendants(BodyPartRecord part)
        {
            List<BodyPartRecord> childParts = new List<BodyPartRecord>();

            if (part.parts.Count > 0)
            {
                foreach (BodyPartRecord currentChildPart in part.parts)
                {
                    childParts.Add(currentChildPart);
                    childParts.AddRange(GetBodyPartDescendants(currentChildPart));
                }
            }

            return childParts;
        }

        public static void AnesthesizePatient(Pawn patientPawn)
        {
            Hediff inducedComa = HediffMaker.MakeHediff(HediffDef.Named("MedPod_InducedComa"), patientPawn);
            patientPawn.health.AddHediff(inducedComa);
        }

        public static void WakePatient(Pawn patientPawn, bool wakeNormally = true)
        {
            patientPawn.health.hediffSet.hediffs.RemoveAll((Hediff x) => x.def.defName == "MedPod_InducedComa");

            // Clear any ongoing mental states (e.g. Manhunter)
            if (patientPawn.InMentalState)
            {
                patientPawn.MentalState.RecoverFromState();
            }

            // Apply the appropriate cortical stimulation hediff, depending on whether the treatment was completed or interrupted            
            string corticalStimulationType = wakeNormally ? "MedPod_CorticalStimulation" : "MedPod_CorticalStimulationImproper";
            string popupMessage = wakeNormally ? "MedPod_Message_TreatmentComplete".Translate(patientPawn.LabelCap, patientPawn) : "MedPod_Message_TreatmentInterrupted".Translate(patientPawn.LabelCap, patientPawn);
            MessageTypeDef popupMessageType = wakeNormally ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeHealthEvent;

            Messages.Message(popupMessage, patientPawn, popupMessageType, true);

            Hediff corticalStimulation = HediffMaker.MakeHediff(HediffDef.Named(corticalStimulationType), patientPawn);
            patientPawn.health.AddHediff(corticalStimulation);

            // Restore previously saved patient food need level
            if (patientPawn.needs.food != null)
            {
                patientPawn.needs.food.CurLevelPercentage = patientSavedFoodNeed;
            }

            // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels
            if (ModCompatibility.DbhIsActive)
            {
                ModCompatibility.SetThirstNeedCurLevelPercentage(patientPawn, patientSavedDbhThirstNeed);
                ModCompatibility.SetBladderNeedCurLevelPercentage(patientPawn, 1f);
                ModCompatibility.SetHygieneNeedCurLevelPercentage(patientPawn, 1f);
            }

            // Remove treatable traits only if treatment was completed normally
            if (!patientTraitsToRemove.NullOrEmpty() && wakeNormally)
            { 
                patientPawn.story?.traits.allTraits.RemoveAll(x => patientTraitsToRemove.Contains(x));
                string letterLabel = "MedPod_Letter_TraitRemoved_Label".Translate();
                string letterText = "MedPod_Letter_TraitRemoved_Desc".Translate(patientPawn.Named("PAWN")) + string.Join("", (from t in patientTraitsToRemove select "\n- " + t.def.degreeDatas.FirstOrDefault().LabelCap).ToArray());
                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, new TargetInfo(patientPawn));
            }

            // If the patient is a surrogate from Android Tiers, try to reconnect them to their last known controller
            if (ModCompatibility.AndroidTiersIsActive)
            {
                Gizmo reconnectGizmo = patientPawn.GetGizmos().FirstOrDefault(x => x is Command_Action y && y.defaultLabel == "ATPP_AndroidSurrogateReconnectToLastController".Translate());

                if (reconnectGizmo != null)
                {
                    ((Command_Action)reconnectGizmo).action();
                }
            }
        }

        public void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            wickSustainer = def.building.soundDispense.TrySpawnSustainer(info);
        }

        public override void Tick()
        {
            base.Tick();

            if (!powerComp.PowerOn)
            {
                if (PatientPawn != null)
                {
                    if ((status == MedPodStatus.DiagnosisFinished) || (status == MedPodStatus.HealingStarted) || (status == MedPodStatus.HealingFinished))
                    {
                        // Wake patient up abruptly, as power was interrupted during treatment
                        WakePatient(PatientPawn, false);
                    }

                    if (status == MedPodStatus.PatientDischarged)
                    {                       
                        // Wake patient up normally, as treatment was already completed when power was interrupted
                        WakePatient(PatientPawn);
                    }
                }

                status = MedPodStatus.Idle;

                return;
            }

            powerComp.PowerOutput = -powerComp.Props.basePowerConsumption;

            if (this.IsHashIntervalTick(60))
            {

                if (PatientPawn != null)
                {
                    PatientBodySizeScaledMaxDiagnosingTicks = (int)(MaxDiagnosingTicks * PatientPawn.BodySize);
                    PatientBodySizeScaledMaxHealingTicks = (int)(MaxHealingTicks * PatientPawn.BodySize);

                    switch (status)
                    {
                        case MedPodStatus.Idle:
                            DiagnosingTicks = PatientBodySizeScaledMaxDiagnosingTicks;

                            // Save initial patient food need level
                            if (PatientPawn.needs.food != null)
                            {
                                patientSavedFoodNeed = PatientPawn.needs.food.CurLevelPercentage;
                            }

                            // Save initial patient DBH thirst and reset DBH bladder/hygiene need levels
                            if (ModCompatibility.DbhIsActive)
                            {
                                patientSavedDbhThirstNeed = ModCompatibility.GetThirstNeedCurLevelPercentage(PatientPawn);
                                ModCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                                ModCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                            }

                            SwitchState();
                            break;

                        case MedPodStatus.DiagnosisFinished:
                            DiagnosePatient(PatientPawn);

                            if (patientTreatableHediffs.NullOrEmpty())
                            {
                                // Skip treatment if no treatable hediffs are found
                                status = MedPodStatus.PatientDischarged;
                            }
                            else
                            {
                                // Scale healing time for current hediff according to its (normalized) severity and patient body size
                                // i.e. More severe hediffs take longer, bigger pawns also take longer
                                HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * PatientBodySizeScaledMaxHealingTicks);

                                SwitchState();
                            }
                            break;

                        case MedPodStatus.HealingFinished:
                            // Don't remove 'good' treatable Hediffs but instead treat them with 100% quality (unless the 'good' Hediff is whitelisted as always treatable)
                            if (!patientTreatableHediffs.First().def.isBad && !AlwaysTreatableHediffs.Contains(patientTreatableHediffs.First().def) && !NonCriticalTreatableHediffs.Contains(patientTreatableHediffs.First().def))
                            {
                                patientTreatableHediffs.First().Tended(1, 1);
                            }
                            else
                            {                                
                                PatientPawn.health.hediffSet.hediffs.Remove(patientTreatableHediffs.First());
                            }

                            patientTreatableHediffs.RemoveAt(0);
                            if (!patientTreatableHediffs.NullOrEmpty())
                            {
                                // Scale healing time for current hediff according to its (normalized) severity and patient body size
                                // i.e. More severe hediffs take longer, bigger pawns also take longer
                                HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * PatientBodySizeScaledMaxHealingTicks);

                                status = MedPodStatus.HealingStarted;
                            }
                            else
                            {
                                SwitchState();
                            }
                            break;

                        case MedPodStatus.PatientDischarged:
                            WakePatient(PatientPawn);
                            SwitchState();
                            ProgressHealingTicks = 0;
                            TotalHealingTicks = 0;
                            break;
                    }
                }
                else
                {
                    status = MedPodStatus.Idle;
                    ProgressHealingTicks = 0;
                    TotalHealingTicks = 0;
                    Aborted = false;
                }
            }

            if (this.IsHashIntervalTick(2))
            {
                if (GantryMoving())
                {
                    if (gantryDirectionForwards)
                    {
                        gantryPositionPercentInt++;

                        if (gantryPositionPercentInt == 100)
                        {
                            gantryDirectionForwards = false;
                        }
                    }
                    else
                    {
                        gantryPositionPercentInt--;

                        if (gantryPositionPercentInt == 0)
                        {
                            gantryDirectionForwards = true;
                        }
                    }

                    if (wickSustainer == null)
                    {
                        StartWickSustainer();
                    }
                    else if (wickSustainer.Ended)
                    {
                        StartWickSustainer();
                    }
                    else
                    {
                        wickSustainer.Maintain();
                    }
                }
                else
                {
                    // Reset gantry
                    gantryPositionPercentInt = 0;
                    gantryDirectionForwards = true;
                }
            }

            if (DiagnosingTicks > 0)
            {
                DiagnosingTicks--;
                powerComp.PowerOutput = -DiagnosingPowerConsumption;
                if (PatientPawn != null)
                {
                    // Suspend patient food need level
                    if (PatientPawn.needs.food != null)
                    {
                        PatientPawn.needs.food.CurLevelPercentage = 1f;
                    }
                    
                    // Suspend patient DBH thirst, bladder and hygiene need levels
                    if (ModCompatibility.DbhIsActive)
                    {
                        ModCompatibility.SetThirstNeedCurLevelPercentage(PatientPawn, 1f);
                        ModCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                        ModCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                    }
                }

                if (DiagnosingTicks == 0)
                {
                    SwitchState();
                }
            }

            if (HealingTicks > 0)
            {
                HealingTicks--;
                ProgressHealingTicks++;
                powerComp.PowerOutput = -HealingPowerConsumption;
                if (PatientPawn != null)
                {
                    // Suspend patient food need level
                    if (PatientPawn.needs.food != null)
                    {
                        PatientPawn.needs.food.CurLevelPercentage = 1f;
                    }

                    // Suspend patient DBH thirst, bladder and hygiene need levels
                    if (ModCompatibility.DbhIsActive)
                    {
                        ModCompatibility.SetThirstNeedCurLevelPercentage(PatientPawn, 1f);
                        ModCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                        ModCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                    }
                }

                if (HealingTicks == 0)
                {
                    SwitchState();
                }
            }
        }
    }
}