using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MedPod
{
    // Adds a "Rescue [pawn] to MedPod" float menu option to pawns,
    // and remove any arresting/carrying/stripping/tending options for patients already lying on MedPods
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddHumanlikeOrders))]
    public static class Harmony_FloatMenuMakerMap_OverrideRescueWithMedPodVersion
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {                      
            // Only for pawns capable of manipulation
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                // Only if there is at least one player-owned MedPod on the pawn's current map
                if (pawn.Map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_BedMedPod))
                {
                    // Get the first MedPod on the map
                    Building_BedMedPod building_BedMedPod = pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_BedMedPod>().First();
                    
                    // Loop through each rescuable victim on the cell the player has right-clicked on
                    foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                    {
                        Pawn victim = (Pawn)item.Thing;

                        // Skip victims:
                        // - Who are already in beds
                        // - Who are unreachable
                        // - Who will automatically join the player when rescued
                        // - Who have hediffs or traits that prevent them from using MedPods
                        // - Who are of races that can't use MedPods
                        // - Who are of xenotypes that can't use MedPods
                        if (victim.InBed() || 
                            !pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) || 
                            victim.mindState.WillJoinColonyIfRescued || 
                            MedPodHealthAIUtility.HasUsageBlockingHediffs(victim, building_BedMedPod.UsageBlockingHediffs) || 
                            MedPodHealthAIUtility.HasUsageBlockingTraits(victim, building_BedMedPod.UsageBlockingTraits) ||
                            MedPodHealthAIUtility.IsValidRaceForMedPod(victim, building_BedMedPod.DisallowedRaces) ||
                            MedPodHealthAIUtility.IsValidXenotypeForMedPod(victim, building_BedMedPod.DisallowedXenotypes))
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
                                Building_BedMedPod building_BedMedPod = MedPodRestUtility.FindBestMedPod(pawn, victim);
                            
                                // Display message on top screen if no valid MedPod is found
                                if (building_BedMedPod == null)
                                {
                                    string pawnType = victim.IsSlave ? "Slave".Translate() : victim.IsPrisoner ? "PrisonerLower".Translate() : "Colonist".Translate();
                                   
                                    string reason = (!victim.RaceProps.Animal) ? ((string)"MedPod_Message_CannotRescue_NoMedPod".Translate(pawnType.ToLower())) : ((string)"MedPod_Message_CannotRescue_NoVetPod".Translate());
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

                        // Combat Extended compatibility
                        // Loop through each pawn that needs stabilizing on the cell the player has right-clicked on, and disallow stabilizing pawns currently on MedPods
                        if (ModCompatibility.CombatExtendedIsActive && victim.CurrentBed() is Building_BedMedPod)
                        {
                            opts.RemoveAll(opt =>
                                opt.Label.Contains("CE_Stabilize".Translate(victim.LabelCap)) ||
                                opt.Label.Contains("CE_CannotStabilize".Translate())
                            );
                        }
                    }

                    // Loop through each pawn that needs arresting on the cell the player has right-clicked on, and disallow arresting pawns currently on MedPods
                    foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), thingsOnly: true))
                    {
                        Pawn arrestTarget = (Pawn)item.Thing;
                        if (arrestTarget.CurrentBed() is Building_BedMedPod)
                        {
                            opts.RemoveAll(opt =>
                                opt.Label.Contains("TryToArrest".Translate(arrestTarget.LabelCap, arrestTarget, arrestTarget.GetAcceptArrestChance(pawn).ToStringPercent()))
                            );
                        }
                    }

                    // Loop through each pawn that needs carrying on the cell the player has right-clicked on, and disallow carrying pawns currently on MedPods
                    foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForCarry(pawn), thingsOnly: true))
                    {
                        Pawn carryTarget = (Pawn)item.Thing;
                        if (carryTarget.CurrentBed() is Building_BedMedPod)
                        {
                            opts.RemoveAll(opt =>
                                opt.Label.Contains("Carry".Translate(carryTarget)) ||
                                opt.Label.Contains("CannotCarry".Translate(carryTarget))
                            );
                        }
                    }

                    // Loop through each pawn/corpse that needs stripping on the cell the player has right-clicked on, and disallow stripping pawns currently on MedPods
                    foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true))
                    {
                        Thing stripTarget = item.Thing;
                        if (stripTarget is Pawn stripTargetPawn && stripTargetPawn.CurrentBed() is Building_BedMedPod)
                        {
                            opts.RemoveAll(opt =>
                                opt.Label.Contains("Strip".Translate(stripTarget.LabelCap, stripTarget)) ||
                                opt.Label.Contains("CannotStrip".Translate(stripTarget.LabelCap, stripTarget))
                            );
                        }
                    }

                    // Loop through each pawn that needs tending on the cell the player has right-clicked on, and disallow tending pawns currently on MedPods
                    foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForTend(pawn), thingsOnly: true))
                    { 
                        Pawn tendTarget = (Pawn)item.Thing;
                        if (tendTarget.CurrentBed() is Building_BedMedPod) 
                        {
                            opts.RemoveAll(opt => 
                                opt.Label.Contains("Tend".Translate(tendTarget)) || 
                                opt.Label.Contains("CannotTend".Translate(tendTarget))
                            );
                        }
                    }
                }
            }
        }
    }
}