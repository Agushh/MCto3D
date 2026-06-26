using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using StbImageSharp;

namespace MCto3D.Services
{
    internal class ColorGenerator_Service
    {
        public static async Task<Dictionary<string, byte[]>> GenerateAndLoadColors(string localAppFolder, IProgress<string> progress = null)
        {
            progress?.Report("Iniciando procesamiento de colores para .3mf ...");

            string texturesPath = Path.Combine(localAppFolder, "MinecraftExtractedAssets", "assets", "minecraft", "textures", "block");
            string jsonOutputPath = Path.Combine(localAppFolder, "colores_base.json");

            if (!Directory.Exists(texturesPath))
            {
                progress?.Report("Error: No se encontró la carpeta de texturas.");
                // Devolvemos un diccionario vacío o podés lanzar una excepción según prefieras
                return new();
            }

            Directory.CreateDirectory(localAppFolder);

            var blockColors = new Dictionary<string, byte[]>();

            string[] files = Directory.GetFiles(texturesPath, "*.png");
            int totalFiles = files.Length;

            if (totalFiles == 0)
            {
                progress?.Report("Error: No hay texturas .png para procesar.");
                return blockColors;
            }

            await Task.Run(() =>
            {
                int processedCount = 0;

                foreach (string file in files)
                {
                    // Obtenemos el nombre limpio (ej: "oak_log")
                    string blockName = Path.GetFileNameWithoutExtension(file);

                    // Cargamos la imagen en memoria usando ImageSharp
                    using (Stream stream = File.OpenRead(file))
                    {
                        // Le decimos que decodifique la imagen y nos asegure 4 canales (RGBA)
                        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                        long totalR = 0, totalG = 0, totalB = 0;
                        int validPixels = 0;

                        // image.Data es un byte[] unidimensional. 
                        // Como pedimos RGBA, cada píxel ocupa 4 posiciones seguidas en el arreglo.
                        for (int i = 0; i < image.Data.Length; i += 4)
                        {
                            byte r = image.Data[i];
                            byte g = image.Data[i + 1];
                            byte b = image.Data[i + 2];
                            byte a = image.Data[i + 3];

                            // Ignoramos los píxeles transparentes
                            if (a > 0)
                            {
                                totalR += r;
                                totalG += g;
                                totalB += b;
                                validPixels++;
                            }
                        }

                        if (validPixels > 0)
                        {
                            blockColors[blockName] = new byte[]
                            {
                            (byte)(totalR / validPixels),
                            (byte)(totalG / validPixels),
                            (byte)(totalB / validPixels)
                            };
                        }

                        processedCount++;

                        // Actualizamos la UI cada 50 archivos para no saturar el reporte
                        if (processedCount % 50 == 0 || processedCount == totalFiles)
                        {
                            progress?.Report($"Procesando texturas: {processedCount} / {totalFiles}");
                        }
                    }
                }
            });
            // 3. Guardamos el resultado en JSON de forma asíncrona
            progress?.Report("Guardando archivo de colores...");

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(blockColors, jsonOptions);

            await File.WriteAllTextAsync(jsonOutputPath, jsonString);

            progress?.Report("¡Procesamiento exitoso!");
            return blockColors;
        }

        public static Dictionary<string, byte[]> LoadColorsSync(string localAppFolder)
        {
            string jsonPath = Path.Combine(localAppFolder, "localData", "colores_base.json");

            // 1. Verificación de seguridad
            if (!File.Exists(jsonPath))
            {
                return new Dictionary<string, byte[]>();
            }

            try
            {
                // 2. LECTURA SINCRÓNICA (File.ReadAllText en lugar de ReadAllTextAsync)
                string jsonString = File.ReadAllText(jsonPath);

                // 3. DESERIALIZACIÓN SINCRÓNICA
                var blockColors = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(jsonString);

                if (blockColors != null)
                {
                    return blockColors;
                }
                return new Dictionary<string, byte[]>();
            }
            catch (Exception ex)
            {
                return new Dictionary<string, byte[]>();
            }
        }
    }
}
