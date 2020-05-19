using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
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

        /* WIP
         * This patch is supposed to allow pawns to use the MedPod if they only have missing body parts
         * (and no other hediffs, tendable or otherwise).
         * 
         * FailOnBedNoLongerUsable() is called by the vanilla Toils_LayDown.LayDown() and Toils_Bed.GotoBed()
         * to check whether a pawn can go to or stay in a bed based on various conditions
         */
        [HarmonyPatch(typeof(Toils_Bed), "FailOnBedNoLongerUsable")]
        static class Toils_Bed_FailOnBedNoLongerUsable_CheckForMedPod
        {
            public static void Prefix(Toil toil, TargetIndex bedIndex)
            {
                Log.Warning("MedPod :: Prefixing Toils_Bed.FailOnBedNoLongerUsable()!");

                toil.FailOn(() => !MedPodHealthAIUtility.ShouldPawnSeekMedPod(toil.actor) && ((Building_Bed)toil.actor.CurJob.GetTarget(bedIndex).Thing).Medical && (Building_BedMedPod)toil.actor.CurJob.GetTarget(bedIndex).Thing is Building_BedMedPod);
            }
        }
    }
}