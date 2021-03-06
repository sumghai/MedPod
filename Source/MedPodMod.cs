﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class MedPodMod : Mod
    {
        public MedPodMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.MedPod.patches");
            harmony.PatchAll();
        }

        // Manually force the MedPod beds to only have one sleeping slot located in the center cell of the nominally 3x3 furniture, as by default RimWorld will assume a 3x3 "bed" should have three slots positioned in the top row cells
        [HarmonyPatch(typeof(Building_Bed), nameof(Building_BedMedPod.SleepingSlotsCount), MethodType.Getter)]
        static class BuildingBed_SleepingSlotsCount
        {
            static void Postfix(ref int __result, ref Building_Bed __instance)
            {
                if (__instance.def.thingClass == typeof(Building_BedMedPod))
                {
                    __result = 1;
                }
            }
        }

        [HarmonyPatch(typeof(Building_Bed), nameof(Building_BedMedPod.GetSleepingSlotPos))]
        static class BuildingBed_GetSleepingSlotPos
        {
            static void Postfix(ref IntVec3 __result, ref Building_Bed __instance, int index)
            {
                if (__instance.def.thingClass == typeof(Building_BedMedPod))
                {
                    __result = __instance.Position;
                }
            }
        }

        // Prevent Doctors/Wardens from feeding patients if:
        // - The patient is lying on a MedPod
        // - The MedPod is powered
        // (as they would get smacked in the face by the MedPod's moving gantry)
        [HarmonyPatch]
        static class ShouldBeFed_IgnoreMedPods
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return AccessTools.Method(typeof(FeedPatientUtility), "ShouldBeFed");
                yield return AccessTools.Method(typeof(WardenFeedUtility), "ShouldBeFed");
            }

            static void Postfix(ref bool __result, Pawn p)
            {
                if (p.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
                {
                    __result = false;
                }
            }
        }

        // Prevent Doctors/Wardens from tending patients if:
        // - The patient is lying on a MedPod
        // - The MedPod is powered
        // (as they would get smacked in the face by the MedPod's moving gantry)
        [HarmonyPatch(typeof(WorkGiver_Tend), nameof(WorkGiver_Tend.HasJobOnThing))]
        static class WorkGiver_Tend_HasJobOnThing_IgnoreMedPods
        {
            static void Postfix(ref bool __result, Pawn pawn, Thing t, bool forced = false)
            {
                Pawn patient = t as Pawn;
                if (patient.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
                {
                    __result = false;
                }
            }
        }

        // Modify the vanilla fail conditions in Toils_Bed.FailOnBedNoLongerUsable() to use different logic
        // for MedPod beds by identifying a hidden inner predicate class and method, then patching it to
        // check whether the patient's target bed is a MedPod before deciding whether to apply our own
        // fail condition
        [HarmonyPatch]
        static class Toils_Bed_FailOnBedNoLongerUsable_CustomFailConditionForMedPods
        {
            static Type predicateClass;

            // This targets the following line from FailOnBedNoLongerUsable():
            // 
            // toil.FailOn(() => !HealthAIUtility.ShouldSeekMedicalRest(toil.actor) && !HealthAIUtility.ShouldSeekMedicalRestUrgent(toil.actor) && ((Building_Bed)toil.actor.CurJob.GetTarget(bedIndex).Thing).Medical);
            static MethodBase TargetMethod()
            {
                predicateClass = typeof(Toils_Bed).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("c__DisplayClass3_0"));
                if (predicateClass == null)
                {
                    Log.Error("MedPod :: Could not find Toils_Bed:c__DisplayClass3_0");
                    return null;
                }

                var m = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("<FailOnBedNoLongerUsable>b__2"));

                if (m == null)
                {
                    Log.Error("MedPod :: Could not find Toils_Bed:c__DisplayClass3_0<FailOnBedNoLongerUsable>b__2");
                }
                return m;
            }

            static bool Prefix(ref bool __result, Toil ___toil, TargetIndex ___bedIndex)
            {
                // Add MedPod-specific fail conditions if the target bed is a MedPod
                if (___toil.actor.CurJob.GetTarget(___bedIndex).Thing is Building_BedMedPod bedMedPod)
                {
                    // - If the MedPod has no power OR
                    // - If the MedPod is forbidden
                    // - If the pawn does not need to use the MedPod OR 
                    // - If the treatment cycle was aborted
                    __result = !bedMedPod.powerComp.PowerOn || bedMedPod.IsForbidden(___toil.actor) || !MedPodHealthAIUtility.ShouldPawnSeekMedPod(___toil.actor, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs) || bedMedPod.Aborted;
                    return false; // Skip original code
                }
                return true; // Run original code
            }
        }

        // Make sure patient stays in MedPod long enough to diagnosis to be run, and to get up once treatment is complete
        [HarmonyPatch]
        static class Toils_LayDown_LayDown_StayLyingInMedPod
        {
            static Type predicateClass;

            // This targets the layDown.tickAction delegate function from Toils_LayDown.LayDown()
            static MethodBase TargetMethod()
            {
                predicateClass = typeof(Toils_LayDown).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("c__DisplayClass2_0"));
                if (predicateClass == null)
                {
                    Log.Error("MedPod :: Could not find Toils_LayDown:c__DisplayClass2_0");
                    return null;
                }

                var m = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("<LayDown>b__1"));

                if (m == null)
                {
                    Log.Error("MedPod :: Could not find Toils_LayDown:c__DisplayClass2_0<LayDown>b__1");
                }

                return m;
            }

            static bool Prefix(Toil ___layDown, TargetIndex ___bedOrRestSpotIndex)
            {
                Pawn patientPawn = ___layDown.actor;
                Job curJob = patientPawn.CurJob;
                JobDriver curDriver = patientPawn.jobs.curDriver;
                Building_Bed building_Bed = (Building_Bed)curJob.GetTarget(___bedOrRestSpotIndex).Thing;
                patientPawn.GainComfortFromCellIfPossible();

                if (building_Bed is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn && bedMedPod.IsForbidden(patientPawn) && MedPodHealthAIUtility.ShouldPawnSeekMedPod(patientPawn, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs) && !bedMedPod.Aborted)
                {
                    // Keep pawn asleep in MedPod as long as they need to use it
                    curDriver.asleep = true;

                    // Fulfil pawn's rest need while they are asleep in MedPod if they're not rest immune (e.g. Circadian Half-Cycler)
                    if (patientPawn.needs.rest != null)
                    {
                        float restEffectiveness = !building_Bed.def.statBases.StatListContains(StatDefOf.BedRestEffectiveness) ? StatDefOf.BedRestEffectiveness.valueIfMissing : building_Bed.GetStatValue(StatDefOf.BedRestEffectiveness);
                        patientPawn.needs.rest.TickResting(restEffectiveness);
                    }
                    return false; // Skip original code
                }
                return true;  // Run original code
            }
        }

        // Make sure patients only use MedPods if:
        // - The MedPod has power
        // - The MedPod is not forbidden
        // - The patient has a valid need to use a MedPod (i.e. MedPodHealthAIUtility.ShouldPawnSeekMedPod() returns true), which excludes surgeries
        // - The patient does not have specific hediffs that prohibit them from using MedPods at all
        // - The patient is not in the disallowedRaces blacklist
        // - The treatment cycle hasn't been aborted
        [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
        static class RestUtility_IsValidBedFor_MedPodRestrictions
        {
            static void Postfix(ref bool __result, Thing bedThing, Pawn sleeper)
            {
                if (bedThing is Building_BedMedPod bedMedPod && (!bedMedPod.powerComp.PowerOn || bedMedPod.IsForbidden(sleeper) || !MedPodHealthAIUtility.ShouldPawnSeekMedPod(sleeper, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs) || MedPodHealthAIUtility.HasUsageBlockingHediffs(sleeper, bedMedPod.UsageBlockingHediffs) || !MedPodHealthAIUtility.IsValidRaceForMedPod(sleeper, bedMedPod.DisallowedRaces) || bedMedPod.Aborted))
                {
                    __result = false;
                }
            }
        }

        // Doctors should not perform scheduled surgeries on patients using MedPods
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.CurrentlyUsableForBills))]
        static class Pawn_CurrentlyUsableForBills_IgnoreSurgeryForPatientsOnMedPods
        {
            static void Postfix(ref bool __result, Pawn __instance)
            {
                if (__instance.InBed() && __instance.CurrentBed() is Building_BedMedPod bedMedPod)
                {
                    JobFailReason.Is("MedPod_SurgeryProhibited_PatientUsingMedPod".Translate());
                    __result = false;
                }
            }
        }

        // Humanoid patients should always lie on their backs when using MedPods,
        // while non-humanoid (animal) pawns should always lie on their sides
        [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing))]
        static class PawnRenderer_LayingFacing_AlwaysLieOnBackForMedPods
        {
            static void Postfix(ref Rot4 __result, Pawn ___pawn)
            {
                if (___pawn.CurrentBed() is Building_BedMedPod bedMedPod)
                {
                    if (___pawn.RaceProps.Humanlike)
                    {
                        __result = Rot4.South;
                    }
                    else
                    {
                        __result = (bedMedPod.Rotation == Rot4.West) ? Rot4.East : Rot4.West;
                    }
                }
            }
        }

        // Non-humanoid (animal) pawns lying in MedPods shouldn't be offset at a random angle
        [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle))]
        static class PawnRenderer_BodyAngle_NonHumanoidPawnPreventRandomBodyAngle
        {
            static void Postfix(ref float __result, Pawn ___pawn)
            {
                if (___pawn.CurrentBed() is Building_BedMedPod bedMedPod && !___pawn.RaceProps.Humanlike && !bedMedPod.def.building.bed_humanlike )
                {
                    Rot4 rotation = bedMedPod.Rotation;
                    if (rotation == Rot4.North)
                    {
                        __result = -90f;
                        return;
                    }
                    if (rotation == Rot4.South)
                    {
                        __result = 90f;
                        return;
                    }
                    __result = 0f;
                }
            }
        }

        // Patients should prioritize using MedPods over other beds for medical needs
        [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.FindPatientBedFor))]
        static class RestUtility_FindPatientBedFor_PrioritizeMedPodsForMedicalBeds
        {
            static bool Prefix(ref Building_Bed __result, Pawn pawn)
            {
                Building_Bed currentBed = pawn.CurrentBed();
                if (pawn.InBed() && (currentBed != null) && currentBed.Medical && ((pawn.RaceProps.Humanlike && 
                    currentBed.def.building.bed_humanlike) || (!pawn.RaceProps.Humanlike && !currentBed.def.building.bed_humanlike)) )
                {
                    __result = currentBed;
                    return false;
                }
                for (int i = 0; i < 2; i++)
                {
                    Danger maxDanger = (i == 0) ? Danger.None : Danger.Deadly;
                    Building_Bed building_Bed = (Building_BedMedPod)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Bed), PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, (Thing b) => (int)b.Position.GetDangerFor(pawn, pawn.Map) <= (int)maxDanger && b is Building_BedMedPod && RestUtility.IsValidBedFor(b, pawn, pawn, pawn.IsPrisoner, checkSocialProperness: false, allowMedBedEvenIfSetToNoCare: true));
                    if (building_Bed != null)
                    {
                        __result = building_Bed;
                        return false;
                    }
                }

                return true;
            }
        }

        // Remove induced coma hediff and wake patient if they are accidentally kicked off a MedPod
        // This handles an edge case where the player prioritizes Patient B to use a MedPod while it is already treating Patient A
        [HarmonyPatch(typeof(JobDriver_WaitDowned), nameof(JobDriver_WaitDowned.DecorateWaitToil))]
        static class JobDriver_WaitDowned_DecorateWaitToil_WakePatientIfKickedOffMedPod
        {
            static void Prefix(Pawn ___pawn)
            {
                if (___pawn.health.hediffSet.hediffs.Any((Hediff x) => x.def.defName == "MedPod_InducedComa"))
                {
                    Building_BedMedPod.WakePatient(___pawn, false);
                }
            }
        }

    }
}