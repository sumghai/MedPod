using HarmonyLib;
using System.Linq;
using Verse;

namespace MedPod.Patches
{
    // Band-aid fix to prevent unconscious pawns from dropping equipment when placed on MedPods
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    public static class Harmony_Pawn_EquipmentTracker_TryDropEquipment
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, IntVec3 pos)
        {
            Pawn pawn = __instance.pawn;
            if (pos.GetThingList(pawn.Map).Where(t => t.def.thingClass == typeof(Building_BedMedPod)) != null)
            {
                return false;
            }
            return true;
        }
    }
}
