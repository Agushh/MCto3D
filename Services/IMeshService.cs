using MCto3D.Models;
using System.Collections.Generic;

namespace MCto3D.Services
{
    public interface IMeshService
    {
        List<Triangle> GenerateFullGeometryMesh(structureData strData, float scale);
        List<Triangle> GenerateMesh(structureData strData, float scale);
        Dictionary<System.Drawing.Color, List<Triangle>> GenerateMultiColorMeshes(multiColorStructureData multiData, structureData originalData, float scale, bool useFullGeom = false);
    }
}
