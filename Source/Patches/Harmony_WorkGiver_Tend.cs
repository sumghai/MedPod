using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod.Patches
{
    // Prevent Doctors/Wardens from tending patients if:
    // - The patient is lying on a MedPod
    // - The MedPod is powered
    // (as they would get smacked in the face by the MedPod's moving gantry)
    [HarmonyPatch(typeof(WorkGiver_Tend), nameof(WorkGiver_Tend.HasJobOnThing))]
    static class WorkGiver_Tend_HasJobOnThing_IgnoreMedPods
    {
        static void Postfix(ref bool __result, Thing t)
        {
            Pawn patient = t as Pawn;
            if (patient.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
            {
                __result = false;
            }
        }
    }
}
