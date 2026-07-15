using Avalonia.Controls;
using Avalonia.OpenGL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
using MCto3D.Services.AssetsProcessing;
using MCto3D.Services.ColorProcesing;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MCto3D.ViewModels;

public partial class PaletteModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    public string OriginalName { get; set; } = string.Empty;
}

public partial class SettingsViewModel : ViewModelBase
{

    private readonly MainWindowViewModel _mainViewModel;

    private readonly AppSettingsService _appSettings;

    private readonly AssetExtractorService _assetExtractorService;

    private readonly ColorMappingService _colorMappingService;

    [ObservableProperty]
    public bool _isMissingFiles = false;

    [ObservableProperty]
    private string _mcVersion = null;

    

    [ObservableProperty]
    private string _localFilesPath;

    [ObservableProperty]
    private bool _isMovingFiles;

    [ObservableProperty]
    private string _movingProgressMessage = "";

    [ObservableProperty]
    private int _selectedTabIndex;

    public SettingsViewModel(MainWindowViewModel mainViewModel, AppSettingsService appSettings, AssetExtractorService assetExtractorService, ColorMappingService colorMappingService)
    {
        _mainViewModel = mainViewModel;
        _appSettings = appSettings;
        _assetExtractorService = assetExtractorService;
        _localFilesPath = _appSettings.LocalFilesPath;

        _showFloor = _appSettings.ShowFloor;
        try { _floorColor = Avalonia.Media.Color.Parse(_appSettings.FloorColorHex); } catch { _floorColor = Avalonia.Media.Color.Parse("#1A1A1A"); }
        try { _modelColor = Avalonia.Media.Color.Parse(_appSettings.ModelColorHex); } catch { _modelColor = Avalonia.Media.Colors.White; }

        _colorMappingService = colorMappingService;

        _isMissingFiles = appSettings.McVersion == null;
        McVersion = appSettings.McVersion == null ? "-" : appSettings.McVersion;
    }
    [ObservableProperty]
    private bool _use3MfAssemblyMode;

    [ObservableProperty]
    private bool _showFloor;

    [ObservableProperty]
    private Avalonia.Media.Color _floorColor = Avalonia.Media.Colors.Green;

    [ObservableProperty]
    private Avalonia.Media.Color _modelColor = Avalonia.Media.Colors.White;

    [ObservableProperty]
    private bool _isPaletteManagerOpen;

    public ObservableCollection<PaletteModel> SavedPalettes { get; } = new();

    [RelayCommand]
    private void OpenPaletteManager()
    {
        SavedPalettes.Clear();
        foreach (var p in _appSettings.SavedPalettes)
        {
            SavedPalettes.Add(new PaletteModel { Name = p.Name, OriginalName = p.Name });
        }
        IsPaletteManagerOpen = true;
    }

    [RelayCommand]
    private void ClosePaletteManager() => IsPaletteManagerOpen = false;

    [RelayCommand]
    private void SavePaletteManager()
    {
        // Actualizar nombres en SavedPalettes
        foreach (var pModel in SavedPalettes)
        {
            if (pModel.Name != pModel.OriginalName && !string.IsNullOrWhiteSpace(pModel.Name))
            {
                var original = _appSettings.SavedPalettes.Find(x => x.Name == pModel.OriginalName);
                if (original != null)
                {
                    original.Name = pModel.Name;
                    pModel.OriginalName = pModel.Name;
                }
            }
        }
        _appSettings.SavePalettes();
        IsPaletteManagerOpen = false;
    }

    [RelayCommand]
    private void DeleteAllPalettes()
    {
        _appSettings.SavedPalettes.Clear();
        _appSettings.SavePalettes();
        SavedPalettes.Clear();
    }

    [RelayCommand]
    private void DeletePalette(PaletteModel palette)
    {
        if (palette == null) return;
        var original = _appSettings.SavedPalettes.Find(x => x.Name == palette.OriginalName);
        if (original != null)
        {
            _appSettings.SavedPalettes.Remove(original);
            _appSettings.SavePalettes();
        }
        SavedPalettes.Remove(palette);
    }

