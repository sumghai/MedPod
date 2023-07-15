using System.Linq;
using Verse;

namespace MedPod
{
    public class ModCompatibility
    {
        // Alpha Genes (use PackageId.Equals to match only the main Alpha Genes mod, and not the Alpha Genes - Insectoid Mutations gene pack)
        public static bool AlphaGenesIsActive => ModLister.AllInstalledMods.Where(x => x.Active && x.PackageId.Equals("sarg.alphagenes".ToLower())).Any();

        // Android Tiers
        public static bool AndroidTiersIsActive => ModLister.AllInstalledMods.Where(x => x.Active && x.PackageId.Contains("Atlas.AndroidTiers".ToLower())).Any();
        
        // Applies to both DBH and DBH Lite (with Thirst module)
        public static bool DbhIsActive => ModLister.AllInstalledMods.Where(x => x.Active && x.PackageId.Contains("Dubwise.DubsBadHygiene".ToLower())).Any();
    }
}
