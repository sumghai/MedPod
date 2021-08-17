using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod.Patches
{
    // Exclude our MedPod beds from normal checks for valid beds, so that regular vanilla bed rest WorkGivers never use them
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
    static class RestUtility_IsValidBedFor_IgnoreMedPods
    {
        static bool Prefix(ref bool __result, Thing bedThing)
        {
            if (bedThing is Building_BedMedPod)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

}