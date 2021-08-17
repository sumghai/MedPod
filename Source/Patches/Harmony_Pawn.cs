using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MedPod.Patches
{
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
}
