using MCto3D.Models;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MCto3D.Services.ExportedFilesWriting
{
    internal class StlGenerator
    {
        public static void CreateAsciiStl(string filePath, List<Triangle> mesh)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("solid CustomModel");

                foreach (var t in mesh)
                {
                    var culture = System.Globalization.CultureInfo.InvariantCulture;

                    writer.WriteLine($"  facet normal {t.Normal.X.ToString(culture)} {t.Normal.Y.ToString(culture)} {t.Normal.Z.ToString(culture)}");
                    writer.WriteLine("    outer loop");
                    writer.WriteLine($"      vertex {t.V1.X.ToString(culture)} {t.V1.Y.ToString(culture)} {t.V1.Z.ToString(culture)}");
                    writer.WriteLine($"      vertex {t.V2.X.ToString(culture)} {t.V2.Y.ToString(culture)} {t.V2.Z.ToString(culture)}");
                    writer.WriteLine($"      vertex {t.V3.X.ToString(culture)} {t.V3.Y.ToString(culture)} {t.V3.Z.ToString(culture)}");
                    writer.WriteLine("    endloop");
                    writer.WriteLine("  endfacet");
                }
                writer.WriteLine("endsolid CustomModel");
            }
        }

        public static void CreateBinaryStlWithColor(string filePath, List<Triangle> triangles, Color modelColor)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                // Header (80 bytes)
                byte[] header = new byte[80];
                System.Text.Encoding.ASCII.GetBytes("Exportado por MCto3D con Color VisCAM").CopyTo(header, 0);
                writer.Write(header);

                // Number of triangles (UInt32)
                writer.Write((uint)triangles.Count);

                // Compress 24-bit color to 15-bit color for VisCAM specification
                ushort r5 = (ushort)(modelColor.R * 31 / 255);
                ushort g5 = (ushort)(modelColor.G * 31 / 255);
                ushort b5 = (ushort)(modelColor.B * 31 / 255);
                ushort colorAttribute = (ushort)((1 << 15) | (r5 << 10) | (g5 << 5) | b5);

                // Geometry and color attribute
                foreach (var t in triangles)
                {
                    // Normal Vector
                    writer.Write(t.Normal.X);
                    writer.Write(t.Normal.Y);
                    writer.Write(t.Normal.Z);

                    // Vertex 1
                    writer.Write(t.V1.X);
                    writer.Write(t.V1.Y);
                    writer.Write(t.V1.Z);

                    // Vertex 2
                    writer.Write(t.V2.X);
                    writer.Write(t.V2.Y);
                    writer.Write(t.V2.Z);

                    // Vertex 3
                    writer.Write(t.V3.X);
                    writer.Write(t.V3.Y);
                    writer.Write(t.V3.Z);

                    // Color attribute
                    writer.Write(colorAttribute);
                }
            }
        }
    }
}
