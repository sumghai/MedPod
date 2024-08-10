using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MedPod
{
    public static class MedPodRestUtility
    {
        private static List<ThingDef> medPodDefsBestToWorst => medPodDefsBestToWorstCached ??= RestUtility.bedDefsBestToWorst_Medical.Where(x => x.thingClass == typeof(Building_BedMedPod)).ToList();

        private static List<ThingDef> medPodDefsBestToWorstCached;

        private static List<Thing> tempMedPodList = new();

        public static string NoPathTrans;

        public static void ResetStaticData()
        {
            NoPathTrans = "NoPath".Translate();
        }

        public static bool IsValidBedForUserType(Building_BedMedPod bedMedPod, Pawn pawn)
        {
            // VetPods: skip execution early and return true if patient is an animal
            if (pawn.RaceProps.Animal && !bedMedPod.def.building.bed_humanlike)
            {
                return true;
            }
            
            // Otherwise, check for humanlike patients
            bool isSlave = pawn.GuestStatus == GuestStatus.Slave;
            bool isPrisoner = pawn.GuestStatus == GuestStatus.Prisoner;

            if (bedMedPod.ForSlaves != isSlave)
            {
                return false;
            }
            if (bedMedPod.ForPrisoners != isPrisoner)
            {
                return false;
            }
            if (bedMedPod.ForColonists && (!pawn.IsColonist || pawn.GuestStatus == GuestStatus.Guest) && !bedMedPod.allowGuests)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidMedPodFor(Building_BedMedPod bedMedPod, Pawn patientPawn, Pawn travelerPawn, GuestStatus? guestStatus = null)
        {
            if (bedMedPod == null)
            {
                return false;
            }
            if (!bedMedPod.powerComp.PowerOn)
            {
                return false;
            }
            if (bedMedPod.IsForbidden(travelerPawn))
            {
                return false;
            }
            if (!travelerPawn.CanReserve(bedMedPod))
            {
                Pawn otherPawn = travelerPawn.Map.reservationManager.FirstRespectedReserver(bedMedPod, patientPawn);
                if (otherPawn != null)
                {
                    JobFailReason.Is("ReservedBy".Translate(otherPawn.LabelShort, otherPawn));
                }
                return false;
            }
            if (!travelerPawn.CanReach(bedMedPod, PathEndMode.OnCell, Danger.Deadly))
            {
                JobFailReason.Is(NoPathTrans);
                return false;
            }
            if (travelerPawn.Map.designationManager.DesignationOn(bedMedPod, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (!RestUtility.CanUseBedEver(patientPawn, bedMedPod.def))
            {
                return false;
            }
            if (!IsValidBedForUserType(bedMedPod, patientPawn))
            {
                return false;
            }
            if (!MedPodHealthAIUtility.ShouldSeekMedPodRest(patientPawn, bedMedPod))
            {
                return false;
            }
            if (!MedPodHealthAIUtility.HasAllowedMedicalCareCategory(patientPawn))
            {
                return false;
            }
            if (!MedPodHealthAIUtility.IsValidRaceForMedPod(patientPawn, bedMedPod.DisallowedRaces))
            {
                return false;
            }
            if (!MedPodHealthAIUtility.IsValidXenotypeForMedPod(patientPawn, bedMedPod.DisallowedXenotypes))
            {
                return false;
            }
            if (MedPodHealthAIUtility.HasUsageBlockingHediffs(patientPawn, bedMedPod.UsageBlockingHediffs))
            {
                return false;
            }
            if (MedPodHealthAIUtility.HasUsageBlockingTraits(patientPawn, bedMedPod.UsageBlockingTraits))
            {
                return false;
            }
            if (bedMedPod.Aborted) 
            {
                return false;
            }
            if (bedMedPod.IsBurning())
            {
                return false;
            }
            if (bedMedPod.IsBrokenDown())
            {
                return false;
            }
            return true;
        }

        public static Building_BedMedPod FindBestMedPod(Pawn pawn, Pawn patient)
        {
            // Skip if there are no MedPod bed defs
            if (!medPodDefsBestToWorst.Any())
            {
                return null;
            }

            Map map = patient.Map;
            ListerThings listerThings = map.listerThings;
            tempMedPodList.Clear();

            // Prioritize searching for usable MedPods by distance, followed by MedPod type and path danger level
            try
            {
                foreach (ThingDef medPodDef in medPodDefsBestToWorst)
                {
                    // Skip MedPod types that the patient can never use
                    if (!RestUtility.CanUseBedEver(patient, medPodDef))
                    {
                        continue;
                    }

                    // Check each MedPod thing of the current type on the map, and add the ones usable by the current patient to a temporary list
                    foreach (Thing medPod in listerThings.ThingsOfDef(medPodDef))
                    {
                        if (medPod is Building_BedMedPod { Medical: true } bedMedPod
                        && IsValidMedPodFor(bedMedPod, patient, pawn, patient.GuestStatus))
                        {
                            tempMedPodList.Add(bedMedPod);
                        }
                    }
                }

                // Look for the closest reachable MedPod from the temporary list, going down by danger level
                for (int i = 0; i < 2; i++)
                {
                    Danger maxDanger = i == 0 ? Danger.None : Danger.Deadly;

                    Building_BedMedPod bedMedPod = (Building_BedMedPod)GenClosest.ClosestThingReachable(patient.Position, map, ThingRequest.ForUndefined(), PathEndMode.OnCell, TraverseParms.For(pawn), validator: thing => thing.Position.GetDangerFor(patient, map) <= maxDanger, customGlobalSearchSet: tempMedPodList);

                    if (bedMedPod != null)
                    {
                        return bedMedPod;
                    }
                }
            }
            finally 
            { 
                // Clean up out temporary list once we're done
                tempMedPodList.Clear();
            }

            // Can't find any valid MedPods
            return null;
        }
    }
}
