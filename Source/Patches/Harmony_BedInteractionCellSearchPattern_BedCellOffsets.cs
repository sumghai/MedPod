using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace MedPod
{
    // Generate custom interaction cell locations for MedPods and VetPods
    [HarmonyPatch(typeof(BedCellSearchPattern), nameof(BedCellSearchPattern.AddCellsToList))]
    public static class Harmony_BedCellSearchPattern_AddCellsToList
    {
        public static bool Prefix(List<IntVec3> orderedCells, Thing thing, CellRect rect, IntVec3 focus, Rot4 focusRotation)
        {
            if (thing is Building_BedMedPod bedMedPod)
            {
                if (!rect.Contains(focus))
                {
                    throw new ArgumentException();
                }
                if (bedMedPod.def.building.bed_humanlike)
                {
                    BedCellOffsetsMedPod(orderedCells);
                }
                else
                {
                    BedCellOffsetsVetPod(orderedCells, bedMedPod.def.size);
                }
                RotationDirection relativeRotation = Rot4.GetRelativeRotation(Rot4.South, focusRotation);
                for (int i = 0; i < orderedCells.Count; i++)
                {
                    orderedCells[i] = focus + orderedCells[i].RotatedBy(relativeRotation);
                }
                return false;
            }
            return true;
        }

        // MedPods are hardcoded to only have interaction cells near the bed and not the machinery at the north side
        public static void BedCellOffsetsMedPod(List<IntVec3> offsets)
        {
            offsets.Add(IntVec3.West);
            offsets.Add(IntVec3.East);
            offsets.Add(IntVec3.West + IntVec3.South);
            offsets.Add(IntVec3.East + IntVec3.South);
            offsets.Add(2 * IntVec3.South);
            offsets.Add(IntVec3.West + 2 * IntVec3.South);
            offsets.Add(IntVec3.East + 2 * IntVec3.South);
            offsets.Add(IntVec3.South);
            offsets.Add(IntVec3.Zero);
        }

        // VetPod interaction cells go around the perimeter
        public static void BedCellOffsetsVetPod(List<IntVec3> offsets, IntVec2 size)
        {
            int sideLength = Math.Min(size.x, size.z);
            int distToBorder = (int)Math.Ceiling((float) sideLength / 2);

            // West edge
            for (int i = -distToBorder; i < distToBorder + 1; i++)
            {
                offsets.Add(distToBorder * IntVec3.West + i * IntVec3.South);
            }

            // East edge
            for (int i = -distToBorder; i < distToBorder + 1; i++)
            {
                offsets.Add(distToBorder * IntVec3.East + i * IntVec3.South);
            }

            // South edge
            for (int i = -distToBorder + 1; i < distToBorder - 1; i++)
            {
                offsets.Add(i * IntVec3.East + distToBorder * IntVec3.South);
            }

            // North edge
            for (int i = -distToBorder + 1; i < distToBorder - 1; i++)
            {
                offsets.Add(i * IntVec3.East + distToBorder * IntVec3.North);
            }

            offsets.Add(IntVec3.Zero);
        }
    }
}
