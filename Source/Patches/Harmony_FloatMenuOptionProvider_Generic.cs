using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace MedPod
{
    // Remove any arresting/carrying options for patients already lying on MedPods
    [HarmonyPatch]
    public static class Harmony_FloatMenuOptionProvider_Generic_GetSingleOptionFor_ClickedPawn_SkipForMedPod
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_Arrest), nameof(FloatMenuOptionProvider_Arrest.GetSingleOptionFor), new Type[] { typeof(Pawn), typeof(FloatMenuContext)});
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_CarryPawn), nameof(FloatMenuOptionProvider_CarryPawn.GetSingleOptionFor), new Type[] { typeof(Pawn), typeof(FloatMenuContext) });
        }

        static void Postfix(ref FloatMenuOption __result, Pawn clickedPawn)
        {
            if (clickedPawn.CurrentBed() is Building_BedMedPod) 
            {
                __result = null;
            }
        }
    }

    // Remove any stripping/tending options for patients already lying on MedPods
    [HarmonyPatch]
    public static class Harmony_FloatMenuOptionProvider_Generic_GetSingleOptionFor_ClickedThing_SkipForMedPod
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_Strip), nameof(FloatMenuOptionProvider_Strip.GetSingleOptionFor), new Type[] { typeof(Thing), typeof(FloatMenuContext) });
            yield return AccessTools.Method(typeof(FloatMenuOptionProvider_DraftedTend), nameof(FloatMenuOptionProvider_DraftedTend.GetSingleOptionFor), new Type[] { typeof(Thing), typeof(FloatMenuContext) });
        }

        static void Postfix(ref FloatMenuOption __result, Thing clickedThing)
        {
            // Extra check to make sure the clicked target is actually a pawn
            if (clickedThing is Pawn clickedPawn && clickedPawn.CurrentBed() is Building_BedMedPod)
            {
                __result = null;
            }
        }
    }
}
