using System;
using UnityEngine;
using Verse;

namespace MedPod
{
    public class CompAnimatedGantry : ThingComp
    {
        private CompProperties_AnimatedGantry Props
        {
            get
            {
                return (CompProperties_AnimatedGantry)props;
            }
        }

        public Vector3 GantryPositionOffset()
        {
            Building_BedMedPod building_medpod = parent as Building_BedMedPod;
            float percent = (float)building_medpod.gantryPositionPercentInt / 100;
            return new Vector3(0, 0, percent * 1.71875f); // 1.71875 = 110 px max gantry offset
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Graphic gantryGlow = GraphicDatabase.Get<Graphic_Multi>("FX/MedPod_gantryGlow", ShaderDatabase.MoteGlow, new Vector2(3f, 3f), Color.white);

            Building_BedMedPod building_medpod = parent as Building_BedMedPod;
            float gantryGlowAlpha = building_medpod.GantryMoving() ? 1f : 0f;

            Mesh gantryMesh = Props.gantryGraphicData.Graphic.MeshAt(parent.Rotation);
            Mesh gantryGlowMesh = gantryGlow.MeshAt(parent.Rotation);
            Mesh machineLidMesh = Props.machineLidGraphicData.Graphic.MeshAt(parent.Rotation);

            Vector3 gantryDrawPos = parent.DrawPos;
            Vector3 gantryGlowDrawPos = parent.DrawPos;
            Vector3 machineLidDrawPos = parent.DrawPos;

            gantryDrawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.03f;
            gantryGlowDrawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.06f;
            machineLidDrawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.09f;

            Graphics.DrawMesh(gantryMesh, gantryDrawPos + Props.gantryGraphicData.drawOffset.RotatedBy(parent.Rotation) + GantryPositionOffset().RotatedBy(parent.Rotation), Quaternion.identity, Props.gantryGraphicData.Graphic.MatAt(parent.Rotation, null), 0);

            Graphics.DrawMesh(gantryGlowMesh, gantryGlowDrawPos + Props.gantryGraphicData.drawOffset.RotatedBy(parent.Rotation) + GantryPositionOffset().RotatedBy(parent.Rotation), Quaternion.identity, FadedMaterialPool.FadedVersionOf(gantryGlow.MatAt(parent.Rotation, null), gantryGlowAlpha), 0);

            Graphics.DrawMesh(machineLidMesh, machineLidDrawPos + Props.machineLidGraphicData.drawOffset.RotatedBy(parent.Rotation), Quaternion.identity, Props.machineLidGraphicData.Graphic.MatAt(parent.Rotation, null), 0);
        }
    }
}