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
    public string ButtonText => $"{MCto3D.Services.LanguageService.GetString("ConverterOpenIn")} {Name}";
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;

    private readonly IAppSettingsService _appSettings;
    private readonly IProjectStorageService _projectStorage;
    private readonly IFileReaderService _fileReaderService;
    private readonly IMeshService _meshService;
    private readonly ITopologyService _topologyService;
    private readonly IColorClusteringService _colorClusteringService;

    public PaletteManagerViewModel PaletteVM { get; }
    public ExportViewModel ExportVM { get; }
    public ProjectSaveViewModel ProjectSaveVM { get; }

    public DashboardViewModel(MainWindowViewModel navigationController, IAppSettingsService appSettings, IProjectStorageService projectStorage, IFileReaderService fileReaderService, IMeshService meshService, ITopologyService topologyService, IColorClusteringService colorClusteringService)
    {
        _navigationController = navigationController;
        _appSettings = appSettings;
        _projectStorage = projectStorage;
        _fileReaderService = fileReaderService;
        _meshService = meshService;
        _topologyService = topologyService;
        _colorClusteringService = colorClusteringService;
        
        PaletteVM = new PaletteManagerViewModel(appSettings, colorClusteringService);
        ExportVM = new ExportViewModel(appSettings);
        ProjectSaveVM = new ProjectSaveViewModel(navigationController, appSettings, projectStorage);

        PaletteVM.PaletteChanged += (s, e) => { if (IsFileLoaded) UpdateLiveMesh(); };
        ExportVM.ExportFormatChanged += (s, e) => { if (IsFileLoaded) UpdateLiveMesh(); };
        ExportVM.StatusTextChanged += (s, msg) => StatusText = msg;
        ProjectSaveVM.StatusTextChanged += (s, msg) => StatusText = msg;

        ExportVM.GetColoredMeshesFunc = () => ColoredMeshes;
        ExportVM.GetOriginalFileNameFunc = () => OriginalFileName;
        ExportVM.GetSelectedFilePathFunc = () => _selectedFilePath;

        ProjectSaveVM.GetOriginalFileNameFunc = () => OriginalFileName;
        ProjectSaveVM.GetSelectedFilePathFunc = () => _selectedFilePath;
        ProjectSaveVM.GetBlockScaleFunc = () => Bs;
        ProjectSaveVM.GetSelectedGeometryModeIndexFunc = () => SelectedGeometryModeIndex;
        ProjectSaveVM.GetExportFormatFunc = () => ExportVM.ExportFormat;
        ProjectSaveVM.GetIsSingleColorModeFunc = () => PaletteVM.IsSingleColorMode;
        
        try { _floorColor = Avalonia.Media.Color.Parse(_appSettings.FloorColorHex); } catch { _floorColor = Avalonia.Media.Colors.DarkGray; }
        try { _modelColor = Avalonia.Media.Color.Parse(_appSettings.ModelColorHex); } catch { _modelColor = Avalonia.Media.Colors.White; }
    }
    
    private string? _selectedFilePath;
    
    [ObservableProperty] private string _statusText = MCto3D.Services.LanguageService.GetString("StatusWaitingFile");

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

    private System.Threading.CancellationTokenSource? _meshUpdateCts;

    private async void UpdateLiveMesh()
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        _meshUpdateCts?.Cancel();
        _meshUpdateCts = new System.Threading.CancellationTokenSource();
        var token = _meshUpdateCts.Token;

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
                    strData = _fileReaderService.readLitematic(_selectedFilePath);
                }
                else
                {
                    strData = _fileReaderService.readNBT(_selectedFilePath);
                }
                
                token.ThrowIfCancellationRequested();
                
                if (FillHoles)
                {
                    _topologyService.ProcessEnclosedSpaces(strData.voxelGrid);
                }
                
                token.ThrowIfCancellationRequested();
                
                if (PaletteVM.SelectedAlgorithm != ColorAlgorithm.SingleColor && ExportVM.ExportFormat == "3MF")
                {
                    List<System.Drawing.Color>? userColors = null;
                    int? k = null;
                    bool useKMedoids = false;

                    if (PaletteVM.SelectedAlgorithm == ColorAlgorithm.KMeansAverage) k = PaletteVM.KMeansCount;
                    else if (PaletteVM.SelectedAlgorithm == ColorAlgorithm.KMeansReal) { k = PaletteVM.KMeansCount; useKMedoids = true; }
                    else if (PaletteVM.SelectedAlgorithm == ColorAlgorithm.PredefinedPalette)
                    {
                        userColors = _colorClusteringService.GetPredefinedPalette(PaletteVM.PredefinedPaletteSize);
                    }
                    else if (PaletteVM.SelectedAlgorithm == ColorAlgorithm.CustomPalette)
                    {
                        userColors = new List<System.Drawing.Color>();
                        if (PaletteVM.UserDefinedColors != null)
                        {
                            foreach(var ac in PaletteVM.UserDefinedColors) 
                                userColors.Add(System.Drawing.Color.FromArgb(255, ac.Color.R, ac.Color.G, ac.Color.B));
                        }
                        if (userColors.Count == 0) userColors.Add(System.Drawing.Color.White);
                    }
                    
                    bool isFullGeom = SelectedGeometryModeIndex == 1;
                    
                    var multiData = _fileReaderService.CreateMultiColorData(strData, k, userColors, useKMedoids, PaletteVM.SelectedAlgorithm == ColorAlgorithm.RawColors, FillHoles, UseCustomFillColor, System.Drawing.Color.FromArgb(CustomFillColor.A, CustomFillColor.R, CustomFillColor.G, CustomFillColor.B));
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => RawColorCount = multiData.voxelGrid.Count);
                    
                    return _meshService.GenerateMultiColorMeshes(multiData, strData, Bs, isFullGeom);
                }
                else
                {
                    var tris = SelectedGeometryModeIndex == 1 
                             ? _meshService.GenerateFullGeometryMesh(strData, Bs)
                             : _meshService.GenerateMesh(strData, Bs);
                             
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
        catch (OperationCanceledException)
        {
            Debug.WriteLine("DashboardViewModel: Mesh generation canceled.");
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

    [ObservableProperty] private int _rawColorCount = 0;

    [ObservableProperty] private Dictionary<System.Drawing.Color, List<Triangle>> _coloredMeshes = new();

    [ObservableProperty] private bool _showFloor;
    [ObservableProperty] private Avalonia.Media.Color _floorColor;
    [ObservableProperty] private Avalonia.Media.Color _modelColor;

    [RelayCommand]
    private void GoBack()
    {
        _navigationController.NavigateToHomeCommand.Execute(null);
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
                    StatusText = $"{MCto3D.Services.LanguageService.GetString("StatusFileLoaded")} {Path.GetFileName(_selectedFilePath)}";
                    UpdateLiveMesh();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"{MCto3D.Services.LanguageService.GetString("StatusErrorOpenFile")} {ex.Message}";
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
        StatusText = MCto3D.Services.LanguageService.GetString("StatusWaitingFile");
    }



    public void LoadDirectFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            _selectedFilePath = filePath;
            IsFileLoaded = true;
            OriginalFileName = Path.GetFileNameWithoutExtension(filePath);
            StatusText = $"{MCto3D.Services.LanguageService.GetString("StatusFileLoaded")} {Path.GetFileName(filePath)}";
            UpdateLiveMesh();
        }
    }


}
