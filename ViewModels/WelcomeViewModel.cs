using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Services;

namespace MCto3D.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _loadingMessage = "Procesando...";

    private string location = "";

    private readonly IAppSettingsService _appSettings;
    private readonly IAssetExtractorService _assetExtractorService;
    private readonly IColorGeneratorService _colorGeneratorService;
    private readonly IColorMappingService _colorMappingService;

    public WelcomeViewModel(MainWindowViewModel mainViewModel, IAppSettingsService appSettings, IAssetExtractorService assetExtractorService, IColorGeneratorService colorGeneratorService, IColorMappingService colorMappingService)
    {
        _mainViewModel = mainViewModel;
        _appSettings = appSettings;
        _assetExtractorService = assetExtractorService;
        _colorGeneratorService = colorGeneratorService;
        _colorMappingService = colorMappingService;
    }

    [RelayCommand]
    private async Task ContinueWithDefaultFolderAsync()
    {
        IsProcessing = true;
        LoadingMessage = "Buscando instalación por defecto...";
        try
        {
            var progressReporter = new Progress<string>(mensaje =>
            {
                LoadingMessage = mensaje;
            });

            location = await Task.Run(() => _assetExtractorService.ExtractLegalAssets(_appSettings.LocalFilesPath, progressReporter));
            await Task.Delay(1500); // Dar un poco de tiempo para leer el mensaje final de extracción


            //procesar colores y guardarlos en JSON.
            _colorMappingService.BlockColors = await _colorGeneratorService.GenerateAndLoadColors(_appSettings.LocalFilesPath, progressReporter);
            await Task.Delay(1500);

            // Actualizamos la versión en SettingsVM ahora que se extrajeron los archivos
            await _mainViewModel.SettingsVM.UpdateMinecraftVersionAsync();
            
            // Ocultamos la bienvenida y pasamos directo al Dashboard (el Loading de modelos ya no existe)
            _mainViewModel.IsWelcomeScreenActive = false;
            _mainViewModel.IsMainContentVisible = true;
        }
        catch (Exception e)
        {
            LoadingMessage = $"Error: {e.Message}";
            await Task.Delay(3000);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task SelectCustomFolderAsync()
    {
        IsProcessing = true;
        LoadingMessage = "Esperando selección de carpeta...";

        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var storageProvider = desktop.MainWindow?.StorageProvider;
                if (storageProvider != null)
                {
                    var result = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Selecciona la carpeta .minecraft",
                        AllowMultiple = false
                    });

                    if (result != null && result.Count > 0)
                    {
                        string selectedFolderPath = result[0].Path.LocalPath;
                        System.Diagnostics.Debug.WriteLine($"Carpeta seleccionada: {selectedFolderPath}");

                        var progressReporter = new Progress<string>(mensaje =>
                        {
                            LoadingMessage = mensaje;
                        });

                        // Extracción de archivos
                        location = await Task.Run(() => _assetExtractorService.ExtractLegalAssets(_appSettings.LocalFilesPath, progressReporter, selectedFolderPath));
                        await Task.Delay(1500);

                        //procesar colores y guardarlos en JSON.
                        _colorMappingService.BlockColors = await _colorGeneratorService.GenerateAndLoadColors(_appSettings.LocalFilesPath, progressReporter);
                        await Task.Delay(1500);

                        // Actualizamos la versión en SettingsVM ahora que se extrajeron los archivos
                        await _mainViewModel.SettingsVM.UpdateMinecraftVersionAsync();
                        
                        // Ocultamos bienvenida y pasamos directo
                        _mainViewModel.IsWelcomeScreenActive = false;
                        _mainViewModel.IsMainContentVisible = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al abrir el buscador o procesar: {ex.Message}");
            LoadingMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
        }
        finally
        {
            IsProcessing = false;
        }
    }
}

