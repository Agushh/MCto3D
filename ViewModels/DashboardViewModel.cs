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
using System.Linq;

namespace MCto3D.ViewModels;

public enum ColorAlgorithm
{
    SingleColor,
    CustomPalette,
    PredefinedPalette,
    KMeansAverage,
    KMeansReal,
    RawColors
}

public class SlicerOption
{
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "";
    public string UriScheme { get; set; } = "";
    public string ButtonText => $"{MCto3D.Services.Language_Service.GetString("ConverterOpenIn")} {Name}";
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;

    public DashboardViewModel(MainWindowViewModel navigationController)
    {
        _navigationController = navigationController;
        _selectedSlicer = SlicerOptions[0];
        
        LoadPalettes();
    }
    
    private string? _selectedFilePath;
    
    [ObservableProperty] private string _statusText = MCto3D.Services.Language_Service.GetString("StatusWaitingFile");

    [ObservableProperty] private bool _fillHoles = false;
    [ObservableProperty] private bool _useCustomFillColor = false;
    [ObservableProperty] private Avalonia.Media.Color _customFillColor = Avalonia.Media.Colors.Green;
    [ObservableProperty] private bool _isFileLoaded = false;
    [ObservableProperty] private string _originalFileName = "";
    
    [ObservableProperty] private float _bs = 1.0f;
    [ObservableProperty] private string _geometryMode = "Bloques sólidos";
    [ObservableProperty] private bool _isLoadingMesh = false;

    [ObservableProperty] private int _selectedGeometryModeIndex = 0;
    partial void OnSelectedGeometryModeIndexChanged(int value)
    {
        if (IsFileLoaded)
        {
            UpdateLiveMesh();
        }
    }

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

    partial void OnFillHolesChanged(bool value)
    {
        if (IsFileLoaded)
        {
            UpdateLiveMesh();
        }
    }

    partial void OnUseCustomFillColorChanged(bool value)
    {
        if (IsFileLoaded)
        {
            UpdateLiveMesh();
        }
    }

    partial void OnCustomFillColorChanged(Avalonia.Media.Color value)
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
            
