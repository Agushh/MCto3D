using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MCto3D.Services
{
    internal class AssetExtractor_Service
    {

        public static string ExtractLegalAssets(IProgress<string> progress = null, string minecraftLocation = "default")
        {
            progress?.Report("Iniciando búsqueda de assets...");

            string appData = minecraftLocation;
            
            if (minecraftLocation == "default")
                appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string minecraftDir = Path.Combine(appData, ".minecraft");
            
            if (!Directory.Exists(minecraftDir))
            {
                if (!Directory.Exists(Path.Combine(appData, "versions")))
                    throw new Exception("No dispones de la carpeta .minecraft en su lugar por defecto! Tienes instalado minecraft en otra direccion?...");
                else
                    minecraftDir = minecraftLocation;
            }
            string versionsDir = Path.Combine(minecraftDir, "versions"); 
            if (!Directory.Exists(versionsDir))
            {
                throw new Exception("No dispones de ninguna instalacion de minecraft descargada! Recuerda que debes de haber abierto el juego al menos una vez");
            }

            string latestVersion = GetLatestMinecraftVersion(versionsDir);
            string jarPath = Path.Combine(versionsDir, latestVersion, $"{latestVersion}.jar");

            if (!File.Exists(jarPath))
            {
                throw new FileNotFoundException($"Error: No se encontró el archivo .jar de la versión {latestVersion} en la ruta {jarPath}");
            }

            string localAppFolder = Path.Combine(AppSettings_Service.LocalFilesPath, "MinecraftExtractedAssets");

            if (Directory.Exists(localAppFolder))
            {
                progress?.Report("Limpiando assets anteriores...");
                Directory.Delete(localAppFolder, true);
            }

            System.Diagnostics.Debug.WriteLine($"Extrayendo assets en: {localAppFolder}...");

            progress?.Report($"Abriendo el archivo de Minecraft {latestVersion}...");
            using (ZipArchive archive = ZipFile.OpenRead(jarPath))
            {
                int totalToExtract = 0;
                int extracted = 0;
                
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.StartsWith("assets/minecraft/models/block/") ||
                        entry.FullName.StartsWith("assets/minecraft/blockstates/") ||
                        entry.FullName.StartsWith("data/minecraft/structure/"))
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        if (entry.FullName.Contains("/villagers/") || entry.FullName.Contains("/mobs/") || entry.FullName.Contains("/entities/")) continue;
                        totalToExtract++;
                    }
                }

                //Shit code for dynamic numbers
                int ammount = Random.Shared.Next(75, 100);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Filtramos: Modelos, blockstates y ESTRUCTURAS VANILLA
                    if (entry.FullName.StartsWith("assets/minecraft/models/block/") ||
                        entry.FullName.StartsWith("assets/minecraft/blockstates/") ||
                        entry.FullName.StartsWith("data/minecraft/structure/"))
                    {
                        // Saltamos las entradas que son solo carpetas vacías
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        if (entry.FullName.Contains("/villagers/") || entry.FullName.Contains("/mobs/") || entry.FullName.Contains("/entities/")) continue;

                        // Recreamos la estructura de carpetas en nuestro destino
                        string destinationPath = Path.Combine(localAppFolder, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        // Extraemos el archivo (sobreescribe si ya existe para asegurar que esté actualizado)
                        entry.ExtractToFile(destinationPath, overwrite: true);

                        extracted++;
                        // Reportamos cada 100 archivos para no saturar la interfaz gráfica
                        if (extracted % ammount == 0)
                        {
                            progress?.Report($"Extrayendo assets... ({extracted} / {totalToExtract})");
                            ammount = Random.Shared.Next(75, 100);
                        }

                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("¡Extracción completada con éxito!");

            File.WriteAllText(Path.Combine(localAppFolder, "version.txt"), latestVersion);
            progress?.Report("¡Extracción completada con éxito!");
            return localAppFolder;
        }



        public static string GetLatestMinecraftVersion(string versionsLocation)
        {
            string latestVersionName = String.Empty;
            Version highestVersion = new(0, 0);

            foreach(string dir in Directory.GetDirectories(versionsLocation))
            {
                string dirName = Path.GetFileName(dir);
                string jarPath = Path.Combine(dir, $"{dirName}.jar");

                if(File.Exists(jarPath))
                {
                    Match match = Regex.Match(dirName, @"^\d+\.\d+(\.\d+)?");

                    if (match.Success)
                    {
                        string versionString = match.Value;

                        if(Version.TryParse(versionString, out Version parsedVersion))
                        {
                            if(parsedVersion > highestVersion)
                            {
                                highestVersion = parsedVersion;
                                latestVersionName = dirName;
                            }
                        }
                    }
                }
            }
            if (latestVersionName == String.Empty)
            {
                throw new FileNotFoundException("No se encontró ninguna instalación de Minecraft Vanilla o Modificada válida con un .jar.");
            }
            return latestVersionName;
        }
    }
}
