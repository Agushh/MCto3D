using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.Numerics;
using MCto3D.Models;

namespace MCto3D.Services.ExportedFilesReading
{
    public class ThreeMfReaderService : IModelReader
    {
        public Dictionary<Color, List<Triangle>> Read(string filePath)
        {
            var result = new Dictionary<Color, List<Triangle>>();

            try
            {
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    var entry = archive.GetEntry("3D/3dmodel.model");
                    if (entry == null) return result;

                    using (var stream = entry.Open())
                    {
                        var doc = XDocument.Load(stream);
                        XNamespace ns = doc.Root.Name.Namespace;
                        XNamespace mNs = "http://schemas.microsoft.com/3dmanufacturing/material/2015/02";

                        var colorMap = new Dictionary<int, Color>();
                        
                        // Parse BaseMaterials for colors
                        var baseMaterials = doc.Root.Descendants(mNs + "colorgroup").FirstOrDefault() ?? doc.Root.Descendants(mNs + "basematerials").FirstOrDefault();
                        if (baseMaterials != null)
                        {
                            int pindex = 0;
                            foreach (var baseMat in baseMaterials.Elements(mNs + "color").Concat(baseMaterials.Elements(mNs + "base")))
                            {
                                var colorAttr = baseMat.Attribute("color") ?? baseMat.Attribute("displaycolor");
                                if (colorAttr != null)
                                {
                                    string hex = colorAttr.Value;
                                    try
                                    {
                                        if (hex.Length == 9) // #RRGGBBAA
                                        {
                                            var color = Color.FromArgb(
                                                Convert.ToByte(hex.Substring(7, 2), 16),
                                                Convert.ToByte(hex.Substring(1, 2), 16),
                                                Convert.ToByte(hex.Substring(3, 2), 16),
                                                Convert.ToByte(hex.Substring(5, 2), 16)
                                            );
                                            colorMap[pindex] = color;
                                        }
                                    }
                                    catch { }
                                }
                                pindex++;
                            }
                        }

                        // Parse objects and triangles
                        var resources = doc.Root.Element(ns + "resources");
                        if (resources != null)
                        {
                            foreach (var obj in resources.Elements(ns + "object"))
                            {
                                var mesh = obj.Element(ns + "mesh");
                                if (mesh != null)
                                {
                                    var vertices = new List<Vector3>();
                                    var vertsElem = mesh.Element(ns + "vertices");
                                    if (vertsElem != null)
                                    {
                                        foreach (var v in vertsElem.Elements(ns + "vertex"))
                                        {
                                            float x = float.Parse(v.Attribute("x").Value, System.Globalization.CultureInfo.InvariantCulture);
                                            float y = float.Parse(v.Attribute("y").Value, System.Globalization.CultureInfo.InvariantCulture);
                                            float z = float.Parse(v.Attribute("z").Value, System.Globalization.CultureInfo.InvariantCulture);
                                            vertices.Add(new Vector3(x, y, z));
                                        }
                                    }

                                    var trisElem = mesh.Element(ns + "triangles");
                                    if (trisElem != null)
                                    {
                                        var pindexAttrObj = obj.Attribute("pindex");
                                        int defaultPindex = pindexAttrObj != null ? int.Parse(pindexAttrObj.Value) : 0;
                                        Color objColor = colorMap.ContainsKey(defaultPindex) ? colorMap[defaultPindex] : Color.Gray;

                                        if (!result.ContainsKey(objColor))
                                        {
                                            result[objColor] = new List<Triangle>();
                                        }

                                        foreach (var t in trisElem.Elements(ns + "triangle"))
                                        {
                                            int v1 = int.Parse(t.Attribute("v1").Value);
                                            int v2 = int.Parse(t.Attribute("v2").Value);
                                            int v3 = int.Parse(t.Attribute("v3").Value);
                                            
                                            // 3MF triangles might have specific pindex
                                            var pindexAttr = t.Attribute("pindex");
                                            if (pindexAttr != null)
                                            {
                                                int tPindex = int.Parse(pindexAttr.Value);
                                                if (colorMap.ContainsKey(tPindex))
                                                {
                                                    var tColor = colorMap[tPindex];
                                                    if (!result.ContainsKey(tColor))
                                                    {
                                                        result[tColor] = new List<Triangle>();
                                                    }
                                                    result[tColor].Add(new Triangle(vertices[v1], vertices[v2], vertices[v3]));
                                                    continue;
                                                }
                                            }
                                            
                                            result[objColor].Add(new Triangle(vertices[v1], vertices[v2], vertices[v3]));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading 3MF: {ex.Message}");
            }

            return result;
        }
    }
}

