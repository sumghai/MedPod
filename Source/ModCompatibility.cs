using System.Linq;
using Verse;

namespace MedPod
{
    public class ModCompatibility
    {
        // Android Tiers
        public static bool AndroidTiersIsActive => ModLister.AllInstalledMods.Where(x => x.Active && x.PackageId.Contains("Atlas.AndroidTiers".ToLower())).Any();
        
        // Applies to both DBH and DBH Lite (with Thirst module)
        public static bool DbhIsActive => ModLister.AllInstalledMods.Where(x => x.Active && x.PackageId.Contains("Dubwise.DubsBadHygiene".ToLower())).Any();

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

        // Vanilla Traits Expanded
        public static bool VteIsActive => ModLister.AllInstalledMods.Where(x => x.Active && x.PackageId.Contains("VanillaExpanded.VanillaTraitsExpanded".ToLower())).Any();
    }
}
