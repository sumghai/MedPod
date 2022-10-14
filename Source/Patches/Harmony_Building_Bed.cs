using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod
{
    // Manually force the MedPod beds to only have one sleeping slot located in the center cell of the nominally 3x3 furniture, as by default RimWorld will assume a 3x3 "bed" should have three slots positioned in the top row cells
    [HarmonyPatch(typeof(Building_Bed), nameof(Building_BedMedPod.SleepingSlotsCount), MethodType.Getter)]
    public static class BuildingBed_SleepingSlotsCount
    {
        public static void Postfix(ref int __result, ref Building_Bed __instance)
        {
            if (__instance.def.thingClass == typeof(Building_BedMedPod))
            {
                __result = 1;
            }
        }
    }

    [HarmonyPatch(typeof(Building_Bed), nameof(Building_BedMedPod.GetSleepingSlotPos))]
    public static class BuildingBed_GetSleepingSlotPos
    {
        public static void Postfix(ref IntVec3 __result, ref Building_Bed __instance)
        {
            if (__instance.def.thingClass == typeof(Building_BedMedPod))
            {
                __result = __instance.Position;
            }
        }
    }
}
