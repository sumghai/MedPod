using RimWorld;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_WardenRescueToMedPod : WorkGiver_Warden
    {
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn warden = pawn;
			Pawn prisoner = t as Pawn;

			if (!ShouldTakeCareOfPrisoner(pawn, prisoner))
			{
				return null;
			}
			if (!prisoner.Downed)
			{
				return null;
			}
			if (prisoner.CurrentBed()?.def.thingClass == typeof(Building_BedMedPod))
			{
				return null;
			}
			if (!warden.CanReserve(prisoner))
			{
				return null;
			}

			Log.Warning(warden + " has job on prisoner " + prisoner);

			Building_BedMedPod bedMedPod = WorkGiver_DoctorRescueToMedPod.FindBestMedPod(warden, prisoner);
			if (bedMedPod != null && prisoner.CanReserve(bedMedPod))
			{
				Job job = JobMaker.MakeJob(MedPodDef.RescueToMedPod, prisoner, bedMedPod);
				job.count = 1;
				return job;
			}

			return null;
		}
	}
}
