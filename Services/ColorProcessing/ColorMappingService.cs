using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using MCto3D.Models;
using MCto3D.Services.AssetsProcessing;

namespace MCto3D.Services.ColorProcesing;

public class ColorMappingService
{
    public Dictionary<string, byte[]> BlockColors { get; set; } = new();
    private Dictionary<string, Color> _computedColorsCache = new();
    
    private readonly NativeModelResolverService _nativeModelResolverService;

    public ColorMappingService(NativeModelResolverService nativeModelResolverService)
    {
        _nativeModelResolverService = nativeModelResolverService;
    }

    public void Initialize(string localFilesPath)
    {
    }

    public Dictionary<int, Color> GetColorsForModel(BlockState[] palette)
    {
        var blockColors = new Dictionary<int, Color>();
        for (int i = 0; i < palette.Length; i++)
        {
            blockColors[i] = GetColorForBlock(palette[i].Name, palette[i].Properties);
        }
        return blockColors;
    }

    public Color GetColorForBlock(string blockName, Dictionary<string, string> properties = null)
    {
        string cleanName = blockName.Split('[')[0].Replace("minecraft:", "");
        string propString = properties != null && properties.Count > 0 
            ? string.Join(",", properties.Select(kv => $"{kv.Key}={kv.Value}").OrderBy(x => x)) 
            : "normal";
        string cacheKey = $"{cleanName}[{propString}]";

        if (_computedColorsCache.TryGetValue(cacheKey, out Color cachedColor))
            return cachedColor;

        // Bloques cuyo color semántico está únicamente en la textura _top
        // (grass_block, podzol, mycelium, dirt_path mezclan dirt en sus modelos)
        string? topOnlyOverride = cleanName switch
        {
            "grass_block" => "grass_block_top",
            "podzol"      => "podzol_top",
            "mycelium"    => "mycelium_top",
            "dirt_path"   => "dirt_path_top",
            _             => null
        };

        if (topOnlyOverride != null && BlockColors.TryGetValue(topOnlyOverride, out byte[]? topRgb) && topRgb.Length == 3)
        {
            Color c = Color.FromArgb(255, topRgb[0], topRgb[1], topRgb[2]);
            _computedColorsCache[cacheKey] = c;
            return c;
        }

        // Intentar resolver colores por texturas JSON
        List<string> textures = _nativeModelResolverService.GetTexturesForBlock(blockName, properties);
        if (textures.Count > 0)
        {
            long totalR = 0, totalG = 0, totalB = 0;
            int validCount = 0;

            foreach(var tex in textures)
            {
                if (BlockColors.TryGetValue(tex, out byte[]? rgb) && rgb.Length == 3)
                {
                    totalR += (rgb[0] * rgb[0]);
                    totalG += (rgb[1] * rgb[1]);
                    totalB += (rgb[2] * rgb[2]);
                    validCount++;
                }
            }

            if (validCount > 0)
            {
                int avgR = (int)Math.Sqrt(totalR / validCount);
                int avgG = (int)Math.Sqrt(totalG / validCount);
                int avgB = (int)Math.Sqrt(totalB / validCount);
                Color result = Color.FromArgb(255, avgR, avgG, avgB);
                _computedColorsCache[cacheKey] = result;
                return result;
            }
        }

        // Mapeo de bloques antiguo (Fallback si JSON falla)
        string textureName = cleanName;
        if (cleanName == "grass_block") textureName = "grass_block_top";
        else if (cleanName == "dirt_path") textureName = "dirt_path_top";
        else if (cleanName == "podzol") textureName = "podzol_top";
        else if (cleanName == "mycelium") textureName = "mycelium_top";
        else if (cleanName == "snow") textureName = "snow";
        else if (cleanName.EndsWith("_stairs")) textureName = cleanName.Replace("_stairs", "");
        else if (cleanName.EndsWith("_slab")) textureName = cleanName.Replace("_slab", "");
        else if (cleanName.EndsWith("_wood")) textureName = cleanName.Replace("_wood", "_log");
        else if (cleanName.EndsWith("_hyphae")) textureName = cleanName.Replace("_hyphae", "_stem");

        if (BlockColors.TryGetValue(textureName, out byte[]? fRgb) && fRgb.Length == 3)
        {
            Color c = Color.FromArgb(255, fRgb[0], fRgb[1], fRgb[2]);
            _computedColorsCache[cacheKey] = c;
            return c;
        }
        
        // Segundo intento de fallback general
        if (BlockColors.TryGetValue(cleanName + "_top", out byte[]? rgbTop) && rgbTop.Length == 3)
        {
            Color c = Color.FromArgb(255, rgbTop[0], rgbTop[1], rgbTop[2]);
            _computedColorsCache[cacheKey] = c;
            return c;
        }

        _computedColorsCache[cacheKey] = Color.Gray;
        return Color.Gray; // Default fallback
    }
}

