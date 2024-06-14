using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod
{
    // Manually force the MedPod beds to only have one sleeping slot located in the center cell of the nominally 3x3 furniture, as by default RimWorld will assume a 3x3 "bed" should have three slots positioned in the top row cells
    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.SleepingSlotsCount), MethodType.Getter)]
    public static class Harmony_Building_Bed_SleepingSlotsCount_OnlyOneSlotOnMedPods
    {
        public static void Postfix(ref int __result, ref Building_Bed __instance)
        {
            if (__instance.def.thingClass == typeof(Building_BedMedPod))
            {
                __result = 1;
            }
        }
    }

    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetSleepingSlotPos))]
    public static class Harmony_Building_Bed_GetSleepingSlotPos_SetSleepingPositionForMedPod
    {
        public static void Postfix(ref IntVec3 __result, ref Building_Bed __instance)
        {
            if (__instance.def.thingClass == typeof(Building_BedMedPod))
            {
                __result = __instance.Position;
            }
        }
    }

    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetFootSlotPos))]
    public static class Harmony_Building_Bed_GetFootSlotPos_ForMedPod
    {
        public static void Postfix(ref IntVec3 __result, ref Building_Bed __instance)
        {
            if (__instance.def.thingClass == typeof(Building_BedMedPod))
            {
                __result = __instance.Position + __instance.Rotation.FacingCell;
            }
        }
    }

    // Disallow Guests from using MedPods set for Prisoners (vanilla)
    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.SetBedOwnerTypeByInterface))]
    public static class Harmony_Building_Bed_SetBedOwnerTypeByInterface_DisableGuestModeIfNonColonistMedPod
    {
        public static void Postfix(ref Building_Bed __instance, BedOwnerType ownerType)
        {
            if (__instance is Building_BedMedPod bedMedPod && ownerType != BedOwnerType.Colonist)
            {
                bedMedPod.allowGuests = false;
            }
        }
    }
}
