using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
using Microsoft.Win32;
using System.Threading.Tasks;
using MCto3D.Services.ExportedFilesWriting;

namespace MCto3D.ViewModels;

public partial class ExportViewModel : ViewModelBase
{
    private readonly AppSettingsService _appSettings;
    
    public event EventHandler? ExportFormatChanged;
    public event EventHandler<string>? StatusTextChanged;
    
    public Func<Dictionary<System.Drawing.Color, List<Triangle>>>? GetColoredMeshesFunc { get; set; }
    public Func<string>? GetOriginalFileNameFunc { get; set; }
    public Func<string?>? GetSelectedFilePathFunc { get; set; }
    
    public ExportViewModel(AppSettingsService appSettings)
    {
        _appSettings = appSettings;
        _selectedSlicer = SlicerOptions[0];
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is3MfSelected))]
    private string _exportFormat = "STL";

    partial void OnExportFormatChanged(string value)
    {
        ExportFormatChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public bool Is3MfSelected => ExportFormat == "3MF";

    public List<SlicerOption> SlicerOptions { get; } = new()
    {
        new SlicerOption { Name = "OrcaSlicer", ColorHex = "#027D52", UriScheme = "orcaslicer://" },
        new SlicerOption { Name = "BambuStudio", ColorHex = "#00AE42", UriScheme = "bambustudio://" },
        new SlicerOption { Name = "PrusaSlicer", ColorHex = "#EA6B24", UriScheme = "prusaslicer://" },
        new SlicerOption { Name = "Cura", ColorHex = "#0055FF", UriScheme = "cura://" }
    };

    [ObservableProperty] private SlicerOption _selectedSlicer;

    [RelayCommand]
    private void ChangeSlicer(SlicerOption newSlicer)
    {
        if (newSlicer != null)
        {
            SelectedSlicer = newSlicer;
        }
    }

    private string GetSlicerExecutableFromRegistry(string uriScheme)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;

#pragma warning disable CA1416
        try
        {
            string scheme = uriScheme.Replace("://", "");
            using var key = Registry.ClassesRoot.OpenSubKey($@"{scheme}\shell\open\command");
            if (key != null)
            {
                string val = key.GetValue("")?.ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    int exeIndex = val.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                    if (exeIndex > 0)
                    {
                        return val.Substring(0, exeIndex + 4).Trim(' ', '"');
                    }
                }
            }
        }
        catch { }
#pragma warning restore CA1416
        return null;
    }

    [RelayCommand]
    private void OpenInSlicer()
    {
        string? selectedFilePath = GetSelectedFilePathFunc?.Invoke();
        if (string.IsNullOrEmpty(selectedFilePath)) return;

        try
        {
            StatusTextChanged?.Invoke(this, MCto3D.Services.LanguageService.GetString("StatusProcessingGeom"));

            string extension = ExportFormat.ToLower();
            string originalFileName = GetOriginalFileNameFunc?.Invoke() ?? "modelo";
            string suggestedName = string.IsNullOrWhiteSpace(originalFileName) ? "modelo" : originalFileName;
            string tempFile = Path.Combine(Path.GetTempPath(), $"{suggestedName}_{Guid.NewGuid():N}.{extension}");

            var meshes = GetColoredMeshesFunc?.Invoke();
            if (meshes == null) return;

            IModelWriter writer = ExportFormat == "STL" ? new StlWriterService() : new ThreeMfWriterService(_appSettings.Use3MfAssemblyMode);
            writer.Write(tempFile, meshes);

            string exePath = GetSlicerExecutableFromRegistry(SelectedSlicer.UriScheme);

            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"\"{tempFile}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });
            }
            StatusTextChanged?.Invoke(this, MCto3D.Services.LanguageService.GetString("StatusSentToSlicer"));
        }
        catch (Exception ex)
        {
            StatusTextChanged?.Invoke(this, $"{MCto3D.Services.LanguageService.GetString("StatusErrorOpenFile")} {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportFile(Control visualTarget)
    {
        string? selectedFilePath = GetSelectedFilePathFunc?.Invoke();
        if (visualTarget == null || string.IsNullOrEmpty(selectedFilePath)) return;

        var topLevel = TopLevel.GetTopLevel(visualTarget);
        if (topLevel == null) return;

        try
        {
            StatusTextChanged?.Invoke(this, MCto3D.Services.LanguageService.GetString("StatusProcessingGeom"));

            string extensionPorDefecto = ExportFormat.ToLower();
            string nombreFiltro = ExportFormat == "STL" ? "Archivo Estereolitografía (*.stl)" : "3D Manufacturing Format (*.3mf)";
            string patronExtension = $"*.{extensionPorDefecto}";
            
            string originalFileName = GetOriginalFileNameFunc?.Invoke() ?? "modelo_minecraft";
            string suggestedName = string.IsNullOrWhiteSpace(originalFileName) ? "modelo_minecraft" : originalFileName;

            var saveOptions = new FilePickerSaveOptions
            {
                Title = $"Exportar modelo como {ExportFormat}",
                DefaultExtension = extensionPorDefecto,
                SuggestedFileName = suggestedName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(nombreFiltro)
                    {
                        Patterns = new[] { patronExtension }
                    }
                }
            };

            var fileLocation = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);

            if (fileLocation != null)
            {
                string rutaDestino = fileLocation.Path.LocalPath;
                var meshes = GetColoredMeshesFunc?.Invoke();
                if (meshes != null)
                {
                    IModelWriter writer = ExportFormat == "STL" ? new StlWriterService() : new ThreeMfWriterService(_appSettings.Use3MfAssemblyMode);
                    writer.Write(rutaDestino, meshes);
                }

                StatusTextChanged?.Invoke(this, MCto3D.Services.LanguageService.GetString("StatusExportSuccess"));
            }
            else
            {
                StatusTextChanged?.Invoke(this, MCto3D.Services.LanguageService.GetString("StatusExportCancel"));
            }
        }
        catch (Exception ex)
        {
            StatusTextChanged?.Invoke(this, MCto3D.Services.LanguageService.GetString("StatusExportError"));
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}
