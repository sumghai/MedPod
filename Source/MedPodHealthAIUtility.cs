using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MedPod
{
    public static class MedPodHealthAIUtility
    {
        public static string NoPathTrans;

        public static void ResetStaticData()
        {
            NoPathTrans = "NoPath".Translate();
        }

        public static bool ShouldSeekMedPodRest(Pawn patientPawn, List<HediffDef> alwaysTreatableHediffs, List<HediffDef> neverTreatableHediffs)
        {
            return 
                    // Is downed
                    patientPawn.Downed
                    // Has hediffs requiring tending
                    || patientPawn.health.HasHediffsNeedingTend(false)
                    // Has tended and healing injuries
                    || patientPawn.health.hediffSet.HasTendedAndHealingInjury()
                    // Has immunizable but not yet immune hediffs
                    || patientPawn.health.hediffSet.HasImmunizableNotImmuneHediff()
                    // Has missing body parts
                    || (!patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().NullOrEmpty() && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart))
                    // Has permanent injuries (excluding those blacklisted from MedPod treatment)
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.IsPermanent() && !neverTreatableHediffs.Contains(x.def))
                    // Has chronic diseases (excluding those blacklisted from MedPod treatment)
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.chronic && !neverTreatableHediffs.Contains(x.def))
                    // Has addictions (excluding those blacklisted from MedPod treatment)
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.IsAddiction && !neverTreatableHediffs.Contains(x.def))
                    // Has hediffs that are always treatable by MedPods
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => alwaysTreatableHediffs.Contains(x.def));
        }

        public static bool IsValidRaceForMedPod(Pawn patientPawn, List<string> disallowedRaces)
        {
            string patientRace = patientPawn.def.ToString();
            if (!disallowedRaces.NullOrEmpty())
            {
                return !disallowedRaces.Contains(patientRace);
            }
            return true;
        }

        public static bool HasUsageBlockingHediffs(Pawn patientPawn, List<HediffDef> usageBlockingHediffs)
        {
            return patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => usageBlockingHediffs.Contains(x.def));
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
            if (!ShouldSeekMedPodRest(patientPawn, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs))
            {
                return false;
            }
            if (!IsValidRaceForMedPod(patientPawn, bedMedPod.DisallowedRaces))
            {
                return false;
            }
            if (HasUsageBlockingHediffs(patientPawn, bedMedPod.UsageBlockingHediffs))
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
    }
}