            var result = await Task.Run(() => 
            {
                MCto3D.Services.structureData strData;
                if (_selectedFilePath.EndsWith(".litematic", System.StringComparison.OrdinalIgnoreCase) || 
                    _selectedFilePath.EndsWith(".schematic", System.StringComparison.OrdinalIgnoreCase))
                {
                    strData = FileReader_Service.readLitematic(_selectedFilePath);
                }
                else
                {
                    strData = FileReader_Service.readNBT(_selectedFilePath);
                }
                
                if (FillHoles)
                {
                    MCto3D.Services.Topology_Service.ProcessEnclosedSpaces(strData.voxelGrid);
                }
                
                if (SelectedAlgorithm != ColorAlgorithm.SingleColor && ExportFormat == "3MF")
                {
                    List<System.Drawing.Color>? userColors = null;
                    int? k = null;
                    bool useKMedoids = false;

                    if (SelectedAlgorithm == ColorAlgorithm.KMeansAverage) k = KMeansCount;
                    else if (SelectedAlgorithm == ColorAlgorithm.KMeansReal) { k = KMeansCount; useKMedoids = true; }
                    else if (SelectedAlgorithm == ColorAlgorithm.PredefinedPalette)
                    {
                        userColors = MCto3D.Services.ColorClustering_Service.GetPredefinedPalette(PredefinedPaletteSize);
                    }
                    else if (SelectedAlgorithm == ColorAlgorithm.CustomPalette)
                    {
                        userColors = new List<System.Drawing.Color>();
                        if (UserDefinedColors != null)
                        {
                            foreach(var ac in UserDefinedColors) 
                                userColors.Add(System.Drawing.Color.FromArgb(255, ac.Color.R, ac.Color.G, ac.Color.B));
                        }
                        if (userColors.Count == 0) userColors.Add(System.Drawing.Color.White);
                    }
                    
                    bool isFullGeom = SelectedGeometryModeIndex == 1;
                    
                    var multiData = FileReader_Service.CreateMultiColorData(strData, k, userColors, useKMedoids, SelectedAlgorithm == ColorAlgorithm.RawColors, FillHoles, UseCustomFillColor, System.Drawing.Color.FromArgb(CustomFillColor.A, CustomFillColor.R, CustomFillColor.G, CustomFillColor.B));
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => RawColorCount = multiData.voxelGrid.Count);
                    
                    return Mesh_Service.GenerateMultiColorMeshes(multiData, strData, Bs, isFullGeom);
                }
                else
                {
                    var tris = SelectedGeometryModeIndex == 1 
                             ? Mesh_Service.GenerateFullGeometryMesh(strData, Bs)
                             : Mesh_Service.GenerateMesh(strData, Bs);
                             
                    var dict = new Dictionary<System.Drawing.Color, List<Triangle>>();
                    dict[System.Drawing.Color.Gray] = tris;
                    return dict;
                }
            });
                     
            int totalTris = 0;
            var allTris = new List<Triangle>();
            foreach(var list in result.Values) 
            {
                totalTris += list.Count;
                allTris.AddRange(list);
            }
            
            Debug.WriteLine($"DashboardViewModel: Mesh generated with {totalTris} triangles.");
            ColoredMeshes = result;
            MeshTriangles = allTris;
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

    partial void OnExportFormatChanged(string value)
    {
        if (IsFileLoaded) UpdateLiveMesh();
    }


    [ObservableProperty] private ColorAlgorithm _selectedAlgorithm = ColorAlgorithm.SingleColor;
    
    public bool IsSingleColorMode => SelectedAlgorithm == ColorAlgorithm.SingleColor;
    public bool IsMultiColorMode => SelectedAlgorithm != ColorAlgorithm.SingleColor;

    partial void OnSelectedAlgorithmChanged(ColorAlgorithm value) 
    { 
        OnPropertyChanged(nameof(IsSingleColorMode));
        OnPropertyChanged(nameof(IsMultiColorMode));
        if (IsFileLoaded) UpdateLiveMesh(); 
    }

    [ObservableProperty] private Dictionary<System.Drawing.Color, List<Triangle>> _coloredMeshes = new();
    
    [ObservableProperty] private int _kMeansCount = 4;
    [ObservableProperty] private int _predefinedPaletteSize = 16;
    
    partial void OnPredefinedPaletteSizeChanged(int value) { if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.PredefinedPalette) UpdateLiveMesh(); }
    [ObservableProperty] private ObservableCollection<CustomColorItem> _userDefinedColors = new();
    
    [ObservableProperty] private ObservableCollection<CustomPaletteModel> _availablePalettes = new();
    
    [ObservableProperty] private CustomPaletteModel _selectedPalette;

    [ObservableProperty] private string _newPaletteName = string.Empty;
    
    [ObservableProperty] private string _selectedPaletteNameInput = string.Empty;
    partial void OnSelectedPaletteNameInputChanged(string value)
    {
        HasUnsavedPaletteChanges = true;
    }

    [ObservableProperty] private bool _hasUnsavedPaletteChanges = false;

    partial void OnSelectedPaletteChanged(CustomPaletteModel value)
    {
        if (value == null) return;
        
        NewPaletteName = string.Empty;
        SelectedPaletteNameInput = value.Name;
        HasUnsavedPaletteChanges = false;

        // Load the colors into UserDefinedColors
        UserDefinedColors.Clear();
        if (value.Name == "Personalizada..." || value.ColorsHex == null || value.ColorsHex.Count == 0)
        {
            // Default blank palette
            var item = new CustomColorItem { Color = Avalonia.Media.Color.Parse("#FFFFFF") };
            item.OnColorChangedCallback = () => { HasUnsavedPaletteChanges = true; if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.CustomPalette) UpdateLiveMesh(); };
            UserDefinedColors.Add(item);
        }
        else
        {
            foreach (var hex in value.ColorsHex)
            {
                try
                {
                    var item = new CustomColorItem { Color = Avalonia.Media.Color.Parse(hex) };
                    item.OnColorChangedCallback = () => { HasUnsavedPaletteChanges = true; if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.CustomPalette) UpdateLiveMesh(); };
                    UserDefinedColors.Add(item);
                }
                catch { }
            }
        }
        if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.CustomPalette) UpdateLiveMesh();
        
        OnPropertyChanged(nameof(IsSavedPaletteSelected));
        OnPropertyChanged(nameof(IsCustomPaletteSelected));
    }

    public bool IsSavedPaletteSelected => SelectedPalette != null && SelectedPalette.Name != "Personalizada...";
    public bool IsCustomPaletteSelected => SelectedPalette != null && SelectedPalette.Name == "Personalizada...";

    [RelayCommand]
    private void SaveCustomPalette(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            int count = AppSettings_Service.SavedPalettes.Count + 1;
            name = $"Personalizada-{count}";
        }
        
        var newPalette = new CustomPaletteModel { Name = name };
        foreach (var c in UserDefinedColors)
        {
            newPalette.ColorsHex.Add(c.Color.ToString());
        }
        
        AppSettings_Service.SavedPalettes.Add(newPalette);
        AppSettings_Service.SavePalettes();
        
        LoadPalettes();
        SelectedPalette = AvailablePalettes.FirstOrDefault(p => p.Name == name);
    }

    [RelayCommand]
    private void UpdateSavedPalette()
    {
        if (SelectedPalette == null || SelectedPalette.Name == "Personalizada...") return;
        
        var existing = AppSettings_Service.SavedPalettes.FirstOrDefault(p => p.Name == SelectedPalette.Name);
        if (existing != null)
        {
            existing.Name = string.IsNullOrWhiteSpace(SelectedPaletteNameInput) ? existing.Name : SelectedPaletteNameInput;
            SelectedPalette.Name = existing.Name;

            existing.ColorsHex.Clear();
            foreach (var c in UserDefinedColors)
            {
                existing.ColorsHex.Add(c.Color.ToString());
            }
            AppSettings_Service.SavePalettes();
            HasUnsavedPaletteChanges = false;
        }
        LoadPalettes();
        SelectedPalette = AvailablePalettes.FirstOrDefault(p => p.Name == existing?.Name);
    }

    [RelayCommand]
    private void DeleteSavedPalette()
    {
        if (SelectedPalette == null || SelectedPalette.Name == "Personalizada...") return;
        
        var existing = AppSettings_Service.SavedPalettes.FirstOrDefault(p => p.Name == SelectedPalette.Name);
        if (existing != null)
        {
            AppSettings_Service.SavedPalettes.Remove(existing);
            AppSettings_Service.SavePalettes();
        }
        LoadPalettes();
    }

    public void LoadPalettes()
    {
        AvailablePalettes.Clear();
        foreach (var p in AppSettings_Service.SavedPalettes)
        {
            AvailablePalettes.Add(p);
        }
        AvailablePalettes.Add(new CustomPaletteModel { Name = MCto3D.Services.Language_Service.GetString("ConverterPaletteCustom") });
        if (SelectedPalette == null || !AvailablePalettes.Contains(SelectedPalette))
        {
            SelectedPalette = AvailablePalettes[^1]; // default to Custom
        }
    }

    [RelayCommand]
    private void AddUserColor()
    {
        var item = new CustomColorItem { Color = Avalonia.Media.Color.Parse("#FFFFFF") };
        item.OnColorChangedCallback = () => { HasUnsavedPaletteChanges = true; if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.CustomPalette) UpdateLiveMesh(); };
        UserDefinedColors.Add(item);
        HasUnsavedPaletteChanges = true;
        if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.CustomPalette) UpdateLiveMesh();
    }

    [RelayCommand]
    private void RemoveUserColor(CustomColorItem item)
    {
        if (UserDefinedColors.Count <= 1) return;
        if (UserDefinedColors.Contains(item))
        {
            UserDefinedColors.Remove(item);
            HasUnsavedPaletteChanges = true;
            if (IsFileLoaded && SelectedAlgorithm == ColorAlgorithm.CustomPalette) UpdateLiveMesh();
        }
    }

    partial void OnKMeansCountChanged(int value) { if (IsFileLoaded && (SelectedAlgorithm == ColorAlgorithm.KMeansAverage || SelectedAlgorithm == ColorAlgorithm.KMeansReal)) UpdateLiveMesh(); }

    [ObservableProperty] private bool _showFloor = MCto3D.Services.AppSettings_Service.ShowFloor;
    [ObservableProperty] private Avalonia.Media.Color _floorColor = Avalonia.Media.Color.Parse(MCto3D.Services.AppSettings_Service.FloorColorHex);
    [ObservableProperty] private Avalonia.Media.Color _modelColor = Avalonia.Media.Color.Parse(MCto3D.Services.AppSettings_Service.ModelColorHex);

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

    [ObservableProperty] private int _rawColorCount = 0;
    
    [ObservableProperty] private SlicerOption _selectedSlicer;

    [ObservableProperty] private bool _isSavePopupOpen = false;
    [ObservableProperty] private string _newProjectName = string.Empty;



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
            GeometryMode = SelectedGeometryModeIndex == 1 ? "Geometrías completas" : "Bloques sólidos",
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
                    new FilePickerFileType("Archivos de Estructura (*.nbt, *.litematic, *.schematic)")
                    {
                        Patterns = new[] { "*.nbt", "*.litematic", "*.schematic" }
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
                    StatusText = $"{MCto3D.Services.Language_Service.GetString("StatusFileLoaded")} {Path.GetFileName(_selectedFilePath)}";
                    UpdateLiveMesh();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"{MCto3D.Services.Language_Service.GetString("StatusErrorOpenFile")} {ex.Message}";
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
        StatusText = MCto3D.Services.Language_Service.GetString("StatusWaitingFile");
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
            StatusText = MCto3D.Services.Language_Service.GetString("StatusProcessingGeom");

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
                IModelWriter writer = ExportFormat == "STL" ? new StlWriter_Service() : new ThreeMfWriter_Service();
                writer.Write(rutaDestino, ColoredMeshes);

                StatusText = MCto3D.Services.Language_Service.GetString("StatusExportSuccess");
            }
            else
            {
                StatusText = MCto3D.Services.Language_Service.GetString("StatusExportCancel");
            }
        }
        catch (Exception ex)
        {
            StatusText = MCto3D.Services.Language_Service.GetString("StatusExportError");
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
            StatusText = $"{MCto3D.Services.Language_Service.GetString("StatusFileLoaded")} {Path.GetFileName(filePath)}";
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
            StatusText = MCto3D.Services.Language_Service.GetString("StatusProcessingGeom");

            string extension = ExportFormat.ToLower();
            string suggestedName = string.IsNullOrWhiteSpace(OriginalFileName) ? "modelo" : OriginalFileName;
            string tempFile = Path.Combine(Path.GetTempPath(), $"{suggestedName}_{Guid.NewGuid():N}.{extension}");

            IModelWriter writer = ExportFormat == "STL" ? new StlWriter_Service() : new ThreeMfWriter_Service();
            writer.Write(tempFile, ColoredMeshes);

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
            StatusText = MCto3D.Services.Language_Service.GetString("StatusSentToSlicer");
        }
        catch (Exception ex)
        {
            StatusText = $"{MCto3D.Services.Language_Service.GetString("StatusErrorOpenFile")} {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    public partial class CustomColorItem : ObservableObject
    {
        [ObservableProperty] private Avalonia.Media.Color _color;
        
        public Action? OnColorChangedCallback { get; set; }
        
        partial void OnColorChanged(Avalonia.Media.Color value)
        {
            OnColorChangedCallback?.Invoke();
        }
    }
}