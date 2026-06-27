using System.Collections.Generic;
using System.Drawing;

namespace MCto3D.Services
{
    public interface IColorMappingService
    {
        Dictionary<string, byte[]> BlockColors { get; set; }
        Color GetColorForBlock(string blockName, Dictionary<string, string> properties = null);
        void Initialize(string localFilesPath);
    }
}
