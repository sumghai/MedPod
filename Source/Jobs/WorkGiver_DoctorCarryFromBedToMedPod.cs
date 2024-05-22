using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_DoctorCarryFromBedToMedPod : WorkGiver_DoctorRescueToMedPod
	{
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			List<Pawn> list = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].InBed())
				{
					return false;
				}
			}
			return true;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn patient = t as Pawn;

			if (patient == null || patient == pawn || !patient.InBed() || patient.CurrentBed()?.def.thingClass == typeof(Building_BedMedPod) || patient.health.surgeryBills.Bills.Any(x => x.suspended == false) || !pawn.CanReserve(patient, 1, -1, null, forced) || GenAI.EnemyIsNear(patient, MinDistFromEnemy))
			{
				return false;
			}
			Building_BedMedPod bedMedPod = MedPodRestUtility.FindBestMedPod(pawn, patient);
			if (bedMedPod != null && MedPodHealthAIUtility.ShouldSeekMedPodRest(patient, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs, bedMedPod.NonCriticalTreatableHediffs, bedMedPod.UsageBlockingHediffs, bedMedPod.UsageBlockingTraits) && MedPodHealthAIUtility.HasAllowedMedicalCareCategory(patient) && patient.CanReserve(bedMedPod))
			{
				return true;
			}
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn patient = t as Pawn;
			Building_BedMedPod bedMedPod = MedPodRestUtility.FindBestMedPod(pawn, patient);
			Job job = JobMaker.MakeJob(MedPodDef.CarryToMedPod, patient, bedMedPod);
			job.count = 1;
			return job;
		}
	}
}
