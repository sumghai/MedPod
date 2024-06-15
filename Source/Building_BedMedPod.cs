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

        [MayRequireBiotech]
        public List<XenotypeDef> DisallowedXenotypes;

        public int ProgressHealingTicks = 0;

        public int TotalHealingTicks = 0;

        public int gantryPositionPercentInt = 0;

        public bool gantryDirectionForwards = true;

        public bool allowGuests = false;

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

            MaxDiagnosingTicks = GenTicks.SecondsToTicks(medpodSettings.Props.maxDiagnosisTime);
            MaxHealingTicks = GenTicks.SecondsToTicks(medpodSettings.Props.maxPerHediffHealingTime);
            DiagnosingPowerConsumption = medpodSettings.Props.diagnosisModePowerConsumption;
            HealingPowerConsumption = medpodSettings.Props.healingModePowerConsumption;

            AlwaysTreatableHediffs = treatmentRestrictions.Props.alwaysTreatableHediffs;
            NeverTreatableHediffs = treatmentRestrictions.Props.neverTreatableHediffs;
            NonCriticalTreatableHediffs = treatmentRestrictions.Props.nonCriticalTreatableHediffs;
            UsageBlockingHediffs = treatmentRestrictions.Props.usageBlockingHediffs;
            UsageBlockingTraits = treatmentRestrictions.Props.usageBlockingTraits;
            AlwaysTreatableTraits = treatmentRestrictions.Props.alwaysTreatableTraits;
            DisallowedRaces = treatmentRestrictions.Props.disallowedRaces;
            DisallowedXenotypes = treatmentRestrictions.Props.disallowedXenotypes;

            // Add a blocker region for the MedPod main machinery, if required
            if (!medpodSettings.Props.disableInvisibleBlocker)
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
                DischargePatient(PatientPawn, false);
            }
            ForOwnerType = BedOwnerType.Colonist; 
            Medical = false;
            allowGuests = false;

            // Remove the blocker region, if required
            if (!medpodSettings.Props.disableInvisibleBlocker)
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowGuests, "allowGuests", false);
            BackCompatibility.PostExposeData(this);
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
                if (MedPodHealthAIUtility.HasUsageBlockingHediffs(myPawn, UsageBlockingHediffs))
                {
                    List<Hediff> blockedHediffs = new();
                    myPawn.health.hediffSet.GetHediffs(ref blockedHediffs);

                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_PatientWithHediffNotAllowed".Translate(blockedHediffs.First(h => UsageBlockingHediffs.Contains(h.def)).LabelCap) + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.HasUsageBlockingTraits(myPawn, UsageBlockingTraits))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " blocking traits (" + "MedPod_FloatMenu_PatientWithTraitNotAllowed".Translate(myPawn.story?.traits.allTraits.First(t => UsageBlockingTraits.Contains(t.def)).LabelCap) + ")", null);
                    yield break;
                }
                if (!MedPodHealthAIUtility.IsValidXenotypeForMedPod(myPawn, DisallowedXenotypes))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_RaceNotAllowed".Translate(myPawn.genes.xenotype.label.CapitalizeFirst()) + ")", null);
                    yield break;
                }
                if (!MedPodHealthAIUtility.IsValidRaceForMedPod(myPawn, DisallowedRaces))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_RaceNotAllowed".Translate(myPawn.def.label.CapitalizeFirst()) + ")", null);
                    yield break;
                }
                if (!MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, this))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "NotInjured".Translate() + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, this) && !powerComp.PowerOn)
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_Unpowered".Translate() + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, this) && this.IsForbidden(myPawn))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "ForbiddenLower".Translate() + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.ShouldSeekMedPodRest(myPawn, this) && !MedPodHealthAIUtility.HasAllowedMedicalCareCategory(myPawn))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_MedicalCareCategoryTooLow".Translate() + ")", null);
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

            // Allow guests gizmo - only available on MedPods for humanlike colonists
            if (def.building.bed_humanlike && ForColonists)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "MedPod_CommandGizmoAllowGuests_Label".Translate(),
                    defaultDesc = "MedPod_CommandGizmoAllowGuests_Desc".Translate(this.LabelCap),
                    isActive = () => allowGuests,
                    toggleAction = delegate
                    {
                        allowGuests = !allowGuests;
                    },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/MedPod_AllowGuests", true),
                    activateSound = SoundDefOf.Tick_Tiny
                };
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
                        // Kick non-ambulatory patients off the MedPod when aborting MedPod treatment
                        RestUtility.KickOutOfBed(PatientPawn, this);
                        Aborted = true;
                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/AbortTreatment", true),
                activateSound = SoundDefOf.Click
            };
        }

        private void SwitchState()
        {
            MedPodStatus oldStatus = status;
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
            if (DebugSettings.godMode)
            {
                Log.Message(this + " :: state change from " + oldStatus.ToStringSafe().Colorize(Color.red) + " to " + status.ToStringSafe().Colorize(Color.red));
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

            // Immediately treat blood loss hediff
            patientTreatableHediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);
            PatientPawn.health.hediffSet.hediffs.RemoveAll(x => x.def == HediffDefOf.BloodLoss);

            // Calculate individual and total cumulative treatment time for each hediff/injury
            foreach (Hediff currentHediff in patientTreatableHediffs)
            {
                float currentSeverity = currentHediff.Severity;

                // currentHediff.Part will throw an error if a hediff is applied to the whole body (e.g. malnutrition), as part == null
                float currentBodyPartMaxHealth = (currentHediff.Part != null) ? EbfCompatibilityWrapper.GetMaxHealth(currentHediff.Part.def, patientPawn, currentHediff.Part) : 1;

                float currentNormalizedSeverity = (currentSeverity < 1) ? currentSeverity : currentSeverity / currentBodyPartMaxHealth;

                totalNormalizedSeverities += currentNormalizedSeverity;

                TotalHealingTicks += (int)Math.Ceiling(GetHediffNormalizedSeverity(currentHediff) * PatientBodySizeScaledMaxHealingTicks);

                // Tend all bleeding hediffs immediately so the pawn doesn't bleed out while on MedPod
                // The Hediff will be completely removed once the Medpod is done with the Healing process
                if (currentHediff.Bleeding)
                {
                    currentHediff.Tended(1,1);
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

        public void DischargePatient(Pawn patientPawn, bool finishTreatmentNormally = true)
        {
            // Clear any ongoing mental states (e.g. Manhunter)
            if (patientPawn.InMentalState)
            {
                patientPawn.MentalState.RecoverFromState();
            }

            // Apply the appropriate cortical stimulation hediff, depending on whether the treatment was completed or interrupted            
            string popupMessage = finishTreatmentNormally ? "MedPod_Message_TreatmentComplete".Translate(patientPawn.LabelCap, patientPawn) : "MedPod_Message_TreatmentInterrupted".Translate(patientPawn.LabelCap, patientPawn);
            MessageTypeDef popupMessageType = finishTreatmentNormally ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeHealthEvent;

            Messages.Message(popupMessage, patientPawn, popupMessageType, true);

            // Restore previously saved patient food need level
            if (patientPawn.needs.food != null)
            {
                patientPawn.needs.food.CurLevelPercentage = patientSavedFoodNeed;
            }

            // Restore previously saved patient DBH thirst and reset DBH bladder/hygiene need levels
            if (ModCompatibility.DbhIsActive)
            {
                DbhCompatibility.SetThirstNeedCurLevelPercentage(patientPawn, patientSavedDbhThirstNeed);
                DbhCompatibility.SetBladderNeedCurLevelPercentage(patientPawn, 1f);
                DbhCompatibility.SetHygieneNeedCurLevelPercentage(patientPawn, 1f);
            }

            // Remove treatable traits only if treatment was completed normally
            if (!patientTraitsToRemove.NullOrEmpty() && finishTreatmentNormally)
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

            // If the patient has the Plaguelust need from Mechanite Persona Traits, remove it
            if (ModCompatibility.MechanitePersonaTraitsIsActive)
            {
                patientPawn.needs.RemoveNeed(MedPodDef.MPT_Need_MechaniteFactory);
            }

            // If the patient is a Sanguophage, top up their Hemogen
            if (ModsConfig.BiotechActive && patientPawn.RaceProps.Humanlike)
            {
                Gene_Hemogen gene_Hemogen = patientPawn.genes?.GetFirstGeneOfType<Gene_Hemogen>();
                if (gene_Hemogen != null)
                {
                    gene_Hemogen.Value = gene_Hemogen.InitialResourceMax;
                }
            }

            // Refresh pawn renderer (especially important for Anomaly DLC not updating visuals from removed hediffs)
            patientPawn.drawer.renderer.SetAllGraphicsDirty();

            // Refreshed pawn disabled work tags (especially important for Anomaly DLC not updating work tags from removed hediffs)
            patientPawn.Notify_DisabledWorkTypesChanged();

            // Clear pawn hediff cache and try to get them off the MedPod
            patientPawn.health.hediffSet.DirtyCache();
            patientPawn.health.healthState = PawnHealthState.Mobile;
        }

        public void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            wickSustainer = def.building.soundDispense.TrySpawnSustainer(info);
        }

        public override void Tick()
        {
            base.Tick();

            // State-dependent power consumption
            if (status == MedPodStatus.DiagnosisStarted || status == MedPodStatus.DiagnosisFinished)
            {
                powerComp.PowerOutput = -DiagnosingPowerConsumption;
            }
            else if (status == MedPodStatus.HealingStarted || status == MedPodStatus.HealingFinished)
            {
                powerComp.PowerOutput = -HealingPowerConsumption;
            }
            else
            {
                powerComp.PowerOutput = -powerComp.Props.basePowerConsumption;
            }

            // Main patient treatment cycle logic
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
                            patientSavedDbhThirstNeed = DbhCompatibility.GetThirstNeedCurLevelPercentage(PatientPawn);
                            DbhCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                            DbhCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                        }
                        if (DebugSettings.godMode)
                        {
                            Log.Message("\t" + PatientPawn + " :: initial DiagnosingTicks = " + DiagnosingTicks); 
                        }
                        SwitchState();
                        break;

                    case MedPodStatus.DiagnosisStarted:
                        DiagnosingTicks--;
                        if (DiagnosingTicks == 0) 
                        { 
                            SwitchState();
                        }
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
                            // Scale healing time for the first hediff according to its (normalized) severity and patient body size
                            // i.e. More severe hediffs take longer, bigger pawns also take longer
                            HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * PatientBodySizeScaledMaxHealingTicks);
                            if (DebugSettings.godMode)
                            {
                                Log.Message("\t" + PatientPawn + " :: first hediff HealingTicks = " + HealingTicks + " (hediff count: " + patientTreatableHediffs.Count() + ")");
                            }
                            SwitchState();
                        }
                        break;

                    case MedPodStatus.HealingStarted:
                        HealingTicks--;
                        ProgressHealingTicks++;
                        if (HealingTicks == 0)
                        {
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
                            // Scale healing time for the next hediff according to its (normalized) severity and patient body size
                            // i.e. More severe hediffs take longer, bigger pawns also take longer
                            HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * PatientBodySizeScaledMaxHealingTicks);
                            if (DebugSettings.godMode)
                            {
                                Log.Message("\t" + PatientPawn + " :: next hediff HealingTicks = " + HealingTicks + " (hediff count: " + patientTreatableHediffs.Count() + ")");
                            }
                            // Jump back to the previous state to start healing the next hediff
                            status = MedPodStatus.HealingStarted;
                        }
                        else
                        {
                            SwitchState();
                        }
                        break;
                    
                    case MedPodStatus.PatientDischarged:
                        DischargePatient(PatientPawn);
                        SwitchState();
                        ProgressHealingTicks = 0;
                        TotalHealingTicks = 0;
                        break;
                }

                // Suspend patient needs during diagnosis and treatment
                if (status == MedPodStatus.DiagnosisStarted || status == MedPodStatus.DiagnosisFinished || status == MedPodStatus.HealingStarted || status == MedPodStatus.HealingFinished)
                {
                    // Food
                    if (PatientPawn.needs.food != null)
                    {
                        PatientPawn.needs.food.CurLevelPercentage = 1f;
                    }

                    // Dubs Bad Hygiene thirst, bladder and hygiene
                    if (ModCompatibility.DbhIsActive)
                    {
                        DbhCompatibility.SetThirstNeedCurLevelPercentage(PatientPawn, 1f);
                        DbhCompatibility.SetBladderNeedCurLevelPercentage(PatientPawn, 1f);
                        DbhCompatibility.SetHygieneNeedCurLevelPercentage(PatientPawn, 1f);
                    }
                }
            }
            else
            {
                status = MedPodStatus.Idle;
                ProgressHealingTicks = 0;
                TotalHealingTicks = 0;
                Aborted = false;
            }

            // Gantry animation
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
        }
    }
}