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
    }
}
