using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MCto3D.Services
{
    public class ColorClusteringService : IColorClusteringService
    {
        private readonly IColorMappingService _colorMappingService;

        public ColorClusteringService(IColorMappingService colorMappingService)
        {
            _colorMappingService = colorMappingService;
        }

        public Dictionary<int, Color> ClusterByKMeans(BlockState[] palette, int k, bool useRealColors = false)
        {
            var blockColors = new Dictionary<int, Color>();
            for (int i = 0; i < palette.Length; i++)
            {
                blockColors[i] = _colorMappingService.GetColorForBlock(palette[i].Name, palette[i].Properties);
            }

            var uniqueColors = blockColors.Values.Distinct().ToList();
            
            if (uniqueColors.Count <= k)
            {
                return blockColors;
            }

            var random = new Random(42); // Seeded for deterministic output
            var centroids = uniqueColors.OrderBy(x => random.Next()).Take(k).Select(c => new double[] { c.R, c.G, c.B }).ToList();

            var assignments = new Dictionary<Color, int>();
            bool changed = true;
            int maxIterations = 50;

            while (changed && maxIterations-- > 0)
            {
                changed = false;
                foreach (var c in uniqueColors)
                {
                    double minDistance = double.MaxValue;
                    int bestCentroid = 0;
                    for (int i = 0; i < k; i++)
                    {
                        double dist = GetDistance(c, centroids[i]);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestCentroid = i;
                        }
                    }
                    
                    if (!assignments.ContainsKey(c) || assignments[c] != bestCentroid)
                    {
                        assignments[c] = bestCentroid;
                        changed = true;
                    }
                }

                for (int i = 0; i < k; i++)
                {
                    var assignedColors = uniqueColors.Where(c => assignments.ContainsKey(c) && assignments[c] == i).ToList();
                    if (assignedColors.Count > 0)
                    {
                        centroids[i][0] = assignedColors.Average(c => c.R);
                        centroids[i][1] = assignedColors.Average(c => c.G);
                        centroids[i][2] = assignedColors.Average(c => c.B);
                    }
                }
            }

            var finalCentroids = centroids.Select(arr => Color.FromArgb(255, (int)arr[0], (int)arr[1], (int)arr[2])).ToList();

            if (useRealColors)
            {
                for (int i = 0; i < finalCentroids.Count; i++)
                {
                    var centroid = finalCentroids[i];
                    double minDistance = double.MaxValue;
                    Color bestReal = uniqueColors[0];
                    foreach (var realC in uniqueColors)
                    {
                        double d = GetDistance(realC, new double[] { centroid.R, centroid.G, centroid.B });
                        if (d < minDistance)
                        {
                            minDistance = d;
                            bestReal = realC;
                        }
                    }
                    finalCentroids[i] = bestReal;
                }
            }

            var resultMap = new Dictionary<int, Color>();
            foreach(var kvp in blockColors)
            {
                int index = kvp.Key;
                Color original = kvp.Value;
                int centroidIdx = assignments.ContainsKey(original) ? assignments[original] : 0;
                resultMap[index] = finalCentroids[centroidIdx];
            }

            return resultMap;
        }

        public Dictionary<int, Color> ClusterByPalette(BlockState[] palette, List<Color> userColors)
        {
            var resultMap = new Dictionary<int, Color>();
            
            if (userColors == null || userColors.Count == 0)
                userColors = new List<Color> { Color.Gray };
                
            for (int i = 0; i < palette.Length; i++)
            {
                Color original = _colorMappingService.GetColorForBlock(palette[i].Name, palette[i].Properties);
                
                double minDistance = double.MaxValue;
                Color bestColor = userColors[0];
                
                foreach(var uc in userColors)
                {
                    double dist = GetDistance(original, new double[]{ uc.R, uc.G, uc.B });
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestColor = uc;
                    }
                }
                
                resultMap[i] = bestColor;
            }
            
            return resultMap;
        }

        public List<Color> GetPredefinedPalette(int paletteIndex)
        {
            var predefinedColors = new List<Color>
            {
                Color.FromArgb(255, 255, 255), // Blanco
                Color.FromArgb(0, 0, 0),       // Negro
                Color.FromArgb(255, 0, 0),     // Rojo
                Color.FromArgb(0, 255, 0),     // Verde
                Color.FromArgb(0, 0, 255),     // Azul
                Color.FromArgb(255, 255, 0),   // Amarillo
                Color.FromArgb(0, 255, 255),   // Cian
                Color.FromArgb(255, 0, 255),   // Magenta
                Color.FromArgb(128, 128, 128), // Gris
                Color.FromArgb(192, 192, 192), // GrisClaro
                Color.FromArgb(64, 64, 64),    // GrisOscuro
                Color.FromArgb(255, 165, 0),   // Naranja
                Color.FromArgb(165, 42, 42),   // Marron
                Color.FromArgb(255, 192, 203), // Rosa
                Color.FromArgb(128, 0, 128),   // Purpura
                Color.FromArgb(50, 205, 50),   // VerdeLima
                Color.FromArgb(135, 206, 235), // Celeste
                Color.FromArgb(255, 215, 0),   // Dorado
                Color.FromArgb(245, 245, 220), // Beige
                Color.FromArgb(128, 128, 0),   // VerdeOliva
                Color.FromArgb(0, 0, 128),     // AzulMarino
                Color.FromArgb(0, 128, 128),   // Teal
                Color.FromArgb(64, 224, 208),  // Turquesa
                Color.FromArgb(230, 230, 250), // Lavanda
                Color.FromArgb(210, 180, 140), // MarronClaro
                Color.FromArgb(101, 67, 33),   // MarronOscuro
                Color.FromArgb(250, 128, 114), // Salmon
                Color.FromArgb(128, 0, 0),     // Granate
                Color.FromArgb(255, 127, 80),  // Coral
                Color.FromArgb(253, 245, 230), // BlancoHueso
                Color.FromArgb(255, 218, 185), // Durazno
                Color.FromArgb(152, 255, 152), // Menta
                Color.FromArgb(220, 20, 60)    // Carmesi
            };
            if (paletteIndex > 0 && paletteIndex <= predefinedColors.Count)
            {
                return predefinedColors.Take(paletteIndex).ToList();
            }
            return predefinedColors;
        }

        private static double GetDistance(Color a, double[] b)
        {
            double dR = a.R - b[0];
            double dG = a.G - b[1];
            double dB = a.B - b[2];
            return Math.Sqrt(dR * dR + dG * dG + dB * dB);
        }
    }
}

