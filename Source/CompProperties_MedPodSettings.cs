using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using UnityEngine;
using Verse;

namespace MedPod
{
    public class CompProperties_MedPodSettings : CompProperties
    {
        // Define defaults if not specified in XML defs

        public float maxDiagnosisTime = 5f;

        public float maxPerHediffHealingTime = 10f;

        public float diagnosisModePowerConsumption = 8000f;

        public float healingModePowerConsumption = 32000f;

        public bool disableInvisibleBlocker = false;

        public GraphicData screenGlowGraphicData = null;

        public List<AnimalRestingDrawOffset> animalRestingDrawOffsets = null;

        public CompProperties_MedPodSettings()
        {
            compClass = typeof(CompMedPodSettings);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (maxDiagnosisTime > 30f)
            {
                yield return $"{nameof(CompProperties_MedPodSettings)}.{nameof(maxDiagnosisTime)} above allowed maximum; value capped at 30 seconds";
                maxDiagnosisTime = 30f;
            }
            if (maxPerHediffHealingTime > 30f)
            {
                yield return $"{nameof(CompProperties_MedPodSettings)}.{nameof(maxPerHediffHealingTime)} above allowed maximum; value capped at 30 seconds";
                maxPerHediffHealingTime = 30f;
            }
        }

        [DebuggerHidden]
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (StatDrawEntry s in base.SpecialDisplayStats(req))
            {
                yield return s;
            }
            yield return new StatDrawEntry(StatCategoryDefOf.Building, "MedPod_Stat_PowerConsumptionDiagnosis_Label".Translate(), diagnosisModePowerConsumption.ToString("F0") + " W", "MedPod_Stat_PowerConsumptionDiagnosis_Desc".Translate(), 4994);
            yield return new StatDrawEntry(StatCategoryDefOf.Building, "MedPod_Stat_PowerConsumptionHealing_Label".Translate(), healingModePowerConsumption.ToString("F0") + " W", "MedPod_Stat_PowerConsumptionHealing_Desc".Translate(), 4993);
            yield return new StatDrawEntry(StatCategoryDefOf.Building, "MedPod_Stat_DiagnosisTime_Label".Translate(), "MedPod_Stat_TimeSeconds".Translate(maxDiagnosisTime), "MedPod_Stat_DiagnosisTime_Desc".Translate(), 4992);
            yield return new StatDrawEntry(StatCategoryDefOf.Building, "MedPod_Stat_PerHediffHealingTime_Label".Translate(), "MedPod_Stat_TimeSeconds".Translate(maxPerHediffHealingTime), "MedPod_Stat_PerHediffHealingTime_Desc".Translate(), 4991);
        }
    }

    public class AnimalRestingDrawOffset
    {
        public ThingDef animalRace;

        public Vector3 drawOffset;

        public AnimalRestingDrawOffset()
        { 
        }

        public AnimalRestingDrawOffset(ThingDef animalRace, Vector3 drawOffset)
        {
            this.animalRace = animalRace;
            this.drawOffset = drawOffset;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured AnimalRestingDrawOffset: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "animalRace", xmlRoot.Name);
            drawOffset = ParseHelper.FromString<Vector3>(xmlRoot.FirstChild.Value);
        }

        public override string ToString()
        {
            return "(" + ((animalRace != null) ? animalRace : "null") + " with draw offset of " + drawOffset + ")";
        }
    }
}