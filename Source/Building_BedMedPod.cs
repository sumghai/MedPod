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

        private float totalNormalizedSeverities = 0;

        public int DiagnosingTicks = 0;

        public int MaxDiagnosingTicks;

        public int HealingTicks = 0;

        public int MaxHealingTicks;

        public float DiagnosingPowerConsumption;

        public float HealingPowerConsumption;

        public List<HediffDef> AlwaysTreatableHediffs;

        public List<HediffDef> NeverTreatableHediffs;

        public List<string> DisallowedRaces;

        public int ProgressHealingTicks = 0;

        public int TotalHealingTicks = 0;

        public int gantryPositionPercentInt = 0;

        public bool gantryDirectionForwards = true;

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
            DisallowedRaces = treatmentRestrictions.DisallowedRaces;

            // Add a blocker region for the MedPod main machinery
            // (If one already exists, then we are probably loading a save with an existing MedPod)
            Thing something = Map.thingGrid.ThingsListAtFast(InvisibleBlockerPosition).FirstOrDefault(x => x.def.Equals(MedPodDef.MedPodInvisibleBlocker));

            if (something != null)
            {
                something.DeSpawn();
            }

            Thing t = ThingMaker.MakeThing(MedPodDef.MedPodInvisibleBlocker);
            GenPlace.TryPlaceThing(t, InvisibleBlockerPosition, Map, ThingPlaceMode.Direct, out resultingBlocker, null, null, Rotation);
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
            this.ForPrisoners = false;
            this.Medical = false;

            // Remove the blocker region
            resultingBlocker.DeSpawn();

            Room room = this.GetRoom(RegionType.Set_Passable);
            base.DeSpawn(mode);
            if (room != null)
            {
                room.Notify_RoomShapeOrContainedBedsChanged();
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
                    if (ForPrisoners)
                    {
                        stringBuilder.AppendInNewLine("ForPrisonerUse".Translate());
                    }
                    else
                    {
                        stringBuilder.AppendInNewLine("ForColonistUse".Translate());
                    }
                }

                if (!powerComp.PowerOn)
                {
                    inspectorStatus = "Error: No power";
                }
                else
                {
                    switch (status)
                    {
                        case MedPodStatus.DiagnosisStarted:
                            float diagnosingProgress = (float)(MaxDiagnosingTicks - DiagnosingTicks) / MaxDiagnosingTicks * 100;
                            inspectorStatus = "Diagnosing (" + (int)diagnosingProgress + "%)";
                            break;
                        case MedPodStatus.DiagnosisFinished:
                            inspectorStatus = "Diagnosis complete";
                            break;
                        case MedPodStatus.HealingStarted:
                        case MedPodStatus.HealingFinished:
                            float healingProgress = (float)ProgressHealingTicks / TotalHealingTicks * 100;
                            inspectorStatus = "Reatomizing (" + (int)healingProgress + "%)";
                            break;
                        case MedPodStatus.PatientDischarged:
                            inspectorStatus = "100% Clear";
                            break;
                        case MedPodStatus.Idle:
                        default:
                            inspectorStatus = "Idle";
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
                if (!MedPodHealthAIUtility.ShouldPawnSeekMedPod(myPawn))
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "NotInjured".Translate() + ")", null);
                    yield break;
                }
                if (MedPodHealthAIUtility.ShouldPawnSeekMedPod(myPawn) && !powerComp.PowerOn)
                {
                    yield return new FloatMenuOption("UseMedicalBed".Translate() + " (" + "MedPod_FloatMenu_Unpowered".Translate() + ")", null);
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
            foreach (Gizmo g in base.GetGizmos())
            {
                if (g is Command_Toggle act && (act.defaultLabel == medicalToggleStr))
                {
                    continue; // Hide the Medical bed toggle, as MedPods are always Medical beds
                }
                yield return g;
            }
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
            List<Hediff> artificialPartHediffs = patientTreatableHediffs.FindAll((Hediff x) => x.def.hediffClass.Equals(typeof(Hediff_AddedPart)) || x.def.hediffClass.Equals(typeof(Hediff_Implant)));

            List<BodyPartRecord> childPartsToSkip = new List<BodyPartRecord>();

            foreach (Hediff currentArtificialPartHediff in artificialPartHediffs)
            {
                childPartsToSkip.AddRange(GetBodyPartDescendants(currentArtificialPartHediff.part));
            }

            patientTreatableHediffs.RemoveAll((Hediff x) => childPartsToSkip.Any(p => x.part == p));

            // Ignore hediffs/injuries that are:
            // - Not explicitly whitelisted as always treatable
            // - Blacklisted as never treatable
            // - Not bad (i.e isBad = false)
            patientTreatableHediffs.RemoveAll((Hediff x) => !AlwaysTreatableHediffs.Contains(x.def) && (NeverTreatableHediffs.Contains(x.def) || !x.def.isBad));

            // Induce coma in the patient so that they don't run off during treatment
            // (Pawns tend to get up as soon as they are "no longer incapable of walking")
            AnesthesizePatient(patientPawn);

            // Calculate individual and total cumulative treatment time for each hediff/injury
            foreach (Hediff currentHediff in patientTreatableHediffs)
            {
                float currentSeverity = currentHediff.Severity;

                // currentHediff.Part will throw an error if a hediff is applied to the whole body (e.g. malnutrition), as part == null
                float currentBodyPartMaxHealth = (currentHediff.Part != null) ? currentHediff.Part.def.GetMaxHealth(patientPawn) : 1;

                float currentNormalizedSeverity = (currentSeverity < 1) ? currentSeverity : currentSeverity / currentBodyPartMaxHealth;

                totalNormalizedSeverities += currentNormalizedSeverity;

                TotalHealingTicks += (int)Math.Ceiling(GetHediffNormalizedSeverity(currentHediff) * MaxHealingTicks);
            }
        }

        private float GetHediffNormalizedSeverity(Hediff specificHediff = null)
        {
            Hediff currentHediff = (specificHediff == null) ? patientTreatableHediffs.First() : specificHediff;

            float currentHediffSeverity = currentHediff.Severity;

            float currentHediffBodyPartMaxHealth = (currentHediff.Part != null) ? currentHediff.Part.def.GetMaxHealth(PatientPawn) : 1;

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

        private void AnesthesizePatient(Pawn patientPawn)
        {
            Hediff inducedComa = HediffMaker.MakeHediff(HediffDef.Named("MedPod_InducedComa"), patientPawn);
            patientPawn.health.AddHediff(inducedComa);
        }

        private void WakePatient(Pawn patientPawn, bool wakeNormally = true)
        {
            patientPawn.health.hediffSet.hediffs.RemoveAll((Hediff x) => x.def.defName == "MedPod_InducedComa");

            string corticalStimulationType = wakeNormally ? "MedPod_CorticalStimulation" : "MedPod_CorticalStimulationImproper";
            string popupMessage = wakeNormally ? "MedPod_Message_TreatmentComplete".Translate(patientPawn.LabelCap, patientPawn) : "MedPod_Message_TreatmentInterrupted".Translate(patientPawn.LabelCap, patientPawn);
            MessageTypeDef popupMessageType = wakeNormally ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeHealthEvent;

            Messages.Message(popupMessage, patientPawn, popupMessageType, true);

            Hediff corticalStimulation = HediffMaker.MakeHediff(HediffDef.Named(corticalStimulationType), patientPawn);
            patientPawn.health.AddHediff(corticalStimulation);
        }

        public void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            wickSustainer = def.building.soundDispense.TrySpawnSustainer(info);
        }

        public override void Draw()
        {
            base.Draw();

            if (powerComp.PowerOn && (Rotation == Rot4.South))
            {
                Graphic screenGlow = GraphicDatabase.Get<Graphic_Single>("FX/MedPod_screenGlow_south", ShaderDatabase.MoteGlow, new Vector2(4f, 5f), Color.white);
                Mesh screenGlowMesh = screenGlow.MeshAt(Rotation);
                Vector3 screenGlowDrawPos = DrawPos;
                screenGlowDrawPos.y = AltitudeLayer.Building.AltitudeFor() + 0.03f;

                Graphics.DrawMesh(screenGlowMesh, screenGlowDrawPos, Quaternion.identity, FadedMaterialPool.FadedVersionOf(screenGlow.MatAt(Rotation, null), 1), 0);
            }
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

            powerComp.PowerOutput = -125f;

            if (this.IsHashIntervalTick(60))
            {

                if (PatientPawn != null)
                {
                    switch (status)
                    {
                        case MedPodStatus.Idle:
                            DiagnosingTicks = MaxDiagnosingTicks;
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
                                // Scale healing time for current hediff according to its (normalized) severity
                                // i.e. More severe hediffs take longer
                                HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * MaxHealingTicks);
                                SwitchState();
                            }
                            break;

                        case MedPodStatus.HealingFinished:
                            PatientPawn.health.hediffSet.hediffs.Remove(patientTreatableHediffs.First());
                            patientTreatableHediffs.RemoveAt(0);
                            if (!patientTreatableHediffs.NullOrEmpty())
                            {
                                // Scale healing time for current hediff according to its (normalized) severity
                                // i.e. More severe hediffs take longer
                                HealingTicks = (int)Math.Ceiling(GetHediffNormalizedSeverity() * MaxHealingTicks);
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

                if (HealingTicks == 0)
                {
                    SwitchState();
                }
            }
        }
    }
}