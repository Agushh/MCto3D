using fNbt;
using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MCto3D.Services.FileReading
{
    internal class LitematicReaderService : IStructureReader
    {
        public bool CanRead(string filePath)
        {
            return filePath.EndsWith(".litematic", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".schematic", StringComparison.OrdinalIgnoreCase);
        }

        public StructureData Read(string filePath)
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
                    string rawName = stateTag.Get<NbtString>("Name").Value;
                    string name = rawName.Split('[')[0].Replace("minecraft:", "");
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

            

            return new StructureData(new(totalSizeX, totalSizeY, totalSizeZ), voxelGrid, globalPalette.ToArray());
        }
    }
}
