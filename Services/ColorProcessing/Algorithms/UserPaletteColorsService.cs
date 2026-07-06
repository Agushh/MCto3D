using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MCto3D.Services.ColorProcesing.Algorithms
{
    internal class UserPaletteColorsService : IColorAlgorithmService
    {
        public Dictionary<int, Color> Process(Dictionary<int, Color> palette, int k = 0, List<Color>? UserPalette = null)
        {
            var resultMap = new Dictionary<int, Color>();

            if (UserPalette == null || UserPalette.Count == 0)
                UserPalette = new List<Color> { Color.Gray };

            for (int i = 0; i < palette.Count; i++)
            {
                Color original = palette[i];

                double minDistance = double.MaxValue;
                Color bestColor = UserPalette[0];

                foreach (var uc in UserPalette)
                {
                    double dist = GetDistance(original, new double[] { uc.R, uc.G, uc.B });
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
        private static double GetDistance(Color a, double[] b)
        {
            double dR = a.R - b[0];
            double dG = a.G - b[1];
            double dB = a.B - b[2];
            return Math.Sqrt(dR * dR + dG * dG + dB * dB);
        }
    }
}
