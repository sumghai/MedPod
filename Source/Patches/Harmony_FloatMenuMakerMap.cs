using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MedPod.Patches
{
    // Replaces vanilla Rescue float menu option with one that prioritizes MedPods
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddHumanlikeOrders))]
    static class Harmony_FloatMenuMakerMap_OverrideRescueWithMedPodVersion
    {
        static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            Pawn pawn2 = GridsUtility.GetThingList(IntVec3.FromVector3(clickPos), pawn.Map)
                    .FirstOrDefault((Thing x) => x is Pawn) as Pawn;

            if (pawn2 != null)
            {
                TaggedString toCheck = "Rescue".Translate(pawn2.LabelCap, pawn2);
                FloatMenuOption floatMenuOption = opts.FirstOrDefault((FloatMenuOption x) => x.Label.Contains(toCheck));

                if (floatMenuOption != null)
                {
                    Log.Warning("Found " + floatMenuOption.Label);
                    opts.Remove(floatMenuOption);
                    opts.Add(AddRescueToMedPodOption(pawn, pawn2));
                }
            }
        }

        static FloatMenuOption AddRescueToMedPodOption(Pawn pawn, Pawn victim)
        {
            var floatOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate(victim.LabelCap, victim), delegate ()
            {
                Building_Bed bed = MedPodRestUtility.FindBestMedPod(pawn, victim);
                if (bed == null)
                {
                    bed = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true);
                }
                if (bed == null)
                {
                    string t4 = (!victim.RaceProps.Animal) ? ("NoNonPrisonerBed".Translate()) : ("NoAnimalBed".Translate());
                    Messages.Message("CannotRescue".Translate() + ": " + t4, victim, MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    JobDef jobDef = (bed.def.thingClass == typeof(Building_BedMedPod)) ? MedPodDef.RescueToMedPod : JobDefOf.Rescue;
                    Job job22 = JobMaker.MakeJob(jobDef, victim, bed);
                    job22.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job22, JobTag.Misc);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                }
            }, MenuOptionPriority.RescueOrCapture, null, victim, 0f, null, null), pawn, victim, "ReservedBy");
            return floatOption;
        }
    }
}
