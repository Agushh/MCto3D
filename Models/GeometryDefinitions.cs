using fNbt;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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

        public static List<Triangle> GenerateListTriangle(string _selectedFilePath, float Bs)
        {
            var myFile = new NbtFile();
            myFile.LoadFromFile(_selectedFilePath);

            var rootTag = myFile.RootTag;

            var palette = rootTag.Get<NbtList>("palette");
            var blocks = rootTag.Get<NbtList>("blocks");
            var size = rootTag.Get<NbtList>("size");


            if (blocks != null && size != null)
            {
                int sizeX = size[0].IntValue;
                int sizeY = size[1].IntValue;
                int sizeZ = size[2].IntValue;

                if (sizeX != 0 || sizeY != 0 || sizeZ != 0)
                {
                    int[,,] voxelGrid = new int[sizeX, sizeY, sizeZ];
                    for (int x = 0; x < sizeX; x++)
                        for (int y = 0; y < sizeY; y++)
                            for (int z = 0; z < sizeZ; z++)
                                voxelGrid[x, y, z] = 0;

                    foreach (NbtCompound blockTag in blocks)
                    {
                        var pos = blockTag.Get<NbtList>("pos");
                        int stateId = blockTag.Get<NbtInt>("state").Value;

                        int x = pos[0].IntValue;
                        int y = pos[1].IntValue;
                        int z = pos[2].IntValue;

                        // Guardamos qué bloque hay en esta coordenada exacta
                        voxelGrid[x, y, z] = stateId;
                    }
                    List<Triangle> t = new();
                    for (int x = 0; x < sizeX; x++)
                        for (int y = 0; y < sizeY; y++)
                            for (int z = 0; z < sizeZ; z++)
                            {
                                int blockState = voxelGrid[x, y, z];

                                if (blockState == -1 || blockState == 0) continue;

                                // Revisamos las 6 caras usando un bucle
                                for (int f = 0; f < 6; f++)
                                {
                                    // Coordenadas del bloque vecino
                                    int nx = x + VoxelData.NeighborOffsets[f][0];
                                    int ny = y + VoxelData.NeighborOffsets[f][1];
                                    int nz = z + VoxelData.NeighborOffsets[f][2];

                                    // Condición única de Face Culling: 
                                    // Dibujamos la cara si toca el borde del mapa O si el vecino es aire (<= 0)
                                    if (nx < 0 || nx >= sizeX || ny < 0 || ny >= sizeY || nz < 0 || nz >= sizeZ || voxelGrid[nx, ny, nz] <= 0)
                                    {
                                        // Extraemos los índices correspondientes a este número de cara
                                        int i0 = VoxelData.FaceTriangles[f][0];
                                        int i1 = VoxelData.FaceTriangles[f][1];
                                        int i2 = VoxelData.FaceTriangles[f][2];
                                        int i3 = VoxelData.FaceTriangles[f][3];

                                        // CORRECCIÓN ESCALA: Multiplicamos toda la coordenada por 'bs' para evitar que se separen
                                        float x0 = (x + VoxelData.Vertices[i0].X) * Bs;
                                        float y0 = (y + VoxelData.Vertices[i0].Y) * Bs;
                                        float z0 = (z + VoxelData.Vertices[i0].Z) * Bs;

                                        float x1 = (x + VoxelData.Vertices[i1].X) * Bs;
                                        float y1 = (y + VoxelData.Vertices[i1].Y) * Bs;
                                        float z1 = (z + VoxelData.Vertices[i1].Z) * Bs;

                                        float x2 = (x + VoxelData.Vertices[i2].X) * Bs;
                                        float y2 = (y + VoxelData.Vertices[i2].Y) * Bs;
                                        float z2 = (z + VoxelData.Vertices[i2].Z) * Bs;

                                        float x3 = (x + VoxelData.Vertices[i3].X) * Bs;
                                        float y3 = (y + VoxelData.Vertices[i3].Y) * Bs;
                                        float z3 = (z + VoxelData.Vertices[i3].Z) * Bs;

                                        // CORRECCIÓN ROTACIÓN (Y-Up a Z-Up): Reordenamos los componentes para que el 
                                        // modelo no aparezca acostado en el software de impresión 3D
                                        Vector3 v0 = new Vector3(x0, -z0, y0);
                                        Vector3 v1 = new Vector3(x1, -z1, y1);
                                        Vector3 v2 = new Vector3(x2, -z2, y2);
                                        Vector3 v3 = new Vector3(x3, -z3, y3);

                                        // Añadimos los dos triángulos que completan este plano cuadrado sin deformaciones diagonales
                                        t.Add(new Triangle(v0, v1, v2));
                                        t.Add(new Triangle(v0, v2, v3));
                                    }
                                }
                            }
                    return t;
                }
                else
                {
                    throw new Exception("El archivo corrupto o incorrecto.");
                }
            }
            else
            {
                throw new Exception("El archivo corrupto o incorrecto.");
            }
        }

    }

    

}

