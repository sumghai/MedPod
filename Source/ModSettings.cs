using UnityEngine;
using Verse;

namespace MedPod
{
    public class Settings : ModSettings
    {
        public void Draw(Rect canvas)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(canvas);

            // Do general settings
            Text.Font = GameFont.Medium;
            listingStandard.Label("MedPod_Settings_HeaderGeneral".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap();

            listingStandard.End();
        }
    }
}
