using fNbt;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using Tmds.DBus.Protocol;

namespace MCto3D.Services
{
    public struct BlockState
    {
        public string Name; // ej: "minecraft:acacia_stairs"
        public Dictionary<string, string> Properties; // ej: ["facing"] = "east", ["half"] = "bottom"

        // Un método útil para cuando necesites buscar este bloque en tu caché de Blockstates
        public string GetPropertiesString()
        {
            if (Properties == null || Properties.Count == 0) return "";

            List<string> props = new List<string>();
            foreach (var kvp in Properties)
            {
                props.Add($"{kvp.Key}={kvp.Value}");
            }
            return string.Join(",", props);
        }
    }
    public struct structureData
    {
        public Vector3 Size;
        public int[,,] voxelGrid;
        public BlockState[] palette;

        public structureData(Vector3 size, int[,,] voxelGrid, BlockState[] palette)
        {
            Size = size;
            this.voxelGrid = voxelGrid;
            this.palette = palette;
        }
    }

    public struct multiColorStructureData
    {
        public Vector3 Size;
        public Dictionary<System.Drawing.Color, int[,,]> voxelGrid;
        public BlockState[] palette;
        public multiColorStructureData(Vector3 size, Dictionary<System.Drawing.Color, int[,,]> voxelGrid, BlockState[] palette)
        {
            Size = size;
            this.voxelGrid = voxelGrid;
            this.palette = palette;
        }
    }
    public class FileReader_Service
    {
        public static structureData readNBT(string filePath)
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
            for (int ix = 0; ix < sizeX; ix++)
                for (int iy = 0; iy < sizeY; iy++)
                    for (int iz = 0; iz < sizeZ; iz++)
                        voxelGrid[ix, iy, iz] = -1;

            foreach (NbtCompound blockTag in blocks)
            {
                var pos = blockTag.Get<NbtList>("pos");
                int stateId = blockTag.Get<NbtInt>("state").Value;

                int x = pos[0].IntValue;
                int y = pos[1].IntValue;
                int z = pos[2].IntValue;

                voxelGrid[x, y, z] = stateId;
            }
            BlockState[] customPalette = new BlockState[palette.Count];
            for (int i = 0; i < palette.Count; i++)
            {
                var tag = palette[i] as NbtCompound;
                string blockName = tag.Get<NbtString>("Name").Value;

                Dictionary<string, string> properties = new Dictionary<string, string>();

                // Algunos bloques (como la piedra) no tienen "Properties", por eso revisamos si existe
                if (tag.TryGet("Properties", out NbtCompound propsTag))
                {
                    foreach (NbtTag prop in propsTag)
                    {
                        properties.Add(prop.Name, prop.StringValue);
                    }
                }
                customPalette[i] = new BlockState
                {
                    Name = blockName,
                    Properties = properties
                };
            }
            return new structureData(new(sizeX, sizeY, sizeZ), voxelGrid, customPalette);
        }

