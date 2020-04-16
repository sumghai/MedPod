using UnityEngine;
using Verse;

namespace MedPod
{
    public class MedPodMod : Mod
    {
        public static Settings Settings;

        public MedPodMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Settings.Draw(canvas);
            base.DoSettingsWindowContents(canvas);
        }

        public override string SettingsCategory()
        {
            return "MedPod_SettingsCategory_Heading".Translate();
        }
    }
}
