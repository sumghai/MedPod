using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MedPod
{
    public class MedPodMod : Mod
    {
        public static Settings Settings;

        public MedPodMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
            var harmony = new Harmony("com.MedPod.patches");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Settings.Draw(canvas);
            base.DoSettingsWindowContents(canvas);
        }

        public override string SettingsCategory()
        {
            return "MedPod_SettingsCategory_Heading".Translate();
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
    }
}