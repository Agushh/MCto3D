using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCto3D.Services.AssetsProcessing
{
    public class AssetExtractorService
    {
        public async Task<string> ExtractLegalAssets(string localFilesPath, IProgress<string> progress = null, string userLocationInput = "default")
        {
            progress?.Report(LanguageService.GetString("AssetExtractorStartExtraction"));

            string appData = userLocationInput == "default" ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData):
                                                              userLocationInput;
            
            string minecraftDir = Path.Combine(appData, ".minecraft");
            
            string versionsDir;

            #region Comprobacion de direcciones
            //Comprobar las rutas y manejar las comprobaciones
            //Adicional, añadir traduccion al texto de las excepciones.
            if (Directory.Exists(minecraftDir))
            {
                if(Directory.Exists(Path.Combine(minecraftDir, "versions")))
                {
                    versionsDir = Path.Combine(minecraftDir, "versions");
                }
                else
                {
                    //La carpeta .minecraft existe pero no contiene carpeta Versions.
                    throw new DirectoryNotFoundException(LanguageService.GetString("AssetExtractorVersionsNotFoundOnDotM"));
                }
            }
            else
            {
                minecraftDir = appData;
                //no se encontro .minecraft, se comprobara que la carpeta elegida sea ya .minecraft
                if (Directory.Exists(Path.Combine(minecraftDir, "versions")))
                {
                    versionsDir = Path.Combine(minecraftDir, "versions");
                }
                else //No se encontro la carpeta versions, se comprobara el caso de Curseforge, el cual utiliza una carpeta Install, y dentro Versions
                {
                    if (Directory.Exists(Path.Combine(minecraftDir, "Install")))
                    {
                        if (Directory.Exists(Path.Combine(minecraftDir, "Install", "versions")))
                        {
                            versionsDir = Path.Combine(minecraftDir, "Install", "versions");
                        }
                        else
                        {
                            //No hay versiones instaladas. -- No se encontro la carpeta Versions.
                            throw new DirectoryNotFoundException(LanguageService.GetString("AssetExtractorVersionsNotFoundOnDotM"));
                        }
                    }
                    else
                    {
                        //No se encontro nada. 
                        throw new DirectoryNotFoundException(LanguageService.GetString("AssetExtractorNothingFound"));
                    }
                }
                

            }

            #endregion

            string latestVersion = GetLatestMinecraftVersion(versionsDir);


            string jarPath = Path.Combine(versionsDir, latestVersion, $"{latestVersion}.jar");

            //---CONST THAT NEEDS TO BE ON SEPARATED FILE
            string localAppFolder = Path.Combine(localFilesPath, "MinecraftExtractedAssets");


            if (Directory.Exists(localAppFolder))
            {
                progress?.Report(LanguageService.GetString("AssetExtractorCleanningOldAssets"));
                Directory.Delete(localAppFolder, true);
            }

            progress?.Report(LanguageService.GetString("AssetExtractorOpeningFile") + latestVersion + "...");
            await Task.Delay(500);
            using (ZipArchive archive = ZipFile.OpenRead(jarPath))
            {
                int totalToExtract = 0;
                int extracted = 0;
                
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.StartsWith("assets/minecraft/models/block/") ||
                        entry.FullName.StartsWith("assets/minecraft/blockstates/") ||
                        entry.FullName.StartsWith("data/minecraft/structure/") ||
                        entry.FullName.StartsWith("assets/minecraft/textures/block/"))
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        
                        if (entry.FullName.Contains("/villagers/") || 
                            entry.FullName.Contains("/mobs/") ||
                            entry.FullName.Contains("/entities/") || 
                            entry.FullName.EndsWith(".mcmeta", StringComparison.OrdinalIgnoreCase)) 
                            continue;

                        totalToExtract++;
                    }
                }

                //random number for dynamic loading - UX
                int ammount = Random.Shared.Next(75, 175);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Filtramos: Modelos, blockstates, estructuras vanillas y texturas
                    if (entry.FullName.StartsWith("assets/minecraft/models/block/") ||
                        entry.FullName.StartsWith("assets/minecraft/blockstates/") ||
                        entry.FullName.StartsWith("data/minecraft/structure/") ||
                        entry.FullName.StartsWith("assets/minecraft/textures/block/"))
                    {
                        // Saltamos las entradas que son solo carpetas vacías
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        
                        // Salteamos informacion innecesaria
                        if (entry.FullName.Contains("/villagers/") || 
                            entry.FullName.Contains("/mobs/") || 
                            entry.FullName.Contains("/entities/") ||
                            entry.FullName.EndsWith(".mcmeta", StringComparison.OrdinalIgnoreCase)) 
                            continue;

                        // Recreamos la estructura de carpetas en nuestro destino
                        string destinationPath = Path.Combine(localAppFolder, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        // Extraemos el archivo (sobreescribe si ya existe para asegurar que esté actualizado)
                        entry.ExtractToFile(destinationPath, overwrite: true);

                        extracted++;
                        // Reportamos cada 100 archivos para no saturar la interfaz gráfica
                        if (extracted % ammount == 0)
                        {
                            string filename = Path.GetFileName(entry.FullName);
                            progress?.Report(LanguageService.GetString("AssetExtractorExtracting") + $" {filename}... ({extracted} / {totalToExtract})");
                            ammount = Random.Shared.Next(75, 175);
                        }

                    }
                }
            }
            File.WriteAllText(Path.Combine(localAppFolder, "version.txt"), latestVersion);
            progress?.Report(LanguageService.GetString("AssetExtractorExtractionSuccessfull"));
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
                throw new FileNotFoundException(LanguageService.GetString("AssetExtractorVersionNotFound"));
            }
            return latestVersionName;
        }
    }
}

