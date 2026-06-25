using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace MCto3D.ViewModels;

public class SlicerOption
{
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "";
    public string UriScheme { get; set; } = "";
    public string ButtonText => $"Abrir en {Name}";
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;

    private string? _selectedFilePath;
    
    [ObservableProperty] private string _statusText = "Esperando archivo NBT...";
    [ObservableProperty] private bool _isFileLoaded = false;
    [ObservableProperty] private string _originalFileName = "";
    
    [ObservableProperty] private float _bs = 1.0f;
    [ObservableProperty] private string _geometryMode = "Bloques sólidos";
    [ObservableProperty] private bool _isLoadingMesh = false;

    [ObservableProperty] private List<Triangle> _meshTriangles = new();

    partial void OnBsChanged(float value)
    {
        if (IsFileLoaded)
        {
            UpdateLiveMesh();
        }
    }

    partial void OnGeometryModeChanged(string value)
    {
        if (IsFileLoaded)
        {
            UpdateLiveMesh();
        }
    }

    private async void UpdateLiveMesh()
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;
        try
        {
            IsLoadingMesh = true;
            Debug.WriteLine($"DashboardViewModel: Generating mesh for {_selectedFilePath} with Bs={Bs}");
            
            var tris = await Task.Run(() => 
            {
                var strData = FileReader_Service.readNBT(_selectedFilePath);
                return GeometryMode == "Geometrías completas" 
                         ? Mesh_Service.GenerateFullGeometryMesh(strData, Bs)
                         : Mesh_Service.GenerateMesh(strData, Bs);
            });
                     
            Debug.WriteLine($"DashboardViewModel: Mesh generated with {tris.Count} triangles.");
            MeshTriangles = tris;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating mesh: {ex.Message}");
        }
        finally
        {
            IsLoadingMesh = false;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is3MfSelected))]
    private string _exportFormat = "STL";

    [ObservableProperty] private bool _isSingleColorMode = true;
    [ObservableProperty] private bool _isMultiColorMode = false;

    [ObservableProperty] private bool _showFloor = false;
    [ObservableProperty] private Avalonia.Media.Color _floorColor = Avalonia.Media.Color.Parse("#1A1A1A");

    [ObservableProperty] private bool _overrideModelColor = false;
    [ObservableProperty] private Avalonia.Media.Color _modelColor = Avalonia.Media.Color.Parse("#FFFFFF");

    // Thumbnails
    [ObservableProperty] private int _selectedThumbnailIndex = 0;
    [ObservableProperty] private ObservableCollection<WriteableBitmap>? _thumbnails;
    public Func<Task<List<WriteableBitmap>>>? CaptureThumbnailsFunc { get; set; }

    public bool Is3MfSelected => ExportFormat == "3MF";

    public List<SlicerOption> SlicerOptions { get; } = new()
    {
        new SlicerOption { Name = "OrcaSlicer", ColorHex = "#027D52", UriScheme = "orcaslicer://" },
        new SlicerOption { Name = "BambuStudio", ColorHex = "#00AE42", UriScheme = "bambustudio://" },
        new SlicerOption { Name = "PrusaSlicer", ColorHex = "#EA6B24", UriScheme = "prusaslicer://" },
        new SlicerOption { Name = "Cura", ColorHex = "#0055FF", UriScheme = "cura://" }
    };

    [ObservableProperty]
    private SlicerOption _selectedSlicer;

    [ObservableProperty] private bool _isSavePopupOpen = false;
    [ObservableProperty] private string _newProjectName = string.Empty;

    public DashboardViewModel(MainWindowViewModel navigationController)
    {
        _navigationController = navigationController;
        _selectedSlicer = SlicerOptions[0];
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationController.NavigateToHomeCommand.Execute(null);
    }

    [RelayCommand]
    private async Task OpenSavePopup()
    {
        NewProjectName = OriginalFileName;
        SelectedThumbnailIndex = 0; // Por defecto la 1
        
        if (CaptureThumbnailsFunc != null)
        {
            var images = await CaptureThumbnailsFunc();
            Thumbnails = new ObservableCollection<WriteableBitmap>(images);
        }

        IsSavePopupOpen = true;
    }

    [RelayCommand]
    private void CloseSavePopup()
    {
        IsSavePopupOpen = false;
        NewProjectName = string.Empty;
    }

    [RelayCommand]
    private void SelectThumbnail(string indexStr)
    {
        if (int.TryParse(indexStr, out int index))
        {
            SelectedThumbnailIndex = index;
        }
    }

    [RelayCommand]
    private void ConfirmSaveToApp()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName) || string.IsNullOrWhiteSpace(_selectedFilePath)) return;

        string thumbnailPath = string.Empty;

        // Guardar miniatura si existe
        if (Thumbnails != null && Thumbnails.Count > SelectedThumbnailIndex)
        {
            try
            {
                var bmp = Thumbnails[SelectedThumbnailIndex];
                var appDataFolder = Path.Combine(MCto3D.Services.AppSettings_Service.LocalFilesPath, "Thumbnails");
                if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);
                
                string fileName = $"{Guid.NewGuid()}.png";
                thumbnailPath = Path.Combine(appDataFolder, fileName);
                
                bmp.Save(thumbnailPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando miniatura: {ex.Message}");
            }
        }

        var newProject = new SavedProject
        {
            Name = NewProjectName,
            OriginalFilePath = _selectedFilePath,
            ThumbnailPath = thumbnailPath,
            BlockScale = Bs,
            GeometryMode = GeometryMode,
            ExportFormat = ExportFormat,
            IsSingleColorMode = IsSingleColorMode
        };

        ProjectStorageService.AddProject(newProject);
        
        IsSavePopupOpen = false;
        StatusText = "¡Guardado en Mis Archivos exitosamente!";
        
        // Update MyFilesVM so the new project is listed immediately
        _navigationController.MyFilesVM?.LoadData();
    }

    [RelayCommand]
    private async Task LoadFile(Control visualTarget)
    {
        if (visualTarget == null) return;

        var topLevel = TopLevel.GetTopLevel(visualTarget);
        if (topLevel == null) return;

        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Seleccionar archivo NBT de Minecraft",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Estructura Minecraft (*.nbt)")
                    {
                        Patterns = new[] { "*.nbt" }
                    }
                }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files != null && files.Count > 0)
            {
                _selectedFilePath = files[0].Path.LocalPath;

                if (!string.IsNullOrEmpty(_selectedFilePath))
                {
                    IsFileLoaded = true;
                    OriginalFileName = Path.GetFileNameWithoutExtension(_selectedFilePath);
                    StatusText = $"Archivo cargado: {Path.GetFileName(_selectedFilePath)}";
                    UpdateLiveMesh();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error al abrir el archivo: {ex.Message}";
            IsFileLoaded = false;
        }
    }

    [RelayCommand]
    private void CancelFile()
    {
        _selectedFilePath = null;
        OriginalFileName = "";
        IsFileLoaded = false;
        MeshTriangles = new List<Triangle>();
        StatusText = "Esperando archivo NBT...";
    }

    [RelayCommand]
    private void ChangeSlicer(SlicerOption newSlicer)
    {
        if (newSlicer != null)
        {
            SelectedSlicer = newSlicer;
        }
    }

    [RelayCommand]
    private async Task ExportFile(Control visualTarget)
    {
        if (visualTarget == null || string.IsNullOrEmpty(_selectedFilePath)) return;

        var topLevel = TopLevel.GetTopLevel(visualTarget);
        if (topLevel == null) return;

        try
        {
            StatusText = "Procesando geometría para exportar...";
            List<Triangle> malla = MeshTriangles;

            string extensionPorDefecto = ExportFormat.ToLower();
            string nombreFiltro = ExportFormat == "STL" ? "Archivo Estereolitografía (*.stl)" : "3D Manufacturing Format (*.3mf)";
            string patronExtension = $"*.{extensionPorDefecto}";
            
            string suggestedName = string.IsNullOrWhiteSpace(OriginalFileName) ? "modelo_minecraft" : OriginalFileName;

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

                if (ExportFormat == "STL")
                {
                    StlGenerator.CreateBinaryStlWithColor(rutaDestino, malla, System.Drawing.Color.Gray);
                }
                StatusText = "¡Exportado con éxito!";
            }
            else
            {
                StatusText = "Exportación cancelada.";
            }
        }
        catch (Exception ex)
        {
            StatusText = "Error al cargar el archivo.";
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    public void LoadDirectFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            _selectedFilePath = filePath;
            IsFileLoaded = true;
            OriginalFileName = Path.GetFileNameWithoutExtension(filePath);
            StatusText = $"Archivo cargado directo: {Path.GetFileName(filePath)}";
            UpdateLiveMesh();
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
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        try
        {
            StatusText = "Procesando geometría para Slicer...";
            List<Triangle> malla = MeshTriangles;

            string extension = ExportFormat.ToLower();
            string suggestedName = string.IsNullOrWhiteSpace(OriginalFileName) ? "modelo" : OriginalFileName;
            string tempFile = Path.Combine(Path.GetTempPath(), $"{suggestedName}_{Guid.NewGuid():N}.{extension}");

            if (ExportFormat == "STL")
            {
                StlGenerator.CreateBinaryStlWithColor(tempFile, malla, System.Drawing.Color.Gray);
            }

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
            StatusText = "¡Enviado al Slicer!";
        }
        catch (Exception ex)
        {
            StatusText = $"Error al abrir el slicer: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error al abrir el slicer: {ex.Message}");
        }
    }
}