        public static structureData readLitematic(string filePath)
        {
            var myFile = new NbtFile();
            myFile.LoadFromFile(filePath);
            var rootTag = myFile.RootTag;

            var regions = rootTag.Get<NbtCompound>("Regions");
            if (regions == null || regions.Count == 0)
            {
                throw new Exception("El archivo Litematica no contiene regiones.");
            }

            // Calcular el bounding box global
            int minX = int.MaxValue; int minY = int.MaxValue; int minZ = int.MaxValue;
            int maxX = int.MinValue; int maxY = int.MinValue; int maxZ = int.MinValue;

            foreach (NbtCompound region in regions.Tags)
            {
                var posTag = region.Get<NbtCompound>("Position");
                var sizeTag = region.Get<NbtCompound>("Size");

                int px = posTag.Get<NbtInt>("x").Value;
                int py = posTag.Get<NbtInt>("y").Value;
                int pz = posTag.Get<NbtInt>("z").Value;

                int sx = sizeTag.Get<NbtInt>("x").Value;
                int sy = sizeTag.Get<NbtInt>("y").Value;
                int sz = sizeTag.Get<NbtInt>("z").Value;

                int rx = sx < 0 ? px + sx : px;
                int ry = sy < 0 ? py + sy : py;
                int rz = sz < 0 ? pz + sz : pz;

                int width = Math.Abs(sx);
                int height = Math.Abs(sy);
                int length = Math.Abs(sz);

                minX = Math.Min(minX, rx);
                minY = Math.Min(minY, ry);
                minZ = Math.Min(minZ, rz);

                maxX = Math.Max(maxX, rx + width);
                maxY = Math.Max(maxY, ry + height);
                maxZ = Math.Max(maxZ, rz + length);
            }

            int totalSizeX = maxX - minX;
            int totalSizeY = maxY - minY;
            int totalSizeZ = maxZ - minZ;

            if (totalSizeX == 0 || totalSizeY == 0 || totalSizeZ == 0)
            {
                throw new Exception("Las dimensiones del archivo Litematica son inválidas.");
            }

            int[,,] voxelGrid = new int[totalSizeX, totalSizeY, totalSizeZ];
            for (int ix = 0; ix < totalSizeX; ix++)
                for (int iy = 0; iy < totalSizeY; iy++)
                    for (int iz = 0; iz < totalSizeZ; iz++)
                        voxelGrid[ix, iy, iz] = -1;

            List<BlockState> globalPalette = new List<BlockState>();
            Dictionary<string, int> paletteMap = new Dictionary<string, int>();

            foreach (NbtCompound region in regions.Tags)
            {
                var posTag = region.Get<NbtCompound>("Position");
                var sizeTag = region.Get<NbtCompound>("Size");

                int px = posTag.Get<NbtInt>("x").Value;
                int py = posTag.Get<NbtInt>("y").Value;
                int pz = posTag.Get<NbtInt>("z").Value;

                int sx = sizeTag.Get<NbtInt>("x").Value;
                int sy = sizeTag.Get<NbtInt>("y").Value;
                int sz = sizeTag.Get<NbtInt>("z").Value;

                int rx = sx < 0 ? px + sx : px;
                int ry = sy < 0 ? py + sy : py;
                int rz = sz < 0 ? pz + sz : pz;

                int width = Math.Abs(sx);
                int height = Math.Abs(sy);
                int length = Math.Abs(sz);

                var blockStatePalette = region.Get<NbtList>("BlockStatePalette");
                if (blockStatePalette == null) continue;

                int[] localToGlobalMap = new int[blockStatePalette.Count];
                for (int i = 0; i < blockStatePalette.Count; i++)
                {
                    var stateTag = blockStatePalette[i] as NbtCompound;
                    string name = stateTag.Get<NbtString>("Name").Value;
                    Dictionary<string, string> properties = new Dictionary<string, string>();

                    if (stateTag.TryGet("Properties", out NbtCompound propsTag))
                    {
                        foreach (NbtTag prop in propsTag)
                        {
                            properties.Add(prop.Name, prop.StringValue);
                        }
                    }

                    BlockState bs = new BlockState { Name = name, Properties = properties };
                    string key = name + bs.GetPropertiesString();
                    if (!paletteMap.ContainsKey(key))
                    {
                        paletteMap[key] = globalPalette.Count;
                        globalPalette.Add(bs);
                    }
                    localToGlobalMap[i] = paletteMap[key];
                }

                // Si no hay array de bloques, tal vez esté vacío (todo aire)
                if (!region.TryGet("BlockStates", out NbtTag blockStatesTag))
                {
                    continue;
                }

                long[] blockStatesArray = null;
                if (blockStatesTag is NbtLongArray longArrayTag)
                {
                    blockStatesArray = longArrayTag.Value;
                }
                else
                {
                    // Fallback in case the library parses it as something else or we have to extract it differently
                    throw new Exception("El tag BlockStates no es un NbtLongArray.");
                }

                int bitsPerBlock = Math.Max(2, (int)Math.Ceiling(Math.Log(blockStatePalette.Count, 2)));
                long maxVal = (1L << bitsPerBlock) - 1;

                int volume = width * height * length;
                int blocksPerLong = 64 / bitsPerBlock;
                
                int expectedTightlyPackedLength = (int)Math.Ceiling((volume * bitsPerBlock) / 64.0);
                int expectedPaddedLength = (int)Math.Ceiling((double)volume / blocksPerLong);
                
                // Si la longitud coincide más con el empaquetado apretado, usamos ese formato.
                bool isTightlyPacked = blockStatesArray.Length == expectedTightlyPackedLength || blockStatesArray.Length < expectedPaddedLength;
                
                for (int i = 0; i < volume; i++)
                {
                    int localId = 0;
                    if (isTightlyPacked)
                    {
                        int startBit = i * bitsPerBlock;
                        int longIndex = startBit / 64;
                        int bitOffset = startBit % 64;
                        
                        if (longIndex < blockStatesArray.Length)
                        {
                            ulong ul = (ulong)blockStatesArray[longIndex];
                            localId = (int)((ul >> bitOffset) & (ulong)maxVal);
                            
                            // Check si los bits cruzan el límite del long
                            if (bitOffset + bitsPerBlock > 64 && longIndex + 1 < blockStatesArray.Length)
                            {
                                int bitsInFirst = 64 - bitOffset;
                                int bitsInSecond = bitsPerBlock - bitsInFirst;
                                ulong ul2 = (ulong)blockStatesArray[longIndex + 1];
                                ulong maxVal2 = (1UL << bitsInSecond) - 1;
                                localId |= (int)((ul2 & maxVal2) << bitsInFirst);
                            }
                        }
                    }
                    else
                    {
                        int longIndex = i / blocksPerLong;
                        if (longIndex < blockStatesArray.Length)
                        {
                            int bitOffset = (i % blocksPerLong) * bitsPerBlock;
                            ulong ul = (ulong)blockStatesArray[longIndex];
                            localId = (int)((ul >> bitOffset) & (ulong)maxVal);
                        }
                    }
                    
                    if (localId < 0 || localId >= localToGlobalMap.Length)
                        continue;
                        
                    int globalId = localToGlobalMap[localId];
                    
                    int localY = i / (width * length);
                    int localZ = (i % (width * length)) / width;
                    int localX = (i % (width * length)) % width;
                    
                    int gridX = (rx - minX) + localX;
                    int gridY = (ry - minY) + localY;
                    int gridZ = (rz - minZ) + localZ;
                    
                    // Solo guardar si no es aire (asumiendo que el ID 0 de Litematica siempre es aire temporal, o simplemente comprobando el nombre)
                    if (globalPalette[globalId].Name != "minecraft:air")
                    {
                        voxelGrid[gridX, gridY, gridZ] = globalId;
                    }
                }
            }

            // Filtrar aire de la paleta si es necesario, o mantenerlo. 
            // Para mantener coherencia con el readNBT, retornamos la estructura
            return new structureData(new(totalSizeX, totalSizeY, totalSizeZ), voxelGrid, globalPalette.ToArray());
        }

