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
using MCto3D.Services.ColorProcesing;
using MCto3D.Services.FileReading;

namespace MCto3D.ViewModels;



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

    private readonly AppSettingsService _appSettings;
    private readonly IProjectStorageService _projectStorage;
    private readonly StructureLoaderService _structureLoaderService;
    private readonly MeshService _meshService;
    private readonly TopologyService _topologyService;
    private readonly ColorSeparatorService _colorSeparatorService;
    public PaletteManagerViewModel PaletteVM { get; }
    public ExportViewModel ExportVM { get; }
    public ProjectSaveViewModel ProjectSaveVM { get; }


    private string? _selectedFilePath;

    [ObservableProperty] private string _statusText = LanguageService.GetString("StatusWaitingFile");

    [ObservableProperty] private bool _fillHoles = false;
    [ObservableProperty] private bool _useCustomFillColor = false;
    [ObservableProperty] private Avalonia.Media.Color _customFillColor = Colors.Green;
    [ObservableProperty] private bool _isFileLoaded = false;
    [ObservableProperty] private string _originalFileName = "";

    [ObservableProperty] private float _bs = 1.0f;
    [ObservableProperty] private string _geometryMode = "Bloques sólidos";
    [ObservableProperty] private bool _isLoadingMesh = false;

    [ObservableProperty] private int _selectedGeometryModeIndex = 0;

    [ObservableProperty] private int _rawColorCount = 0;

    [ObservableProperty] private Dictionary<System.Drawing.Color, List<Triangle>> _coloredMeshes = new();

    [ObservableProperty] private bool _showFloor;
    [ObservableProperty] private Avalonia.Media.Color _floorColor;
    [ObservableProperty] private Avalonia.Media.Color _modelColor;

    public DashboardViewModel(MainWindowViewModel navigationController, AppSettingsService appSettings, ColorSeparatorService colorSeparatorService, IProjectStorageService projectStorage, StructureLoaderService structureLoaderService, MeshService meshService, TopologyService topologyService)
    {
        _navigationController = navigationController;
        _appSettings = appSettings;
        _projectStorage = projectStorage;
        _structureLoaderService = structureLoaderService;
        _meshService = meshService;
        _topologyService = topologyService;
        _colorSeparatorService = colorSeparatorService;

        PaletteVM = new PaletteManagerViewModel(appSettings);
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
        
        try { _floorColor = Avalonia.Media.Color.Parse(_appSettings.FloorColorHex); } catch { _floorColor = Colors.DarkGray; }
        try { _modelColor = Avalonia.Media.Color.Parse(_appSettings.ModelColorHex); } catch { _modelColor = Colors.White; }
    }

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
                // 1. Cargar archivo
                StructureData strData = _structureLoaderService.Load(_selectedFilePath);
                token.ThrowIfCancellationRequested();

                // 2. Aplicar topología (si aplica)
                ApplyTopologyIfEnabled(strData);
                token.ThrowIfCancellationRequested();

                // 3. Procesar colores y generar malla
                return GenerateFinalMeshes(strData);
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
    private void ApplyTopologyIfEnabled(StructureData strData)
    {
        if (FillHoles)
        {
            _topologyService.ProcessEnclosedSpaces(strData.voxelGrid);
        }
    }
    private Dictionary<System.Drawing.Color, List<Triangle>> GenerateFinalMeshes(StructureData strData)
    {
        bool isFullGeom = SelectedGeometryModeIndex == 1;
        // Si es 3MF y no es modo de un solo color, vamos por la ruta multicolor
        if (PaletteVM.SelectedAlgorithm != ColorAlgorithm.SingleColor && ExportVM.ExportFormat == "3MF")
        {
            var (k, userColors) = GetAlgorithmParameters();
            var customFill = System.Drawing.Color.FromArgb(CustomFillColor.A, CustomFillColor.R, CustomFillColor.G, CustomFillColor.B);

            var multiData = _colorSeparatorService.SeparateByColor(strData, k, userColors, PaletteVM.SelectedAlgorithm, FillHoles, UseCustomFillColor, customFill);

            // Actualizamos UI en el hilo principal
            Avalonia.Threading.Dispatcher.UIThread.Post(() => RawColorCount = multiData.Count);

            return _meshService.GenerateMultiColorMeshes(multiData, Bs, isFullGeom);
        }

        // Ruta por defecto: un solo color / malla monolítica
        var tris = isFullGeom
                 ? _meshService.GenerateFullGeometryMesh(strData, Bs)
                 : _meshService.GenerateMesh(strData, Bs);

        return new Dictionary<System.Drawing.Color, List<Triangle>>
    {
        { System.Drawing.Color.Gray, tris }
    };
    }
    private (int k, List<System.Drawing.Color> userColors) GetAlgorithmParameters()
    {
        int k = 0;
        var userColors = new List<System.Drawing.Color>();

        if (PaletteVM.SelectedAlgorithm == ColorAlgorithm.Palette)
        {
            if (PaletteVM.UserDefinedColors != null)
            {
                foreach (var ac in PaletteVM.UserDefinedColors)
                    userColors.Add(System.Drawing.Color.FromArgb(255, ac.Color.R, ac.Color.G, ac.Color.B));
            }

            if (userColors.Count == 0)
                userColors.Add(System.Drawing.Color.White);
        }
        else if (PaletteVM.SelectedAlgorithm == ColorAlgorithm.KMeans || PaletteVM.SelectedAlgorithm == ColorAlgorithm.KMedoids)
        {
            k = PaletteVM.KMeansCount;
        }

        return (k, userColors);
    }


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
        StatusText = LanguageService.GetString("StatusWaitingFile");
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
