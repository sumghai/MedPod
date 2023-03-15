using RimWorld;
using Verse;

namespace MedPod
{
    public static class DbhCompatibility
    {
        public static float GetThirstNeedCurLevelPercentage(Pawn pawn)
        {
            var thirstNeed = pawn.needs.TryGetNeed<DubsBadHygiene.Need_Thirst>();
            if (thirstNeed != null)
            {
                return thirstNeed.CurLevelPercentage;
            }
            return 0;
        }

        public static void SetThirstNeedCurLevelPercentage(Pawn pawn, float value)
        {
            var thirstNeed = pawn.needs.TryGetNeed<DubsBadHygiene.Need_Thirst>();
            if (thirstNeed != null)
            {
                thirstNeed.CurLevelPercentage = value;
            }
        }

        public static float GetHygieneNeedCurLevelPercentage(Pawn pawn)
        {
            var thirstNeed = pawn.needs.TryGetNeed<DubsBadHygiene.Need_Hygiene>();
            if (thirstNeed != null)
            {
                return thirstNeed.CurLevelPercentage;
            }
            return 0;
        }

        public static void SetHygieneNeedCurLevelPercentage(Pawn pawn, float value)
        {
            var thirstNeed = pawn.needs.TryGetNeed<DubsBadHygiene.Need_Hygiene>();
            if (thirstNeed != null)
            {
                thirstNeed.CurLevelPercentage = value;
            }
        }

        public static void SetBladderNeedCurLevelPercentage(Pawn pawn, float value)
        {
            var thirstNeed = pawn.needs.TryGetNeed<DubsBadHygiene.Need_Bladder>();
            if (thirstNeed != null)
            {
                thirstNeed.CurLevelPercentage = value;
            }
        }

        public static void ShouldBeWashedBySomeonePostfix(Pawn pawn, ref bool __result)
        {
            if (pawn.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
            {
                __result = false;
            }
        }
    }
}
