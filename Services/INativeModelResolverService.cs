using System.Collections.Generic;
using System.Numerics;

namespace MCto3D.Services
{
    public interface INativeModelResolverService
    {
        void ClearCache();
        List<(Vector3 from, Vector3 to)> ResolveGeometry(string blockName, Dictionary<string, string> properties);
        List<string> GetTexturesForBlock(string blockName, Dictionary<string, string> properties);
    }
}
