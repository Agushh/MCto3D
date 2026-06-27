using System.Collections.Generic;
using System.Drawing;

namespace MCto3D.Services
{
    public interface IColorClusteringService
    {
        Dictionary<int, Color> ClusterByKMeans(BlockState[] palette, int k, bool useRealColors = false);
        Dictionary<int, Color> ClusterByPalette(BlockState[] palette, List<Color> userColors);
        List<Color> GetPredefinedPalette(int paletteIndex);
    }
}
