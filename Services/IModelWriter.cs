using System.Collections.Generic;
using System.Drawing;
using MCto3D.Models;

namespace MCto3D.Services
{
    public interface IModelWriter
    {
        void Write(string filePath, Dictionary<Color, List<Triangle>> coloredMeshes);
    }
}
