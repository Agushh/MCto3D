using MCto3D.Models;
using MCto3D.Services.ColorProcesing.Factory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MCto3D.Services.ColorProcesing
{
    public class ColorSeparatorService
    {
        private readonly ColorMappingService _colorMappingService;
        private readonly IColorAlgorithmFactory _colorAlgorithmFactory;

        public ColorSeparatorService(ColorMappingService colorMappingService, IColorAlgorithmFactory colorAlgorithmFactory)
        {
            _colorMappingService = colorMappingService;
            _colorAlgorithmFactory = colorAlgorithmFactory;
        }

        public Dictionary<Color, StructureData> SeparateByColor(StructureData strData, int k,List<Color> userColors, ColorAlgorithm algorithm, bool fillHoles = false,bool useCustomFillColor = false, Color customFillColor = default)
        {
            var algorithmService = _colorAlgorithmFactory.GetAlgorithm(algorithm);
            Dictionary<int, Color> palette = _colorMappingService.GetColorsForModel(strData.palette);

            Dictionary<int, Color> indexToColorMap = algorithmService.Process(palette, k, userColors);

            var voxelGrids = new Dictionary<Color, int[,,]>();

            Color fillColor = Color.Gray;
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
                    for (int x = 0; x < strData.Size.X; x++)
                    {
                        for (int y = 0; y < strData.Size.Y; y++)
                        {
                            for (int z = 0; z < strData.Size.Z; z++)
                            {
                                int st = strData.voxelGrid[x, y, z];
                                if (st >= 0 && indexToColorMap.ContainsKey(st))
                                {
                                    var c = indexToColorMap[st];
                                    if (!counts.ContainsKey(c)) counts[c] = 0;
                                    counts[c]++;
                                }
                            }
                        }
                    }
                    int maxCount = -1;
                    foreach (var kvp in counts)
                    {
                        if (kvp.Value > maxCount)
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

            foreach (var color in System.Linq.Enumerable.Distinct(indexToColorMap.Values))
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
                            voxelGrids[fillColor][ix, iy, iz] = -2; // Keep -2 so MeshService handles it natively
                        }
                    }
                }
            }

            var separatedStructures = new Dictionary<Color, StructureData>();
            foreach (var kvp in voxelGrids)
            {
                separatedStructures[kvp.Key] = new StructureData(strData.Size, kvp.Value, strData.palette);
            }

            return separatedStructures;
        }
    }

}