        public static multiColorStructureData CreateMultiColorData(structureData strData, int? k, List<System.Drawing.Color> userColors, bool useKMedoids = false, bool useRawColors = false, bool fillHoles = false, bool useCustomFillColor = false, System.Drawing.Color customFillColor = default)
        {
            Dictionary<int, System.Drawing.Color> indexToColorMap;
            
            if (useRawColors)
            {
                indexToColorMap = new Dictionary<int, System.Drawing.Color>();
                for (int i = 0; i < strData.palette.Length; i++)
                {
                    indexToColorMap[i] = ColorMapping_Service.GetColorForBlock(strData.palette[i].Name, strData.palette[i].Properties);
                }
            }
            else if (k.HasValue)
            {
                indexToColorMap = ColorClustering_Service.ClusterByKMeans(strData.palette, k.Value, useKMedoids);
            }
            else
            {
                indexToColorMap = ColorClustering_Service.ClusterByPalette(strData.palette, userColors);
            }
            
            var voxelGrids = new Dictionary<System.Drawing.Color, int[,,]>();
            
            System.Drawing.Color fillColor = System.Drawing.Color.Gray;
            if (fillHoles)
            {
                if (useCustomFillColor)
                {
                    fillColor = customFillColor;
                }
                else
                {
                    // Find mode color (most used) by analyzing grid
                    var counts = new Dictionary<System.Drawing.Color, int>();
                    for(int x=0; x<strData.Size.X; x++)
                    {
                        for(int y=0; y<strData.Size.Y; y++)
                        {
                            for(int z=0; z<strData.Size.Z; z++)
                            {
                                int st = strData.voxelGrid[x,y,z];
                                if (st >= 0 && indexToColorMap.ContainsKey(st))
                                {
                                    var c = indexToColorMap[st];
                                    if(!counts.ContainsKey(c)) counts[c] = 0;
                                    counts[c]++;
                                }
                            }
                        }
                    }
                    int maxCount = -1;
                    foreach(var kvp in counts)
                    {
                        if(kvp.Value > maxCount)
                        {
                            maxCount = kvp.Value;
                            fillColor = kvp.Key;
                        }
                    }
                }
                
                // Ensure fill color grid exists
                if (!voxelGrids.ContainsKey(fillColor) && !indexToColorMap.ContainsValue(fillColor))
                {
                    // We need to add it to the grids dictionary
                    var grid = new int[(int)strData.Size.X, (int)strData.Size.Y, (int)strData.Size.Z];
                    for (int ix = 0; ix < strData.Size.X; ix++)
                        for (int iy = 0; iy < strData.Size.Y; iy++)
                            for (int iz = 0; iz < strData.Size.Z; iz++)
                                grid[ix, iy, iz] = -1;
                    voxelGrids[fillColor] = grid;
                }
            }
            
            foreach(var color in System.Linq.Enumerable.Distinct(indexToColorMap.Values))
            {
                var grid = new int[(int)strData.Size.X, (int)strData.Size.Y, (int)strData.Size.Z];
                for (int ix = 0; ix < strData.Size.X; ix++)
                    for (int iy = 0; iy < strData.Size.Y; iy++)
                        for (int iz = 0; iz < strData.Size.Z; iz++)
                            grid[ix, iy, iz] = -1;
                            
                voxelGrids[color] = grid;
            }
            
            for (int ix = 0; ix < strData.Size.X; ix++)
            {
                for (int iy = 0; iy < strData.Size.Y; iy++)
                {
                    for (int iz = 0; iz < strData.Size.Z; iz++)
                    {
                        int blockState = strData.voxelGrid[ix, iy, iz];
                        if (blockState >= 0)
                        {
                            var color = indexToColorMap[blockState];
                            voxelGrids[color][ix, iy, iz] = blockState;
                        }
                        else if (blockState == -2 && fillHoles)
                        {
                            voxelGrids[fillColor][ix, iy, iz] = -2; // Keep -2 so Mesh_Service handles it natively
                        }
                    }
                }
            }
            
            return new multiColorStructureData(strData.Size, voxelGrids, strData.palette);
        }
    }
}
