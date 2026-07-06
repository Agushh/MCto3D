using System.Collections.Generic;
using System.Drawing;

namespace MCto3D.Services.ColorProcesing.Algorithms
{
    public class RawColorService : IColorAlgorithmService
    {
        public Dictionary<int, Color> Process(Dictionary<int, Color> palette, int k = 0, List<Color>? UserPalette = null)
        {
            // Devuelve la paleta cruda tal cual, sin modificaciones
            return new Dictionary<int, Color>(palette);
        }
    }
}
