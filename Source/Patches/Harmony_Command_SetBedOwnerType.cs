using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod
{
    // Disallow Guests from using MedPods set for Prisoners or Slaves (Ideology DLC uses different bed owner type gizmo compared with vanilla)
    [HarmonyPatch(typeof(Command_SetBedOwnerType), nameof(Command_SetBedOwnerType.ProcessInput))]
    public static class Harmony_Command_SetBedOwnerType_ProcessInput_DisableGuestModeIfNonColonistMedPod
    {
        static void Postfix(Command_SetBedOwnerType __instance)
        {
            if (__instance.bed is Building_BedMedPod bedMedPod && bedMedPod.ForOwnerType != BedOwnerType.Colonist) 
            {
                bedMedPod.allowGuests = false;
            }
        }
    }
}
