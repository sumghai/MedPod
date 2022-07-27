using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MedPod.Patches
{
    // Adds a "Rescue [pawn] to MedPod" float menu option to pawns
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddHumanlikeOrders))]
    static class Harmony_FloatMenuMakerMap_OverrideRescueWithMedPodVersion
    {
        static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {                      
            // Only for pawns capable of manipulation
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                // Only if there is at least one player-owned MedPod on the pawn's current map
                if (pawn.Map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_BedMedPod))
                { 
                    // Loop through each rescuable victim on the cell the player has right-clicked on
                    foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                    {
                        Pawn victim = (Pawn)item.Thing;
                    
                        // Skip victims:
                        // - Who are already in beds
                        // - Who are unreachable
                        // - Who will automatically join the player when rescued
                        if (victim.InBed() || !pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) || victim.mindState.WillJoinColonyIfRescued)
                        {
                            continue;
                        }

                        // Allow victims:
                        // - Who are not in a mental state
                        // - Who are colonists with Scaria
                        // - Who belong to no faction
                        // - Who are not hostile to the player
                        if ((!victim.InMentalState || victim.health.hediffSet.HasHediff(HediffDefOf.Scaria)) && (victim.Faction == Faction.OfPlayer || victim.Faction == null || !victim.Faction.HostileTo(Faction.OfPlayer)))
                        {
                            // Insert the new MedPod rescue option just after the corresponding base game rescue option 
                            int insertIndex = opts.FindLastIndex((FloatMenuOption x) => x.Label.Contains("Rescue".Translate(victim.LabelCap, victim))) + 1;

                            opts.Insert(insertIndex, FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("MedPod_FloatMenu_Rescue".Translate(victim.LabelCap, victim), delegate
                            {
                                Building_Bed building_BedMedPod = MedPodRestUtility.FindBestMedPod(pawn, victim);
                            
                                // Display message on top screen if no valid MedPod is found
                                if (building_BedMedPod == null)
                                {
                                    string reason = (!victim.RaceProps.Animal) ? ((string)"MedPod_Message_CannotRescue_NoNonPrisonerMedPod".Translate()) : ((string)"MedPod_Message_CannotRescue_NoVetPod".Translate());
                                    Messages.Message("CannotRescue".Translate() + ": " + reason, victim, MessageTypeDefOf.RejectInput, historical: false);
                                }
                                // Assign a MedPod rescue job to the pawn
                                else
                                {
                                    Job job = JobMaker.MakeJob(MedPodDef.RescueToMedPod, victim, building_BedMedPod);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim));
                        }
                    }
                }
            }
        }
    }
}