using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MedPod
{
    // Adds a "Rescue [pawn] to MedPod" float menu option to pawns
    public class FloatMenuOptionProvider_RescuePawnToMedPod : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override bool RequiresManipulation => true;

        public override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn rescuer = context.FirstSelectedPawn;

            // Only if there is at least one player-owned MedPod on the pawn's current map
            if (rescuer.Map.listerBuildings.ColonistsHaveBuilding((Thing building) => building is Building_BedMedPod))
            {                
                // Get the first MedPod on the map
                Building_BedMedPod building_BedMedPod = rescuer.Map.listerBuildings.AllBuildingsColonistOfClass<Building_BedMedPod>().First();

                // Skip victims:
                // - Who are unreachable
                // - Who do not need MedPod treatment
                // - Who are already in beds
                // - Who will automatically join the player when rescued
                // - Who have hediffs or traits that prevent them from using MedPods
                // - Who are of races that can't use MedPods
                // - Who are of xenotypes that can't use MedPods
                if (!rescuer.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
                {
                    return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                }

                if (!MedPodHealthAIUtility.ShouldSeekMedPodRest(clickedPawn, building_BedMedPod) || clickedPawn.InBed() ||clickedPawn.mindState.WillJoinColonyIfRescued)
                {
                    return null;
                }

                if (MedPodHealthAIUtility.HasUsageBlockingHediffs(clickedPawn, building_BedMedPod.UsageBlockingHediffs))
                {
                    List<Hediff> blockedHediffs = new();
                    clickedPawn.health.hediffSet.GetHediffs(ref blockedHediffs);

                    return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "MedPod_FloatMenu_PatientWithHediffNotAllowed".Translate(blockedHediffs.First(h => building_BedMedPod.UsageBlockingHediffs.Contains(h.def)).LabelCap), null);
                }

                if (MedPodHealthAIUtility.HasUsageBlockingTraits(clickedPawn, building_BedMedPod.UsageBlockingTraits))
                {
                    return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "MedPod_FloatMenu_PatientWithTraitNotAllowed".Translate(clickedPawn.story?.traits.allTraits.First(t => building_BedMedPod.UsageBlockingTraits.Contains(t.def)).LabelCap) + ")", null);
                }

                if (!MedPodHealthAIUtility.IsValidRaceForMedPod(clickedPawn, building_BedMedPod.DisallowedRaces))
                {
                    return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "MedPod_FloatMenu_RaceNotAllowed".Translate(clickedPawn.def.label.CapitalizeFirst()) + ")", null);
                }

                if (!MedPodHealthAIUtility.IsValidXenotypeForMedPod(clickedPawn, building_BedMedPod.DisallowedXenotypes))
                {
                    return new FloatMenuOption("CannotRescuePawn".Translate(clickedPawn.Named("PAWN")) + ": " + "MedPod_FloatMenu_RaceNotAllowed".Translate(clickedPawn.genes.xenotype.label.CapitalizeFirst()) + ")", null);
                }

                // Allow victims:
                // - Who are not in a mental state
                // - Who are colonists with Scaria
                // - Who belong to no faction
                // - Who are not hostile to the player
                if ((!clickedPawn.InMentalState || clickedPawn.health.hediffSet.HasHediff(HediffDefOf.Scaria)) && (clickedPawn.Faction == Faction.OfPlayer || clickedPawn.Faction == null || !clickedPawn.Faction.HostileTo(Faction.OfPlayer)))
                {
                    FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("MedPod_FloatMenu_Rescue".Translate(clickedPawn.LabelCap, clickedPawn), delegate
                    {
                        Building_BedMedPod building_BedMedPod = MedPodRestUtility.FindBestMedPod(rescuer, clickedPawn);

                        // Display message on top screen if no valid MedPod is found
                        if (building_BedMedPod == null)
                        {
                            string pawnType = clickedPawn.IsSlave ? "Slave".Translate() : clickedPawn.IsPrisoner ? "PrisonerLower".Translate() : "Colonist".Translate();

                            string reason = (!clickedPawn.RaceProps.Animal) ? ((string)"MedPod_Message_CannotRescue_NoMedPod".Translate(pawnType.ToLower())) : ((string)"MedPod_Message_CannotRescue_NoVetPod".Translate());
                            Messages.Message("CannotRescue".Translate() + ": " + reason, clickedPawn, MessageTypeDefOf.RejectInput, historical: false);
                        }
                        // Assign a MedPod rescue job to the pawn
                        else
                        {
                            Job job = JobMaker.MakeJob(MedPodDef.RescueToMedPod, clickedPawn, building_BedMedPod);
                            job.count = 1;
                            rescuer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, clickedPawn), rescuer, clickedPawn);
                    return floatMenuOption;
                }
            }
            return null;
        }
    }
}
