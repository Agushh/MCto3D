using MCto3D.Models;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MCto3D.Services
{
    internal class StlGenerator
    {

        public static void CreateAsciiStl(string filePath, List<Triangle> triangles)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("solid CustomModel");

                foreach (var t in triangles)
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
            // Usamos un escritor binario para manipular los bytes directamente
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                // 1. LA CABECERA (80 bytes de texto)
                // Aquí los laminadores leen con qué software se hizo el archivo
                byte[] header = new byte[80];
                System.Text.Encoding.ASCII.GetBytes("Exportado por MCto3D con Color VisCAM").CopyTo(header, 0);
                writer.Write(header);

                // 2. NÚMERO TOTAL DE TRIÁNGULOS (4 bytes - UInt32)
                writer.Write((uint)triangles.Count);

                // 3. LA MAGIA DEL COLOR (Comprimir 24 bits a 15 bits)
                // Avalonia nos da colores de 0 a 255. El "Hack" de VisCAM requiere colores de 0 a 31.
                ushort r5 = (ushort)(modelColor.R * 31 / 255);
                ushort g5 = (ushort)(modelColor.G * 31 / 255);
                ushort b5 = (ushort)(modelColor.B * 31 / 255);

                // Unimos los bits: [1 bit válido] [5 bits Rojo] [5 bits Verde] [5 bits Azul]
                ushort colorAttribute = (ushort)((1 << 15) | (r5 << 10) | (g5 << 5) | b5);

                // 4. ESCRIBIR LA GEOMETRÍA
                foreach (var t in triangles)
                {
                    // Vector Normal (12 bytes: 3 floats)
                    writer.Write(t.Normal.X);
                    writer.Write(t.Normal.Y);
                    writer.Write(t.Normal.Z);

                    // Vértice 1 (12 bytes: 3 floats)
                    writer.Write(t.V1.X);
                    writer.Write(t.V1.Y);
                    writer.Write(t.V1.Z);

                    // Vértice 2 (12 bytes: 3 floats)
                    writer.Write(t.V2.X);
                    writer.Write(t.V2.Y);
                    writer.Write(t.V2.Z);

                    // Vértice 3 (12 bytes: 3 floats)
                    writer.Write(t.V3.X);
                    writer.Write(t.V3.Y);
                    writer.Write(t.V3.Z);

                    // EL ATRIBUTO SECRETO (2 bytes) -> ¡Aquí inyectamos el color!
                    writer.Write(colorAttribute);
                }
            }
        }

    }
}
