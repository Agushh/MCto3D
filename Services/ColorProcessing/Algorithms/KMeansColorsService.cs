using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MCto3D.Services.ColorProcesing.Algorithms
{
    internal class KMeansColorsService : IColorAlgorithmService
    {
        //indice de blockstate representa ID del bloque, luego incluye el nombre y la lista de parametros de ese id
        public Dictionary<int, Color> Process(Dictionary<int, Color> palette, int k = 0, List<Color>? UserPalette = null)
        {
            var uniqueColors = palette.Values.Distinct().ToList();
            
            if (uniqueColors.Count <= k)
            {
                return palette;
            }

            var random = new Random(42); // Seeded for deterministic output
            
            //centroid guarda array centroid[i] donde i es el ID del centroide y la posicion es el color. el color es un array de tipo [0] r, [1] g, [2] b.

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

            var finalCentroids = centroids.Select(arr => Color.FromArgb(255, (int)arr[0], (int)arr[1], (int)arr[2])).ToArray();

            var resultMap = new Dictionary<int, Color>();
            foreach (var kvp in palette)
            {
                int index = kvp.Key;
                Color original = kvp.Value;
                int centroidIdx = assignments.ContainsKey(original) ? assignments[original] : 0;
                resultMap[index] = finalCentroids[centroidIdx];
            }

            return resultMap;
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
