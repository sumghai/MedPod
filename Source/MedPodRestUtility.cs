using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MedPod
{
    public static class MedPodRestUtility
    {
        public static string NoPathTrans;

        public static void ResetStaticData()
        {
            NoPathTrans = "NoPath".Translate();
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
            if (guestStatus == GuestStatus.Prisoner)
            {
                if (!bedMedPod.ForPrisoners)
                {
                    return false;
                }
                if (!bedMedPod.Position.IsInPrisonCell(bedMedPod.Map))
                {
                    return false;
                }
            }
            if (patientPawn.IsPrisoner != bedMedPod.ForPrisoners)
            {
                return false;
            }
            if (!MedPodHealthAIUtility.ShouldSeekMedPodRest(patientPawn, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs))
            {
                return false;
            }
            if (!MedPodHealthAIUtility.IsValidRaceForMedPod(patientPawn, bedMedPod.DisallowedRaces))
            {
                return false;
            }
            if (MedPodHealthAIUtility.HasUsageBlockingHediffs(patientPawn, bedMedPod.UsageBlockingHediffs))
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
            List<ThingDef> medPodDefsBestToWorst = RestUtility.bedDefsBestToWorst_Medical.Where(x => x.thingClass == typeof(Building_BedMedPod)).ToList();

            float initialSearchDistance = 10f;

            // Prioritize searching for usable MedPods by distance, followed by MedPod type and path danger level
            while (initialSearchDistance <= 9999f)
            {

                for (int i = 0; i < medPodDefsBestToWorst.Count; i++)
                {
                    ThingDef thingDef = medPodDefsBestToWorst[i];

                    if (!RestUtility.CanUseBedEver(patient, thingDef))
                    {
                        continue;
                    }

                    for (int j = 0; j < 2; j++)
                    {
                        Danger maxDanger2 = (j == 0) ? Danger.None : Danger.Deadly;

                        bool validator(Thing t)
                        {
                            Building_BedMedPod bedMedPod = t as Building_BedMedPod;

                            bool isMedicalBed = bedMedPod.Medical;

                            bool patientDangerCheck = (int)bedMedPod.Position.GetDangerFor(patient, patient.Map) <= (int)maxDanger2;

                            bool isValidBedFor = IsValidMedPodFor(bedMedPod, patient, pawn, patient.GuestStatus);

                            bool result = isMedicalBed && patientDangerCheck && isValidBedFor;

                            return result;
                        }

                        Building_BedMedPod bedMedPod = (Building_BedMedPod)GenClosest.ClosestThingReachable(patient.Position, patient.Map, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(pawn), initialSearchDistance, validator);

                        if (bedMedPod != null)
                        {
                            return bedMedPod;
                        }
                    }
                }

                // Double our search range for each iteration
                initialSearchDistance *= 2;
            }

            return null;
        }
    }
}
