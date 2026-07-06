using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MCto3D.Services.ColorProcesing
{
    public interface IColorAlgorithmService
    {
        public Dictionary<int, Color> Process(Dictionary<int, Color> palette, int k = 0, List<Color>? UserPalette = null);
    }
}
