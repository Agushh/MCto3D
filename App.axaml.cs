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

namespace MCto3D
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            var collection = new ServiceCollection();
            collection.AddSingleton<IAppSettingsService, AppSettingsService>();
            collection.AddSingleton<IProjectStorageService, ProjectStorageService>();
            collection.AddSingleton<IFileReaderService, FileReaderService>();
            collection.AddSingleton<IMeshService, MeshService>();
            collection.AddSingleton<IAssetExtractorService, AssetExtractorService>();
            collection.AddSingleton<IColorGeneratorService, ColorGeneratorService>();
            collection.AddSingleton<ITopologyService, TopologyService>();
            collection.AddSingleton<IColorClusteringService, ColorClusteringService>();
            collection.AddSingleton<IColorMappingService, ColorMappingService>();
            collection.AddSingleton<IModelReader, ThreeMfReaderService>();
            collection.AddSingleton<INativeModelResolverService, NativeModelResolverService>();
            // ViewModels
            collection.AddTransient<MainWindowViewModel>();
            
            var provider = collection.BuildServiceProvider();
            Services = provider;

            ChangeLanguage(Services.GetRequiredService<IAppSettingsService>().Language);
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