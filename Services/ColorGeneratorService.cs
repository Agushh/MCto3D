using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using StbImageSharp;

namespace MCto3D.Services
{
    public class ColorGeneratorService : IColorGeneratorService
    {
        public async Task<Dictionary<string, byte[]>> GenerateAndLoadColors(string localAppFolder, IProgress<string> progress = null)
        {
            progress?.Report(MCto3D.Services.LanguageService.GetString("ProgressInitColors"));

            string texturesPath = Path.Combine(localAppFolder, "MinecraftExtractedAssets", "assets", "minecraft", "textures", "block");
            string jsonOutputPath = Path.Combine(localAppFolder, "colores_base.json");

            if (!Directory.Exists(texturesPath))
            {
                progress?.Report(MCto3D.Services.LanguageService.GetString("ProgressErrorTexture"));
                // Devolvemos un diccionario vacío o podés lanzar una excepción según prefieras
                return new();
            }

            Directory.CreateDirectory(localAppFolder);

            var blockColors = new Dictionary<string, byte[]>();

            string[] files = Directory.GetFiles(texturesPath, "*.png");
            int totalFiles = files.Length;

            if (totalFiles == 0)
            {
                progress?.Report(MCto3D.Services.LanguageService.GetString("ProgressErrorNoPng"));
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
                                // Elevamos al cuadrado para calcular la media cuadrática (RMS)
                                // Esto evita que los colores promediados se vean "sucios" o desaturados.
                                totalR += (r * r);
                                totalG += (g * g);
                                totalB += (b * b);
                                validPixels++;
                            }
                        }

                        if (validPixels > 0)
                        {
                            byte avgR = (byte)Math.Sqrt(totalR / validPixels);
                            byte avgG = (byte)Math.Sqrt(totalG / validPixels);
                            byte avgB = (byte)Math.Sqrt(totalB / validPixels);

                            // Hardcoded biome tints (Plains/Forest defaults)
                            if (blockName == "grass_block_top" || blockName == "grass" || blockName.StartsWith("tall_grass") || blockName.Contains("fern") || blockName == "vine" || blockName == "sugar_cane")
                            {
                                avgR = (byte)(avgR * 121 / 255);
                                avgG = (byte)(avgG * 192 / 255);
                                avgB = (byte)(avgB * 90 / 255);
                            }
                            else if (blockName == "oak_leaves" || blockName == "jungle_leaves" || blockName == "acacia_leaves" || blockName == "dark_oak_leaves" || blockName == "mangrove_leaves" || blockName == "lily_pad")
                            {
                                avgR = (byte)(avgR * 89 / 255);
                                avgG = (byte)(avgG * 174 / 255);
                                avgB = (byte)(avgB * 48 / 255);
                            }
                            else if (blockName == "spruce_leaves")
                            {
                                avgR = (byte)(avgR * 97 / 255);
                                avgG = (byte)(avgG * 153 / 255);
                                avgB = (byte)(avgB * 97 / 255);
                            }
                            else if (blockName == "birch_leaves")
                            {
                                avgR = (byte)(avgR * 128 / 255);
                                avgG = (byte)(avgG * 167 / 255);
                                avgB = (byte)(avgB * 85 / 255);
                            }
                            else if (blockName == "water_still" || blockName == "water_flow")
                            {
                                avgR = (byte)(avgR * 63 / 255);
                                avgG = (byte)(avgG * 118 / 255);
                                avgB = (byte)(avgB * 228 / 255);
                            }

                            blockColors[blockName] = new byte[] { avgR, avgG, avgB };
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
            progress?.Report(MCto3D.Services.LanguageService.GetString("ProgressSavingColors"));

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(blockColors, jsonOptions);

            await File.WriteAllTextAsync(jsonOutputPath, jsonString);

            progress?.Report("¡Procesamiento exitoso!");
            return blockColors;
        }

        public Dictionary<string, byte[]> LoadColorsSync(string localAppFolder)
        {
            string jsonPath = Path.Combine(localAppFolder, "colores_base.json");

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

