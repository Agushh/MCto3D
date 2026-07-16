using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;

namespace MCto3D.ViewModels;

public partial class ProjectSaveViewModel : ViewModelBase
{
    private readonly AppSettingsService _appSettings;
    private readonly IProjectStorageService _projectStorage;
    private readonly MainWindowViewModel _navigationController;

    public Func<Task<System.Collections.Generic.List<WriteableBitmap>>>? CaptureThumbnailsFunc { get; set; }
    public Func<string>? GetOriginalFileNameFunc { get; set; }
    public Func<string?>? GetSelectedFilePathFunc { get; set; }
    public Func<float>? GetBlockScaleFunc { get; set; }
    public Func<int>? GetSelectedGeometryModeIndexFunc { get; set; }
    public Func<string>? GetExportFormatFunc { get; set; }
    public Func<bool>? GetIsSingleColorModeFunc { get; set; }
    public Func<System.Collections.Generic.Dictionary<System.Drawing.Color, System.Collections.Generic.List<Triangle>>>? GetColoredMeshesFunc { get; set; }
    
    public event EventHandler<string>? StatusTextChanged;

    public ProjectSaveViewModel(MainWindowViewModel navigationController, AppSettingsService appSettings, IProjectStorageService projectStorage)
    {
        _navigationController = navigationController;
        _appSettings = appSettings;
        _projectStorage = projectStorage;
    }

    [ObservableProperty] private bool _isSavePopupOpen = false;
    [ObservableProperty] private string _newProjectName = string.Empty;
    [ObservableProperty] private int _selectedThumbnailIndex = 0;
    [ObservableProperty] private ObservableCollection<WriteableBitmap?> _thumbnails = new(new WriteableBitmap?[4]);

    [RelayCommand]
    private async Task OpenSavePopup()
    {
        NewProjectName = GetOriginalFileNameFunc?.Invoke() ?? string.Empty;
        SelectedThumbnailIndex = 0; // Por defecto la 1
        
        if (CaptureThumbnailsFunc != null)
        {
            var images = await CaptureThumbnailsFunc();
            Thumbnails = new ObservableCollection<WriteableBitmap?>(images!);
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
        string? selectedFilePath = GetSelectedFilePathFunc?.Invoke();
        if (string.IsNullOrWhiteSpace(NewProjectName) || string.IsNullOrWhiteSpace(selectedFilePath)) return;

        string thumbnailPath = string.Empty;

        // Guardar miniatura si existe
        if (Thumbnails != null && Thumbnails.Count > SelectedThumbnailIndex)
        {
            try
            {
                var bmp = Thumbnails[SelectedThumbnailIndex];
                var appDataFolder = Path.Combine(_appSettings.LocalFilesPath, "Thumbnails");
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

        string exportedPath = string.Empty;
        var format = GetExportFormatFunc?.Invoke() ?? "STL";
        var meshes = GetColoredMeshesFunc?.Invoke();

        if (meshes != null)
        {
            try
            {
                var exportsFolder = Path.Combine(_appSettings.LocalFilesPath, "Exports");
                if (!Directory.Exists(exportsFolder)) Directory.CreateDirectory(exportsFolder);

                string safeName = string.Join("_", NewProjectName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"{safeName}_{Guid.NewGuid():N}.{format.ToLower()}";
                exportedPath = Path.Combine(exportsFolder, fileName);

                MCto3D.Services.ExportedFilesWriting.IModelWriter writer = format == "STL" 
                    ? new MCto3D.Services.ExportedFilesWriting.StlWriterService() 
                    : new MCto3D.Services.ExportedFilesWriting.ThreeMfWriterService(_appSettings.Use3MfAssemblyMode);
                
                writer.Write(exportedPath, meshes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exportando modelo: {ex.Message}");
                StatusTextChanged?.Invoke(this, $"Error guardando archivo: {ex.Message}");
                return;
            }
        }

        var newProject = new SavedProject
        {
            Name = NewProjectName,
            OriginalFilePath = selectedFilePath,
            ThumbnailPath = thumbnailPath,
            ExportedFilePath = exportedPath,
            BlockScale = GetBlockScaleFunc?.Invoke() ?? 1.0f,
            GeometryMode = (GetSelectedGeometryModeIndexFunc?.Invoke() ?? 0) == 1 ? "Geometrías completas" : "Bloques sólidos",
            ExportFormat = format,
            IsSingleColorMode = GetIsSingleColorModeFunc?.Invoke() ?? true
        };

        _projectStorage.AddProject(newProject);
        
        IsSavePopupOpen = false;
        StatusTextChanged?.Invoke(this, "¡Guardado en Mis Archivos exitosamente!");
        
        // Update MyFilesVM so the new project is listed immediately
        _navigationController.MyFilesVM?.LoadData();
    }
}
