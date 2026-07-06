using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MCto3D.Models
{
    public struct StructureData
    {
        public Vector3 Size;
        public int[,,] voxelGrid;
        public BlockState[] palette;

        public StructureData(Vector3 size, int[,,] voxelGrid, BlockState[] palette)
        {
            Size = size;
            this.voxelGrid = voxelGrid;
            this.palette = palette;
        }
    }
}
