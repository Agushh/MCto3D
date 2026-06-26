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
        public Dictionary<byte[], int[,,]> voxelGrid;
        public BlockState[] palette;
        public multiColorStructureData(Vector3 size, Dictionary<byte[], int[,,]> voxelGrid, BlockState[] palette)
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


    }


}
