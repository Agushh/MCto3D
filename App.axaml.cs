using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json;
using MCto3D.ViewModels;
using MCto3D.Services;
using MCto3D.Services.ColorProcesing;
using MCto3D.Services.ColorProcesing.Algorithms;
using MCto3D.Services.FileReading;
using MCto3D.Services.ColorProcesing.Factory;
using MCto3D.Services.AssetsProcessing;
using MCto3D.Services.ExportedFilesReading;

namespace MCto3D
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            var collection = new ServiceCollection();
            
            collection.AddSingleton<AppSettingsService>();
            collection.AddSingleton<IProjectStorageService, ProjectStorageService>();
            collection.AddSingleton<StructureLoaderService>();
            collection.AddSingleton<MeshService>();
            collection.AddSingleton<AssetExtractorService>();
            collection.AddSingleton<TopologyService>();
            collection.AddSingleton<KMeansColorsService>();
            collection.AddSingleton<KMedoidsColorsService>();
            collection.AddSingleton<UserPaletteColorsService>();
            collection.AddSingleton<SingleColorService>();
            collection.AddSingleton<RawColorService>();
            collection.AddSingleton<IColorAlgorithmFactory, ColorAlgorithmFactory>();

            collection.AddSingleton<ColorMappingService>();
            collection.AddSingleton<IModelReader, ThreeMfReaderService>();
            collection.AddSingleton<NativeModelResolverService>();


            collection.AddTransient<IStructureReader, NbtReaderService>();
            collection.AddTransient<IStructureReader, LitematicReaderService>();
            collection.AddSingleton<StructureLoaderService>();
            collection.AddSingleton<ColorSeparatorService>();

            // ViewModels
            collection.AddTransient<MainWindowViewModel>();
            
            var provider = collection.BuildServiceProvider();
            Services = provider;

            ChangeLanguage(Services.GetRequiredService<AppSettingsService>().Language);
        }

        public static void ChangeLanguage(string cultureCode)
        {
            if (Current != null)
            {
                try
                {
                    var uri = new Uri($"avares://MCto3D/Assets/Lang/{cultureCode}.json");
                    using var stream = AssetLoader.Open(uri);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                    
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            Current.Resources[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error changing language: {ex.Message}");
                }
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                mainWindow.DataContext = Services?.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}