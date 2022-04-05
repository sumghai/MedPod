using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace MedPod.Patches
{
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
                __result = !bedMedPod.powerComp.PowerOn || bedMedPod.IsForbidden(___toil.actor) || !MedPodHealthAIUtility.ShouldSeekMedPodRest(___toil.actor, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs, bedMedPod.NonCriticalTreatableHediffs) || bedMedPod.Aborted;
                return false; // Skip original code
            }
            return true; // Run original code
        }
    }
}
