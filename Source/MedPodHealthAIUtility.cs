using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MedPod
{
    public static class MedPodHealthAIUtility
    {
        public static bool ShouldSeekMedPodRest(Pawn patientPawn, Building_BedMedPod bedMedPod)
        {
            List<Hediff> patientHediffs = new();
            patientPawn.health.hediffSet.GetHediffs(ref patientHediffs);

            List<HediffDef> alwaysTreatableHediffs = bedMedPod.GetComp<CompTreatmentRestrictions>().Props.alwaysTreatableHediffs;
            List<HediffDef> neverTreatableHediffs = bedMedPod.GetComp<CompTreatmentRestrictions>().Props.neverTreatableHediffs;
            List<HediffDef> nonCriticalTreatableHediffs = bedMedPod.GetComp<CompTreatmentRestrictions>().Props.nonCriticalTreatableHediffs;
            List<HediffDef> usageBlockingHediffs = bedMedPod.GetComp<CompTreatmentRestrictions>().Props.usageBlockingHediffs;
            List<TraitDef> usageBlockingTraits = bedMedPod.GetComp<CompTreatmentRestrictions>().Props.usageBlockingTraits;

            // Is downed and not meant to be always downed (e.g. babies)
            bool isDowned = (patientPawn.Downed && !LifeStageUtility.AlwaysDowned(patientPawn));

            // Has (visible) hediffs requiring tending (excluding those blacklisted or greylisted from MedPod treatment)
            bool hasTendableHediffs = patientHediffs.Any(x => x.Visible && x.TendableNow() && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def));

            // Has tended and healing injuries
            bool hasTendedAndHealingInjuries = patientPawn.health.hediffSet.HasTendedAndHealingInjury();

            // Has immunizable but not yet immune hediffs
            bool hasImmunizableNotImmuneHediffs = patientPawn.health.hediffSet.HasImmunizableNotImmuneHediff();

            // Has (visible) hediffs causing sick thoughts (excluding those blacklisted or greylisted from MedPod treatment)
            bool hasSickThoughtHediffs = patientHediffs.Any(x => x.def.makesSickThought && x.Visible && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def));

            // Has missing body parts
            bool hasMissingBodyParts = !patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().NullOrEmpty() && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart);

            // Has permanent injuries (excluding those blacklisted from MedPod treatment)
            bool hasPermanentInjuries = patientHediffs.Any(x => x.IsPermanent() && !neverTreatableHediffs.Contains(x.def));

            // Has chronic diseases (excluding those blacklisted or greylisted from MedPod treatment)
            bool hasChronicDiseases = patientHediffs.Any(x => x.def.chronic && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def));

            // Has addictions (excluding those blacklisted or greylisted from MedPod treatment)
            bool hasAddictions = patientHediffs.Any(x => x.def.IsAddiction && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def));

            // Has hediffs that are always treatable by MedPods
            bool hasAlwaysTreatableHediffs = patientHediffs.Any(x => alwaysTreatableHediffs.Contains(x.def));

            // Is already using a MedPod and has any greylisted hediffs
            bool hasGreylistedHediffsDuringTreatment = patientPawn.CurrentBed() == bedMedPod && patientHediffs.Any(x => nonCriticalTreatableHediffs.Contains(x.def));

            // Does not have hediffs or traits that block the pawn from using MedPods
            bool hasNoBlockingHediffsOrTraits = !HasUsageBlockingHediffs(patientPawn, usageBlockingHediffs) && !HasUsageBlockingTraits(patientPawn, usageBlockingTraits);

            bool result = (isDowned || hasTendableHediffs || hasTendedAndHealingInjuries || hasImmunizableNotImmuneHediffs || hasSickThoughtHediffs || hasMissingBodyParts || hasPermanentInjuries || hasChronicDiseases || hasAddictions || hasAlwaysTreatableHediffs || hasGreylistedHediffsDuringTreatment) && hasNoBlockingHediffsOrTraits;

            // Expose results for debugging
            if (DebugSettings.godMode)
            {
                Log.Message($"{patientPawn} should use {bedMedPod}? = {result.ToStringYesNo()}\n" +
                $"isDowned = {isDowned.ToStringYesNo()}\n" +
                $"hasTendableHediffs = {hasTendableHediffs.ToStringYesNo()}\n" +
                $"hasTendedAndHealingInjuries = {hasTendedAndHealingInjuries.ToStringYesNo()}\n" +
                $"hasImmunizableNotImmuneHediffs = {hasImmunizableNotImmuneHediffs.ToStringYesNo()}\n" +
                $"hasSickThoughtHediffs = {hasSickThoughtHediffs.ToStringYesNo()}\n" +
                $"hasMissingBodyParts = {hasMissingBodyParts.ToStringYesNo()}\n" +
                $"hasPermanentInjuries = {hasPermanentInjuries.ToStringYesNo()}\n" +
                $"hasChronicDiseases = {hasChronicDiseases.ToStringYesNo()}\n" +
                $"hasAddictions = {hasAddictions.ToStringYesNo()}\n" +
                $"hasAlwaysTreatableHediffs = {hasAlwaysTreatableHediffs.ToStringYesNo()}\n" +
                $"hasGreylistedHediffsDuringTreatment = {hasGreylistedHediffsDuringTreatment.ToStringYesNo()}\n" +
                $"hasNoBlockingHediffsOrTraits = {hasNoBlockingHediffsOrTraits.ToStringYesNo()}");
            }

            return result;
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

        public static bool IsValidXenotypeForMedPod(Pawn patientPawn, List<XenotypeDef> disallowedXenotypes)
        {
            XenotypeDef patientXenotype = patientPawn.genes?.xenotype;

            // For pawns without genes and/or xenotypes (usually animals)
            if (patientXenotype == null)
            {
                return true;
            }

            if (!disallowedXenotypes.NullOrEmpty())
            {
                return !disallowedXenotypes.Contains(patientXenotype);
            }
            return true;
        }

        public static bool HasAllowedMedicalCareCategory(Pawn patientPawn)
        {
            return WorkGiver_DoBill.GetMedicalCareCategory(patientPawn) >= MedicalCareCategory.NormalOrWorse;
        }

        public static bool HasUsageBlockingHediffs(Pawn patientPawn, List<HediffDef> usageBlockingHediffs)
        {
            List<Hediff> patientHediffs = new();
            patientPawn.health.hediffSet.GetHediffs(ref patientHediffs);

            return patientHediffs.Any(x => usageBlockingHediffs.Contains(x.def));
        }

        public static bool HasUsageBlockingTraits(Pawn patientPawn, List<TraitDef> usageBlockingTraits)
        {
            return patientPawn.story?.traits.allTraits.Any(x => usageBlockingTraits.Contains(x.def)) ?? false;
        }
    }
}