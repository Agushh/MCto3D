using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System;
using System.Collections.ObjectModel;
using MCto3D.Services;

namespace MCto3D.ViewModels;

public partial class PaletteModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
}

public partial class SettingsViewModel : ViewModelBase
{
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
    private void OpenPaletteManager() => IsPaletteManagerOpen = true;

    [RelayCommand]
    private void ClosePaletteManager() => IsPaletteManagerOpen = false;

    [RelayCommand]
    private void SavePaletteManager() => IsPaletteManagerOpen = false;

    [RelayCommand]
    private void DeleteAllPalettes() { }

    [RelayCommand]
    private void DeletePalette(PaletteModel palette) { }

    private string GetLocalizedString(string key)
    {
        if (Avalonia.Application.Current != null && Avalonia.Application.Current.TryGetResource(key, Avalonia.Application.Current.ActualThemeVariant, out var res) && res is string s)
        {
            return s;
        }
        return key;
    }

    private readonly MainWindowViewModel _mainViewModel;

    [ObservableProperty]
    private string _loadedMinecraftVersion = "NaN";

    [ObservableProperty]
    private string _localFilesPath;

    [ObservableProperty]
    private bool _isMovingFiles;

    [ObservableProperty]
    private string _movingProgressMessage = "";

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
                OnPropertyChanged(nameof(SelectedLanguageIndex));
            }
        }
    }

    partial void OnLocalFilesPathChanged(string value)
    {
        OnPropertyChanged(nameof(IsNotDefaultPath));
    }

    private readonly AppSettingsService _appSettings;
    
    public SettingsViewModel(MainWindowViewModel mainViewModel, AppSettingsService appSettings)
    {
        _mainViewModel = mainViewModel;
        _appSettings = appSettings;
        _localFilesPath = _appSettings.LocalFilesPath;
        _ = UpdateMinecraftVersionAsync();
    }

    public async Task UpdateMinecraftVersionAsync()
    {
        try
        {
            string versionFile = Path.Combine(LocalFilesPath, "MinecraftExtractedAssets", "version.txt");
            if (File.Exists(versionFile))
            {
                LoadedMinecraftVersion = (await File.ReadAllTextAsync(versionFile)).Trim();
            }
            else
            {
                LoadedMinecraftVersion = "NaN";
            }
        }
        catch
        {
            LoadedMinecraftVersion = "NaN";
        }
    }

    [RelayCommand]
    private void ReloadBaseFiles()
    {
        // Vuelve a mostrar la pantalla de bienvenida para cargar archivos
        _mainViewModel.IsWelcomeScreenActive = true;
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
                    Title = "Selecciona la nueva ruta para los archivos locales",
                    AllowMultiple = false
                });

                if (result != null && result.Count > 0)
                {
                    string newFolderPath = result[0].Path.LocalPath;
                    
                    if (string.Equals(newFolderPath, LocalFilesPath, StringComparison.OrdinalIgnoreCase))
                        return;

                    IsMovingFiles = true;
                    string oldPath = LocalFilesPath;

                    string msgCalculating = GetLocalizedString("SettingsCalculatingFiles");
                    string msgMovingFormat = GetLocalizedString("SettingsMovingProgress");
                    string msgCleaning = GetLocalizedString("SettingsCleaningOldFiles");

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
                                    string destDir = Path.GetDirectoryName(destFile);

                                    if (!Directory.Exists(destDir))
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
                        MovingProgressMessage = string.Format(GetLocalizedString("SettingsMovingError"), ex.Message);
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

        string msgCalculating = GetLocalizedString("SettingsCalculatingFiles");
        string msgMovingFormat = GetLocalizedString("SettingsMovingProgress");
        string msgCleaning = GetLocalizedString("SettingsCleaningOldFiles");

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
                        string destDir = Path.GetDirectoryName(destFile);

                        if (!Directory.Exists(destDir))
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
            MovingProgressMessage = string.Format(GetLocalizedString("SettingsMovingError"), ex.Message);
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
}
