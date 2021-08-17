using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_PatientGoToMedPodEmergency : WorkGiver_Scanner
    {
        public static string NoPathTrans;

        public override bool Prioritized => true;

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public static void ResetStaticData()
        {
            NoPathTrans = "NoPath".Translate();
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_BedMedPod>();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_BedMedPod bedMedPod) || !bedMedPod.powerComp.PowerOn)
            {
                return false;
            }
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                Pawn pawn2 = pawn.Map.reservationManager.FirstRespectedReserver(t, pawn);
                if (pawn2 != null)
                {
                    JobFailReason.Is("ReservedBy".Translate(pawn2.LabelShort, pawn2));
                }
                return false;
            }
            if (!pawn.CanReach(t, PathEndMode, Danger.Deadly, false, mode: TraverseMode.ByPawn))
            {
                JobFailReason.Is(NoPathTrans);
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (!bedMedPod.def.building.bed_humanlike)
            {
                return false;
            }
            if (pawn.IsPrisoner && !bedMedPod.ForPrisoners) // Prevent prisoners using non-prisoner MedPods...
            {
                return false;
            }
            if (!pawn.IsPrisoner && bedMedPod.ForPrisoners) // ...and vice versa
            {
                return false;
            }
            if (!MedPodHealthAIUtility.ShouldSeekMedPodRest(pawn, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs))
            {
                return false;
            }
            if (!MedPodHealthAIUtility.IsValidRaceForMedPod(pawn, bedMedPod.DisallowedRaces))
            {
                return false;
            }
            if (MedPodHealthAIUtility.HasUsageBlockingHediffs(pawn, bedMedPod.UsageBlockingHediffs))
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (t.IsBrokenDown())
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(MedPodDef.PatientGoToMedPodEmergency, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            // TODO - include distance in heuristics
            t.Thing.TryGetQuality(out QualityCategory quality);
            float value = t.Thing.def.GetStatValueAbstract(StatDefOf.BedRestEffectiveness) + (float)quality;
            Log.Warning(t.Thing.ToString() + " (" + t.Thing.Label.ToString() + ") has a priority value of " + value.ToString());
            return value;
        }
    }
}