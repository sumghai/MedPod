using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_PatientGoToMedPod : WorkGiver_Scanner
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
            Building_BedMedPod bedMedPod = t as Building_BedMedPod;
            return MedPodHealthAIUtility.IsValidMedPodFor(bedMedPod, pawn, pawn, pawn.GuestStatus);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(MedPodDef.PatientGoToMedPod, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            // TODO - include distance in heuristics?
            float value = t.Thing.def.GetStatValueAbstract(StatDefOf.BedRestEffectiveness);
            Log.Warning(t.Thing.ToString() + " (" + t.Thing.Label.ToString() + ") has a priority value of " + value.ToString());
            return value;
        }
    }
}