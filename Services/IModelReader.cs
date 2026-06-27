using System.Collections.Generic;
using System.Drawing;
using MCto3D.Models;

namespace MCto3D.Services
{
    public interface IModelReader
    {
        Dictionary<Color, List<Triangle>> Read(string filePath);
    }
}
