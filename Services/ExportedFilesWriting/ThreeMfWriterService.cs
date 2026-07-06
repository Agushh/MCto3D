using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using MCto3D.Models;

namespace MCto3D.Services.ExportedFilesWriting
{
    public class ThreeMfWriterService : IModelWriter
    {
        private readonly bool _use3MfAssemblyMode;

        public ThreeMfWriterService(bool use3MfAssemblyMode)
        {
            _use3MfAssemblyMode = use3MfAssemblyMode;
        }

        public void Write(string filePath, Dictionary<Color, List<Triangle>> coloredMeshes)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            
            using (FileStream zipToOpen = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                // 1. [Content_Types].xml
                ZipArchiveEntry contentTypesEntry = archive.CreateEntry("[Content_Types].xml");
                using (StreamWriter writer = new StreamWriter(contentTypesEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""model"" ContentType=""application/vnd.ms-package.3dmanufacturing-3dmodel+xml""/>
</Types>");
                }

                // 2. _rels/.rels
                ZipArchiveEntry relsEntry = archive.CreateEntry("_rels/.rels");
                using (StreamWriter writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Target=""/3D/3dmodel.model"" Id=""rel-rel-1"" Type=""http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel""/>
</Relationships>");
                }

                // 3. 3D/3dmodel.model
                ZipArchiveEntry modelEntry = archive.CreateEntry("3D/3dmodel.model");
                using (StreamWriter writer = new StreamWriter(modelEntry.Open(), Encoding.UTF8))
                {
                    StringBuilder xml = new StringBuilder();
                    var cul = CultureInfo.InvariantCulture;
                    
                    xml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
                    xml.AppendLine(@"<model unit=""millimeter"" xml:lang=""en-US"" xmlns=""http://schemas.microsoft.com/3dmanufacturing/core/2015/02"" xmlns:m=""http://schemas.microsoft.com/3dmanufacturing/material/2015/02"">");
                    xml.AppendLine(@"  <resources>");
                    
                    // Escribir grupo de colores
                    xml.AppendLine(@"    <m:colorgroup id=""1"">");
                    foreach (var kvp in coloredMeshes)
                    {
                        var c = kvp.Key;
                        xml.AppendLine($@"      <m:color color=""#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}"" />");
                    }
                    xml.AppendLine(@"    </m:colorgroup>");


                    int currentObjectId = 2;
                    var meshObjectIds = new List<int>();

                    int colorIndex = 0;
                    foreach (var kvp in coloredMeshes)
                    {
                        var color = kvp.Key;
                        var triangles = kvp.Value;
                        if (triangles.Count == 0) 
                        {
                            colorIndex++;
                            continue;
                        }

                        string colorName = GetNearestColorName(color);
                        xml.AppendLine($@"    <object id=""{currentObjectId}"" type=""model"" pid=""1"" pindex=""{colorIndex}"" name=""{colorName}_Part_{currentObjectId}"">");
                        xml.AppendLine(@"      <mesh>");

                        var verticesList = new List<Vector3>();
                        var vertexMap = new Dictionary<Vector3, int>();
                        
                        int AddVertex(Vector3 v)
                        {
                            if (!vertexMap.TryGetValue(v, out int idx))
                            {
                                idx = verticesList.Count;
                                verticesList.Add(v);
                                vertexMap[v] = idx;
                            }
                            return idx;
                        }

                        var indicesList = new List<int[]>();
                        foreach (var tri in triangles)
                        {
                            int v1 = AddVertex(tri.V1);
                            int v2 = AddVertex(tri.V2);
                            int v3 = AddVertex(tri.V3);
                            indicesList.Add(new int[] { v1, v2, v3 });
                        }

                        xml.AppendLine(@"        <vertices>");
                        foreach (var v in verticesList)
                        {
                            xml.AppendLine($@"          <vertex x=""{v.X.ToString(cul)}"" y=""{v.Y.ToString(cul)}"" z=""{v.Z.ToString(cul)}"" />");
                        }
                        xml.AppendLine(@"        </vertices>");

                        xml.AppendLine(@"        <triangles>");
                        foreach (var tri in indicesList)
                        {
                            xml.AppendLine($@"          <triangle v1=""{tri[0]}"" v2=""{tri[1]}"" v3=""{tri[2]}"" />");
                        }
                        xml.AppendLine(@"        </triangles>");
                        
                        xml.AppendLine(@"      </mesh>");
                        xml.AppendLine(@"    </object>");
                        
                        meshObjectIds.Add(currentObjectId);
                        currentObjectId++;
                        colorIndex++;
                    }

                    int masterObjectId = currentObjectId;
                    if (meshObjectIds.Count > 1 && _use3MfAssemblyMode)
                    {
                        xml.AppendLine($@"    <object id=""{masterObjectId}"" type=""model"" name=""MCto3D_Model"">");
                        xml.AppendLine(@"      <components>");
                        foreach (var id in meshObjectIds)
                        {
                            xml.AppendLine($@"        <component objectid=""{id}""/>");
                        }
                        xml.AppendLine(@"      </components>");
                        xml.AppendLine(@"    </object>");
                    }
                    else if (meshObjectIds.Count == 1)
                    {
                        masterObjectId = meshObjectIds[0];
                    }

                    xml.AppendLine(@"  </resources>");
                    
                    xml.AppendLine(@"  <build>");
                    if (meshObjectIds.Count > 1 && _use3MfAssemblyMode)
                    {
                        xml.AppendLine($@"    <item objectid=""{masterObjectId}"" />");
                    }
                    else
                    {
                        foreach (var id in meshObjectIds)
                        {
                            xml.AppendLine($@"    <item objectid=""{id}"" />");
                        }
                    }
                    xml.AppendLine(@"  </build>");
                    
                    xml.AppendLine(@"</model>");
                    
                    writer.Write(xml.ToString());
                }
            }
        }

