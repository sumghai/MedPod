using RimWorld;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_DoctorRescueToMedPod : WorkGiver_RescueDowned
    {
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn patient = t as Pawn;

			if (patient == null || !patient.Downed || patient.Faction != pawn.Faction || patient.CurrentBed()?.def.thingClass == typeof(Building_BedMedPod) || !pawn.CanReserve(patient, 1, -1, null, forced) || GenAI.EnemyIsNear(patient, MinDistFromEnemy))
			{
				return false;
			}
			Building_BedMedPod bedMedPod = MedPodRestUtility.FindBestMedPod(pawn, patient);
			if (bedMedPod != null && patient.CanReserve(bedMedPod))
			{
				return true;
			}
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn patient = t as Pawn;
			Building_BedMedPod bedMedPod = MedPodRestUtility.FindBestMedPod(pawn, patient);
			Job job = JobMaker.MakeJob(MedPodDef.RescueToMedPod, patient, bedMedPod);
			job.count = 1;
			return job;
		}
	}
}
