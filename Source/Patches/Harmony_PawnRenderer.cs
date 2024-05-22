using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MedPod
{
    // Humanoid patients should always lie on their backs when using MedPods,
    // while non-humanoid (animal) pawns should always lie on their sides
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing))]
    public static class PawnRenderer_LayingFacing_AlwaysLieOnBackForMedPods
    {
        public static void Postfix(ref Rot4 __result, Pawn ___pawn)
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
    public static class PawnRenderer_BodyAngle_NonHumanoidPawnPreventRandomBodyAngle
    {
        public static void Postfix(ref float __result, Pawn ___pawn)
        {
            if (___pawn.CurrentBed() is Building_BedMedPod bedMedPod && !___pawn.RaceProps.Humanlike && !bedMedPod.def.building.bed_humanlike)
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

    // Offset position of animals resting in VetPods if optional draw offset data is provided in the MedPod Settings comp
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.GetBodyPos))]
    public static class PawnRenderer_GetBodyPos_OffsetLargeAnimalsInVetPods
    { 
        public static void Postfix(ref Vector3 __result, PawnRenderer __instance)
        {
            Pawn pawn = __instance.pawn;
            Building_Bed building_Bed = pawn.CurrentBed();

            if (pawn.GetPosture() == PawnPosture.LayingInBed && building_Bed != null && building_Bed is Building_BedMedPod bedMedPod)
            { 
                CompMedPodSettings medpodSettings = bedMedPod.GetComp<CompMedPodSettings>();
                var animalWithOffset = medpodSettings?.Props.animalRestingDrawOffsets?.Find(x => x.animalRace == pawn.def);

                if (animalWithOffset != null)
                {
                    switch (bedMedPod.Rotation.AsInt)
                    {
                        case Rot4.NorthInt:
                        case Rot4.SouthInt:
                            __result -= animalWithOffset.drawOffset.RotatedBy(bedMedPod.Rotation);
                            break;
                        default:    // East and West
                            __result -= animalWithOffset.drawOffset.RotatedBy(Rot4.East);
                            break;
                    }
                }
            }
        }
    }
}
