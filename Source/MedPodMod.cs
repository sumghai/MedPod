using HarmonyLib;
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
        // (as they would get smacked in the face by the MedPod's moving reatomizer gantry)
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
        // (as they would get smacked in the face by the MedPod's moving reatomizer gantry)
        [HarmonyPatch(typeof(WorkGiver_Tend), "HasJobOnThing")]
        static class WorkGiver_Tend_JobOnThing_IgnoreMedPods
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
                predicateClass = typeof(Toils_Bed).GetNestedTypes(AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass3_0"));
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
                if (___toil.actor.CurJob.GetTarget(___bedIndex).Thing is Building_BedMedPod bedMedPod) {
                    // Add MedPod-specific fail conditions
                    // - If the target bed is a MedPod AND
                    // - If the pawn does not need to use the MedPod OR the MedPod has no power
                    ___toil.FailOn(() => ((Building_Bed)___toil.actor.CurJob.GetTarget(___bedIndex).Thing is Building_BedMedPod) && (!bedMedPod.powerComp.PowerOn || !MedPodHealthAIUtility.ShouldPawnSeekMedPod(___toil.actor)));
                    return false; // Skip original code
                }

                return true; // Run original code
            }
        }

        // Make sure pawn stays in MedPod long enough to diagnosis to be run, and to get up once treatment is complete
        [HarmonyPatch]
        static class Toils_LayDown_LayDown_StayLyingInMedPod
        {
            static Type predicateClass;

            // This targets the layDown.tickAction delegate function from Toils_LayDown.LayDown()
            static MethodBase TargetMethod()
            {
                predicateClass = typeof(Toils_LayDown).GetNestedTypes(AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass2_0"));
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

                if (building_Bed is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn && MedPodHealthAIUtility.ShouldPawnSeekMedPod(patientPawn))
                {
                    // Keep pawn asleep in MedPod as long as they need to use it
                    curDriver.asleep = true;
                    return false; // Skip original code
                }

                return true;  // Run original code
            }
        }

        // Make sure pawns only use MedPods if:
        // - The MedPod has power
        // - The pawn has a valid need to use a MedPod (i.e. MedPodHealthAIUtility.ShouldPawnSeekMedPod() returns true), which excludes surgeries
        [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
        static class RestUtility_IsValidBedFor_MedPodRestrictions
        {
            static void Postfix(ref bool __result, Thing bedThing, Pawn sleeper)
            {
                if (bedThing is Building_BedMedPod bedMedPod && (!bedMedPod.powerComp.PowerOn || !MedPodHealthAIUtility.ShouldPawnSeekMedPod(sleeper)))
                {
                    __result = false;
                }
            }
        } 
    }
}