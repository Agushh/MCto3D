using System.Numerics;

namespace MCto3D.Models
{
    /// <summary>
    /// Contains coordinate offsets and vertex data used to generate the faces of a voxel.
    /// </summary>
    internal class VoxelData
    {
        // 8 corners of the voxel block
        public static readonly Vector3[] Vertices =
        [
            new(0, 0, 0), // 0: Back-Left-Bottom
            new(1, 0, 0), // 1: Back-Right-Bottom
            new(1, 1, 0), // 2: Back-Right-Top
            new(0, 1, 0), // 3: Back-Left-Top
            new(0, 0, 1), // 4: Front-Left-Bottom
            new(1, 0, 1), // 5: Front-Right-Bottom
            new(1, 1, 1), // 6: Front-Right-Top
            new(0, 1, 1)  // 7: Front-Left-Top
        ];

        // Vertices order for the 6 faces (Top, Bottom, Right, Left, Front, Back)
        public static readonly int[][] FaceTriangles =
        [
            [3, 7, 6, 2], // Face 0: +Y (Top)
            [4, 0, 1, 5], // Face 1: -Y (Bottom)
            [6, 5, 1, 2], // Face 2: +X (Right)
            [3, 0, 4, 7], // Face 3: -X (Left)
            [7, 4, 5, 6], // Face 4: +Z (Front)
            [2, 1, 0, 3]  // Face 5: -Z (Back)
        ];

        // Neighbor offsets matching the order of the faces
        public static readonly int[][] NeighborOffsets =
        [
            [0, 1, 0],  // 0: Top neighbor
            [0, -1, 0], // 1: Bottom neighbor
            [1, 0, 0],  // 2: Right neighbor
            [-1, 0, 0], // 3: Left neighbor
            [0, 0, 1],  // 4: Front neighbor
            [0, 0, -1]  // 5: Back neighbor
        ];
        
    }

}
