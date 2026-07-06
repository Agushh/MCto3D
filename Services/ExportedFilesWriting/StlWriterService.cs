using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MCto3D.Models;

namespace MCto3D.Services.ExportedFilesWriting
{
    public class StlWriterService : IModelWriter
    {
        public void Write(string filePath, Dictionary<Color, List<Triangle>> coloredMeshes)
        {
            var allTriangles = new List<Triangle>();
            foreach (var kvp in coloredMeshes)
            {
                allTriangles.AddRange(kvp.Value);
            }
            
            Color generalColor = Color.Gray;
            if (coloredMeshes.Count == 1) generalColor = coloredMeshes.Keys.First();
            
            StlGenerator.CreateBinaryStlWithColor(filePath, allTriangles, generalColor);
        }
    }
}

