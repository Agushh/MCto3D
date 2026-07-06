using fNbt;
using MCto3D.Models;
using MCto3D.Services.AssetsProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MCto3D.Services
{
    public class MeshService
    {
        private readonly NativeModelResolverService _nativeModelResolverService;

        public MeshService(NativeModelResolverService nativeModelResolverService)
        {
            _nativeModelResolverService = nativeModelResolverService;
        }

        private static bool IsAirOrInvisible(string blockName)
        {
            string clean = blockName.Replace("minecraft:", "");
            return clean == "air" || clean == "cave_air" || clean == "structure_void" || 
                   clean == "jigsaw" || clean == "barrier" || clean == "light" || 
                   clean == "water" || clean == "lava";
        }

        public List<Triangle> GenerateFullGeometryMesh(StructureData strData, float scale)
        {
            List<Triangle> triangles = new();
            
            //Temporal implementation of clean names for palette. This needs to be in StructureLoaderService And save clean names from the beggining.


            for (int x = 0; x < strData.Size.X; x++)
            {
                for (int y = 0; y < strData.Size.Y; y++)
                {
                    for (int z = 0; z < strData.Size.Z; z++)
                    {
                        int blockState = strData.voxelGrid[x, y, z];

                        if (blockState < -2 || blockState == -1 || (blockState >= 0 && IsAirOrInvisible(strData.palette[blockState].Name))) continue;

                        bool isSolid;
                        List<(Vector3 from, Vector3 to)> geometryCuboids;

                        if (blockState == -2)
                        {
                            geometryCuboids = new List<(Vector3 from, Vector3 to)> { (Vector3.Zero, new Vector3(16, 16, 16)) };
                            isSolid = true;
                        }
                        else
                        {
                            // HACK: Limpiar blockName (viene con minecraft: y estados). 
                            string cleanName = strData.palette[blockState].Name;

                            // Resolutor Nativo: Lee el JSON directamente y aplica rotaciones oficiales
                            geometryCuboids = _nativeModelResolverService.ResolveGeometry(strData.palette[blockState].Name, strData.palette[blockState].Properties);
                            isSolid = (geometryCuboids.Count == 1 && geometryCuboids[0].from == Vector3.Zero && geometryCuboids[0].to == new Vector3(16, 16, 16));
                        }
                        
                        if (geometryCuboids.Count == 0) continue;

                        foreach (var cuboid in geometryCuboids)
                        {
                            Vector3[] localVertices = GetCuboidVertices(cuboid.from, cuboid.to);

                            for (int f = 0; f < 6; f++)
                            {
                                bool drawFace = true;

                                if (isSolid)
                                {
                                    int nx = x + VoxelData.NeighborOffsets[f][0];
                                    int ny = y + VoxelData.NeighborOffsets[f][1];
                                    int nz = z + VoxelData.NeighborOffsets[f][2];

                                    if (nx >= 0 && nx < strData.Size.X && ny >= 0 && ny < strData.Size.Y && nz >= 0 && nz < strData.Size.Z)
                                    {
                                        int nState = strData.voxelGrid[nx, ny, nz];
                                        if (nState == -2)
                                        {
                                            drawFace = false;
                                        }
                                        else if (nState >= 0 && !IsAirOrInvisible(strData.palette[nState].Name))
                                        {
                                            var nCuboids = _nativeModelResolverService.ResolveGeometry(strData.palette[nState].Name, strData.palette[nState].Properties);
                                            bool nIsSolid = (nCuboids.Count == 1 && nCuboids[0].from == Vector3.Zero && nCuboids[0].to == new Vector3(16, 16, 16));
                                            if (nIsSolid)
                                            {
                                                drawFace = false;
                                            }
                                        }
                                    }
                                }

                                if (drawFace)
                                {
                                    int i0 = VoxelData.FaceTriangles[f][0];
                                    int i1 = VoxelData.FaceTriangles[f][1];
                                    int i2 = VoxelData.FaceTriangles[f][2];
                                    int i3 = VoxelData.FaceTriangles[f][3];

                                    float x0 = (x + localVertices[i0].X) * scale;
                                    float y0 = (y + localVertices[i0].Y) * scale;
                                    float z0 = (z + localVertices[i0].Z) * scale;

                                    float x1 = (x + localVertices[i1].X) * scale;
                                    float y1 = (y + localVertices[i1].Y) * scale;
                                    float z1 = (z + localVertices[i1].Z) * scale;

                                    float x2 = (x + localVertices[i2].X) * scale;
                                    float y2 = (y + localVertices[i2].Y) * scale;
                                    float z2 = (z + localVertices[i2].Z) * scale;

                                    float x3 = (x + localVertices[i3].X) * scale;
                                    float y3 = (y + localVertices[i3].Y) * scale;
                                    float z3 = (z + localVertices[i3].Z) * scale;

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
            }
            return triangles;
        }
        private static Vector3[] GetCuboidVertices(Vector3 from, Vector3 to)
        {
            Vector3 min = from / 16f;
            Vector3 max = to / 16f;

            return
            [
                new(min.X, min.Y, min.Z), // 0: Back-Left-Bottom
                new(max.X, min.Y, min.Z), // 1: Back-Right-Bottom
                new(max.X, max.Y, min.Z), // 2: Back-Right-Top
                new(min.X, max.Y, min.Z), // 3: Back-Left-Top
                new(min.X, min.Y, max.Z), // 4: Front-Left-Bottom
                new(max.X, min.Y, max.Z), // 5: Front-Right-Bottom
                new(max.X, max.Y, max.Z), // 6: Front-Right-Top
                new(min.X, max.Y, max.Z)  // 7: Front-Left-Top
            ];
        }

        public static List<(Vector3 from, Vector3 to)> ParseSignature(string signature)
        {
            // 1. Cortamos el string cada vez que haya un '|'.
            // StringSplitOptions.RemoveEmptyEntries ignora el pedacito vacío que queda al final.
            string[] boxes = signature.Split('|', StringSplitOptions.RemoveEmptyEntries);

            // Preparamos nuestro arreglo de Cuboides (sabemos el tamaño exacto gracias a boxes.Length)
            List<(Vector3 from, Vector3 to)> geometry = new();

            for (int i = 0; i < boxes.Length; i++)
            {
                // En este punto, boxes[i] se ve así: "[0,0,0=>16,8.5,16]"

                // 2. Limpiamos los corchetes
                string cleanBox = boxes[i].Replace("[", "").Replace("]", "");
                // Ahora es: "0,0,0=>16,8.5,16"

                // 3. Cortamos a la mitad usando el '=>'
                string[] vectors = cleanBox.Split("=>");

                // 4. Convertimos el texto en vectores reales
                Vector3 from = ParseToVector3(vectors[0]);
                Vector3 to = ParseToVector3(vectors[1]);

                // 5. Lo guardamos en nuestro arreglo
                geometry.Add((from, to));
            }

            return geometry;
        }

        // Función auxiliar para no repetir código al leer X, Y, Z
        private static Vector3 ParseToVector3(string coords)
        {
            // coords se ve así: "16,8.5,16"
            string[] xyz = coords.Split(',');

            // Usamos InvariantCulture para que C# entienda que el punto (8.5) es el decimal,
            // sin importar si la PC del usuario está en español o en inglés.
            float x = float.Parse(xyz[0], CultureInfo.InvariantCulture);
            float y = float.Parse(xyz[1], CultureInfo.InvariantCulture);
            float z = float.Parse(xyz[2], CultureInfo.InvariantCulture);

            return new Vector3(x, y, z);
        }

        public List<Triangle> GenerateMesh(StructureData strData, float scale)
        {
            List<Triangle> triangles = new();

            for (int x = 0; x < strData.Size.X; x++)
            {
                for (int y = 0; y < strData.Size.Y; y++)
                {
                    for (int z = 0; z < strData.Size.Z; z++)
                    {
                        int blockState = strData.voxelGrid[x, y, z];

                        if (blockState < -2 || blockState == -1 || (blockState >= 0 && IsAirOrInvisible(strData.palette[blockState].Name))) continue;

                        for (int f = 0; f < 6; f++)
                        {
                            int nx = x + VoxelData.NeighborOffsets[f][0];
                            int ny = y + VoxelData.NeighborOffsets[f][1];
                            int nz = z + VoxelData.NeighborOffsets[f][2];

                            // Face culling
                            bool drawFace = false;
                            if (nx < 0 || nx >= strData.Size.X || ny < 0 || ny >= strData.Size.Y || nz < 0 || nz >= strData.Size.Z)
                            {
                                drawFace = true;
                            }
                            else
                            {
                                int nState = strData.voxelGrid[nx, ny, nz];
                                if (nState == -1 || (nState >= 0 && IsAirOrInvisible(strData.palette[nState].Name)))
                                {
                                    drawFace = true;
                                }
                            }

                            if (drawFace)
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
        public Dictionary<Color, List<Triangle>> GenerateMultiColorMeshes(Dictionary<Color, StructureData> multiData, float scale, bool useFullGeom = false)
        {
            var meshes = new Dictionary<Color, List<Triangle>>();
            foreach (var kvp in multiData)
            {
                var color = kvp.Key;
                var colorStrData = kvp.Value;

                if (useFullGeom)
                {
                    meshes[color] = GenerateFullGeometryMesh(colorStrData, scale);
                }
                else
                {
                    meshes[color] = GenerateMesh(colorStrData, scale);
                }
            }
            return meshes;
        }
    }
}
