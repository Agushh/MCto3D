using fNbt;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MCto3D.Models
{
    public struct Triangle
    {
        public Vector3 V1, V2, V3, Normal;

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            Normal = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
        }

        /// <summary>
        /// Generates a list of triangles representing the 3D mesh from a Minecraft NBT structure file.
        /// Iterates through the voxel grid and applies face culling to omit hidden faces.
        /// </summary>
        /// <param name="filePath">Absolute path to the .nbt file.</param>
        /// <param name="scale">The block scale multiplier.</param>
        /// <returns>A list of triangles forming the optimized mesh.</returns>
        public static List<Triangle> GenerateListTriangle(string filePath, float scale)
        {
            var myFile = new NbtFile();
            myFile.LoadFromFile(filePath);

            var rootTag = myFile.RootTag;

            var palette = rootTag.Get<NbtList>("palette");
            var blocks = rootTag.Get<NbtList>("blocks");
            var size = rootTag.Get<NbtList>("size");

            if (blocks == null || size == null)
            {
                throw new Exception("El archivo está corrupto o tiene un formato incorrecto.");
            }

            int sizeX = size[0].IntValue;
            int sizeY = size[1].IntValue;
            int sizeZ = size[2].IntValue;

            if (sizeX == 0 && sizeY == 0 && sizeZ == 0)
            {
                throw new Exception("El archivo tiene dimensiones no válidas.");
            }

            int[,,] voxelGrid = new int[sizeX, sizeY, sizeZ];

            foreach (NbtCompound blockTag in blocks)
            {
                var pos = blockTag.Get<NbtList>("pos");
                int stateId = blockTag.Get<NbtInt>("state").Value;

                int x = pos[0].IntValue;
                int y = pos[1].IntValue;
                int z = pos[2].IntValue;

                voxelGrid[x, y, z] = stateId;
            }

            List<Triangle> triangles = new();

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int blockState = voxelGrid[x, y, z];

                        if (blockState <= 0) continue;

                        for (int f = 0; f < 6; f++)
                        {
                            int nx = x + VoxelData.NeighborOffsets[f][0];
                            int ny = y + VoxelData.NeighborOffsets[f][1];
                            int nz = z + VoxelData.NeighborOffsets[f][2];

                            // Face culling
                            if (nx < 0 || nx >= sizeX || ny < 0 || ny >= sizeY || nz < 0 || nz >= sizeZ || voxelGrid[nx, ny, nz] <= 0)
                            {
                                int i0 = VoxelData.FaceTriangles[f][0];
                                int i1 = VoxelData.FaceTriangles[f][1];
                                int i2 = VoxelData.FaceTriangles[f][2];
                                int i3 = VoxelData.FaceTriangles[f][3];

                                float x0 = (x + VoxelData.Vertices[i0].X) * scale;
                                float y0 = (y + VoxelData.Vertices[i0].Y) * scale;
                                float z0 = (z + VoxelData.Vertices[i0].Z) * scale;

                                float x1 = (x + VoxelData.Vertices[i1].X) * scale;
                                float y1 = (y + VoxelData.Vertices[i1].Y) * scale;
                                float z1 = (z + VoxelData.Vertices[i1].Z) * scale;

                                float x2 = (x + VoxelData.Vertices[i2].X) * scale;
                                float y2 = (y + VoxelData.Vertices[i2].Y) * scale;
                                float z2 = (z + VoxelData.Vertices[i2].Z) * scale;

                                float x3 = (x + VoxelData.Vertices[i3].X) * scale;
                                float y3 = (y + VoxelData.Vertices[i3].Y) * scale;
                                float z3 = (z + VoxelData.Vertices[i3].Z) * scale;

                                // Rotation correction (Y-Up to Z-Up)
                                Vector3 v0 = new Vector3(x0, -z0, y0);
                                Vector3 v1 = new Vector3(x1, -z1, y1);
                                Vector3 v2 = new Vector3(x2, -z2, y2);
                                Vector3 v3 = new Vector3(x3, -z3, y3);

                                triangles.Add(new Triangle(v0, v1, v2));
                                triangles.Add(new Triangle(v0, v2, v3));
                            }
                        }
                    }
                }
            }
            
            return triangles;
        }
    }
}