        private static string GetNearestColorName(Color color)
        {
            var predefinedColors = new Dictionary<string, Color>
            {
                { "Blanco", Color.FromArgb(255, 255, 255) },
                { "Negro", Color.FromArgb(0, 0, 0) },
                { "Rojo", Color.FromArgb(255, 0, 0) },
                { "Verde", Color.FromArgb(0, 255, 0) },
                { "Azul", Color.FromArgb(0, 0, 255) },
                { "Amarillo", Color.FromArgb(255, 255, 0) },
                { "Cian", Color.FromArgb(0, 255, 255) },
                { "Magenta", Color.FromArgb(255, 0, 255) },
                { "Gris", Color.FromArgb(128, 128, 128) },
                { "GrisClaro", Color.FromArgb(192, 192, 192) },
                { "GrisOscuro", Color.FromArgb(64, 64, 64) },
                { "Naranja", Color.FromArgb(255, 165, 0) },
                { "Marron", Color.FromArgb(165, 42, 42) },
                { "Rosa", Color.FromArgb(255, 192, 203) },
                { "Purpura", Color.FromArgb(128, 0, 128) },
                { "VerdeLima", Color.FromArgb(50, 205, 50) },
                { "Celeste", Color.FromArgb(135, 206, 235) },
                { "Dorado", Color.FromArgb(255, 215, 0) },
                { "Beige", Color.FromArgb(245, 245, 220) },
                { "VerdeOliva", Color.FromArgb(128, 128, 0) },
                { "AzulMarino", Color.FromArgb(0, 0, 128) },
                { "Teal", Color.FromArgb(0, 128, 128) },
                { "Turquesa", Color.FromArgb(64, 224, 208) },
                { "Lavanda", Color.FromArgb(230, 230, 250) },
                { "MarronClaro", Color.FromArgb(210, 180, 140) },
                { "MarronOscuro", Color.FromArgb(101, 67, 33) },
                { "Salmon", Color.FromArgb(250, 128, 114) },
                { "Granate", Color.FromArgb(128, 0, 0) },
                { "Coral", Color.FromArgb(255, 127, 80) },
                { "BlancoHueso", Color.FromArgb(253, 245, 230) },
                { "Durazno", Color.FromArgb(255, 218, 185) },
                { "Menta", Color.FromArgb(152, 255, 152) },
                { "Carmesi", Color.FromArgb(220, 20, 60) }
            };

            string nearestName = "Color";
            double minDistance = double.MaxValue;

            foreach (var kvp in predefinedColors)
            {
                double dR = color.R - kvp.Value.R;
                double dG = color.G - kvp.Value.G;
                double dB = color.B - kvp.Value.B;
                
                double distance = dR * dR + dG * dG + dB * dB;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestName = kvp.Key;
                }
            }

            return nearestName;
        }
    }
}

