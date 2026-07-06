using System.Collections.Generic;
using System.Drawing;

namespace MCto3D.Services.ColorProcesing.Algorithms
{
    public class SingleColorService : IColorAlgorithmService
    {
        public Dictionary<int, Color> Process(Dictionary<int, Color> palette, int k = 0, List<Color>? UserPalette = null)
        {
            var resultMap = new Dictionary<int, Color>();
            foreach (var key in palette.Keys)
            {
                resultMap[key] = Color.Gray;
            }
            return resultMap;
        }
    }
}