    public bool IsNotDefaultPath => !string.Equals(LocalFilesPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCto3D"), StringComparison.OrdinalIgnoreCase);

    public int SelectedLanguageIndex
    {
        get => _appSettings.Language == "es-ES" ? 1 : 0;
        set
        {
            if (value < 0 || value > 1)
                return;

            string newLang = value == 1 ? "es-ES" : "en-US";
            if (_appSettings.Language != newLang)
            {
                _appSettings.Language = newLang;
                MCto3D.App.ChangeLanguage(newLang);
                MCto3D.Services.LanguageService.RaiseLanguageChanged();
                OnPropertyChanged(nameof(SelectedLanguageIndex));
            }
        }
    }

    partial void OnLocalFilesPathChanged(string value)
    {
        OnPropertyChanged(nameof(IsNotDefaultPath));
    }

    partial void OnShowFloorChanged(bool value)
    {
        _appSettings.ShowFloor = value;
    }

    partial void OnFloorColorChanged(Avalonia.Media.Color value)
    {
        _appSettings.FloorColorHex = value.ToString();
    }

    partial void OnModelColorChanged(Avalonia.Media.Color value)
    {
        _appSettings.ModelColorHex = value.ToString();
    }

    [RelayCommand]
    private async Task ChangeLocalFilesPathAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider != null)
            {
                var result = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = LanguageService.GetString("SettingsSelectNewFolderPath"),
                    AllowMultiple = false
                });

                if (result != null && result.Count > 0)
                {
                    string newFolderPath = result[0].Path.LocalPath;
                    
                    if (string.Equals(newFolderPath, LocalFilesPath, StringComparison.OrdinalIgnoreCase))
                        return;

                    IsMovingFiles = true;
                    string oldPath = LocalFilesPath;

                    string msgCalculating = LanguageService.GetString("SettingsCalculatingFiles");
                    string msgMovingFormat = LanguageService.GetString("SettingsMovingProgress");
                    string msgCleaning = LanguageService.GetString("SettingsCleaningOldFiles");

                    try
                    {
                        await Task.Run(async () =>
                        {
                            MovingProgressMessage = msgCalculating;
                            await Task.Delay(500);

                            if (Directory.Exists(oldPath))
                            {
                                string[] allFiles = Directory.GetFiles(oldPath, "*", SearchOption.AllDirectories);
                                int totalFiles = allFiles.Length;
                                int current = 0;
                                int updateAmount = Random.Shared.Next(50, 100);

                                foreach (string file in allFiles)
                                {
                                    string relPath = Path.GetRelativePath(oldPath, file);
                                    string destFile = Path.Combine(newFolderPath, relPath);
                                    string? destDir = Path.GetDirectoryName(destFile);

                                    if (destDir != null && !Directory.Exists(destDir))
                                        Directory.CreateDirectory(destDir);

                                    File.Copy(file, destFile, true);
                                    current++;

                                    if (current % updateAmount == 0 || current == totalFiles)
                                    {
                                        MovingProgressMessage = string.Format(msgMovingFormat, current, totalFiles);
                                        updateAmount = Random.Shared.Next(50, 100);
                                    }
                                }

                                MovingProgressMessage = msgCleaning;
                                await Task.Delay(500);
                                
                                Directory.Delete(oldPath, true);
                            }
                        });

                        _appSettings.LocalFilesPath = newFolderPath;
                        LocalFilesPath = newFolderPath;
                    }
                    catch (Exception ex)
                    {
                        MovingProgressMessage = string.Format(LanguageService.GetString("SettingsMovingError"), ex.Message);
                        await Task.Delay(3000);
                    }
                    finally
                    {
                        IsMovingFiles = false;
                    }
                }
            }
        }
    }

    [RelayCommand]
    private async Task RestoreDefaultLocalFilesPathAsync()
    {
        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCto3D");
        if (string.Equals(LocalFilesPath, defaultPath, StringComparison.OrdinalIgnoreCase))
            return;

        IsMovingFiles = true;
        string oldPath = LocalFilesPath;

        string msgCalculating = LanguageService.GetString("SettingsCalculatingFiles");
        string msgMovingFormat = LanguageService.GetString("SettingsMovingProgress");
        string msgCleaning = LanguageService.GetString("SettingsCleaningOldFiles");

        try
        {
            await Task.Run(async () =>
            {
                MovingProgressMessage = msgCalculating;
                await Task.Delay(500);

                if (Directory.Exists(oldPath))
                {
                    string[] allFiles = Directory.GetFiles(oldPath, "*", SearchOption.AllDirectories);
                    int totalFiles = allFiles.Length;
                    int current = 0;
                    int updateAmount = Random.Shared.Next(50, 100);

                    foreach (string file in allFiles)
                    {
                        string relPath = Path.GetRelativePath(oldPath, file);
                        string destFile = Path.Combine(defaultPath, relPath);
                        string? destDir = Path.GetDirectoryName(destFile);

                        if (destDir != null && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        File.Copy(file, destFile, true);
                        current++;

                        if (current % updateAmount == 0 || current == totalFiles)
                        {
                            MovingProgressMessage = string.Format(msgMovingFormat, current, totalFiles);
                            updateAmount = Random.Shared.Next(50, 100);
                        }
                    }

                    MovingProgressMessage = msgCleaning;
                    await Task.Delay(500);
                    
                    Directory.Delete(oldPath, true);
                }
            });

            _appSettings.LocalFilesPath = defaultPath;
            LocalFilesPath = defaultPath;
        }
        catch (Exception ex)
        {
            MovingProgressMessage = string.Format(LanguageService.GetString("SettingsMovingError"), ex.Message);
            await Task.Delay(3000);
        }
        finally
        {
            IsMovingFiles = false;
        }
    }

    [RelayCommand]
    private void OpenLocalFilesFolder()
    {
        if (Directory.Exists(LocalFilesPath))
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = LocalFilesPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }


    [RelayCommand]
    private async Task ExtractMinecraftFilesFromDefault()
    {
        _mainViewModel.LoadingVM.HasError = false;
        _mainViewModel.LoadingVM.StatusMessage = " ";
        _mainViewModel.LoadingVM.IsActive = true;
        try
        {
            var progressReporter = new Progress<string>(mensaje =>
            {
                _mainViewModel.LoadingVM.StatusMessage = mensaje;
            });

            var location = await Task.Run(() => _assetExtractorService.ExtractLegalAssets(_appSettings.LocalFilesPath, progressReporter));
            await Task.Delay(1500); // Dar un poco de tiempo para leer el mensaje final de extracción

            //procesar colores y guardarlos en JSON.
            _colorMappingService.BlockColors = await ColorGeneratorService.GenerateAndLoadColors(_appSettings.LocalFilesPath, progressReporter);
            await Task.Delay(1500);
            //update version
            McVersion = _appSettings.McVersion == null ? "-" : _appSettings.McVersion;
        }
        catch (Exception e)
        {
            _mainViewModel.LoadingVM.StatusMessage = $"Error: {e.Message}";
            await Task.Delay(3000);
        }
        finally
        {
            _mainViewModel.LoadingVM.IsActive = false;
        }


    }

    [RelayCommand]
    async Task ExtractMinecraftFilesFromCustom()
    {
        _mainViewModel.LoadingVM.HasError = false;
        _mainViewModel.LoadingVM.StatusMessage = " ";
        _mainViewModel.LoadingVM.IsActive = true;

        _mainViewModel.LoadingVM.StatusMessage = LanguageService.GetString("AssetExtractionWaitingStatus");

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
                            _mainViewModel.LoadingVM.StatusMessage = mensaje;
                        });

                        // Extracción de archivos
                        var location = await Task.Run(() => _assetExtractorService.ExtractLegalAssets(_appSettings.LocalFilesPath, progressReporter, selectedFolderPath));
                        await Task.Delay(1500);

                        //procesar colores y guardarlos en JSON.
                        _colorMappingService.BlockColors = await ColorGeneratorService.GenerateAndLoadColors(_appSettings.LocalFilesPath, progressReporter);
                        await Task.Delay(1500);

                        //update version
                        McVersion = _appSettings.McVersion == null ? "-" : _appSettings.McVersion;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al abrir el buscador o procesar: {ex.Message}");
            _mainViewModel.LoadingVM.StatusMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
        }
        finally
        {
            _mainViewModel.LoadingVM.IsActive = false;
        }
    }
}