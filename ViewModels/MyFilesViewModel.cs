using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
using MCto3D.Services.ExportedFilesWriting;
using MCto3D.Services.FileReading;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MCto3D.ViewModels;

public partial class MyFilesViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;

    [ObservableProperty]
    private ObservableCollection<SavedProject> _projects = new();

    private List<SavedProject> _allProjects = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedSortIndex = 0; // 0 = Más Recientes, 1 = Nombre

    [ObservableProperty]
    private bool _isGridView = true;

    [ObservableProperty]
    private bool _isListView = false;

    // Pop-Up State
    [ObservableProperty]
    private bool _isPopupOpen = false;

    [ObservableProperty]
    private SavedProject? _selectedProject;

    [ObservableProperty]
    private bool _isEditingName = false;

    [ObservableProperty]
    private string _editingNameText = string.Empty;

    public List<SlicerOption> SlicerOptions { get; } = new()
    {
        new SlicerOption { Name = "OrcaSlicer", ColorHex = "#027D52", UriScheme = "orcaslicer://" },
        new SlicerOption { Name = "BambuStudio", ColorHex = "#00AE42", UriScheme = "bambustudio://" },
        new SlicerOption { Name = "PrusaSlicer", ColorHex = "#EA6B24", UriScheme = "prusaslicer://" },
        new SlicerOption { Name = "Cura", ColorHex = "#0055FF", UriScheme = "cura://" }
    };

    [ObservableProperty]
    private SlicerOption _selectedSlicer;

    private readonly IProjectStorageService _projectStorage;
    private readonly StructureLoaderService _structureLoaderService;
    private readonly MeshService _meshService;

    public MyFilesViewModel(MainWindowViewModel navigationController, IProjectStorageService projectStorage, StructureLoaderService structureLoaderService, MeshService meshService)
    {
        _navigationController = navigationController;
        _projectStorage = projectStorage;
        _structureLoaderService = structureLoaderService;
        _meshService = meshService;
        _selectedSlicer = SlicerOptions[0];
        LoadData();
    }

    public void LoadData()
    {
        _allProjects = _projectStorage.LoadProjects();
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedSortIndexChanged(int value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allProjects.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedSortIndex == 1)
        {
            // Sort by Name
            filtered = filtered.OrderBy(p => p.Name);
        }
        else
        {
            // Sort by newest first
            filtered = filtered.OrderByDescending(p => p.CreationDate);
        }

        Projects = new ObservableCollection<SavedProject>(filtered);
    }

    [RelayCommand]
    private void SetGridView()
    {
        IsGridView = true;
        IsListView = false;
    }

    [RelayCommand]
    private void SetListView()
    {
        IsGridView = false;
        IsListView = true;
    }

    [RelayCommand]
    private void OpenProject(SavedProject project)
    {
        if (project != null)
        {
            SelectedProject = project;
            IsPopupOpen = true;
            IsEditingName = false;
        }
    }

    [RelayCommand]
    private void ClosePopup()
    {
        IsPopupOpen = false;
        SelectedProject = null;
        IsEditingName = false;
    }

    [RelayCommand]
    private void StartEditName()
    {
        if (SelectedProject != null)
        {
            EditingNameText = SelectedProject.Name;
            IsEditingName = true;
        }
    }

    [RelayCommand]
    private void SaveEditName()
    {
        if (SelectedProject != null && !string.IsNullOrWhiteSpace(EditingNameText))
        {
            SelectedProject.Name = EditingNameText;
            _projectStorage.UpdateProject(SelectedProject);
            IsEditingName = false;
            
            // Trigger refresh
            LoadData();
            
            // Re-select to update popup binding
            SelectedProject = _allProjects.FirstOrDefault(p => p.Id == SelectedProject.Id);
        }
    }

    [RelayCommand]
    private void CancelEditName()
    {
        IsEditingName = false;
    }

    [RelayCommand]
    private void DeleteProject()
    {
        if (SelectedProject != null)
        {
            // Simple direct delete. Could add a confirmation state if needed.
            _projectStorage.DeleteProject(SelectedProject.Id);
            ClosePopup();
            LoadData();
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationController.NavigateToDashboardCommand.Execute(null);
    }

    [RelayCommand]
    private void ChangeSlicer(SlicerOption newSlicer)
    {
        if (newSlicer != null)
        {
            SelectedSlicer = newSlicer;
        }
    }

    // Commands to reuse Export/Slicer logic for the saved project
    [RelayCommand]
    private async Task ExportLocal(Control visualTarget)
    {
        if (visualTarget == null || SelectedProject == null || string.IsNullOrEmpty(SelectedProject.OriginalFilePath)) return;

        var topLevel = TopLevel.GetTopLevel(visualTarget);
        if (topLevel == null) return;

        try
        {
            StructureData strData = _structureLoaderService.Load(SelectedProject.OriginalFilePath);

            List<Triangle> malla = _meshService.GenerateMesh(strData, SelectedProject.BlockScale);
            string extensionPorDefecto = SelectedProject.ExportFormat.ToLower();
            string nombreFiltro = SelectedProject.ExportFormat == "STL" ? "Archivo Estereolitografía (*.stl)" : "3D Manufacturing Format (*.3mf)";
            string patronExtension = $"*.{extensionPorDefecto}";
            
            string suggestedName = string.IsNullOrWhiteSpace(SelectedProject.Name) ? "modelo_minecraft" : SelectedProject.Name;

            var saveOptions = new FilePickerSaveOptions
            {
                Title = $"Exportar modelo como {SelectedProject.ExportFormat}",
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

                if (SelectedProject.ExportFormat == "STL")
                {
                    StlGenerator.CreateBinaryStlWithColor(rutaDestino, malla, Color.Gray);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al exportar: {ex.Message}");
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
        if (SelectedProject == null || string.IsNullOrEmpty(SelectedProject.OriginalFilePath)) return;

        try
        {
            StructureData strData = _structureLoaderService.Load(SelectedProject.OriginalFilePath);


            List<Triangle> malla = _meshService.GenerateMesh(strData, SelectedProject.BlockScale);

            string extension = SelectedProject.ExportFormat.ToLower();
            string suggestedName = string.IsNullOrWhiteSpace(SelectedProject.Name) ? "modelo" : SelectedProject.Name;
            string tempFile = Path.Combine(Path.GetTempPath(), $"{suggestedName}_{Guid.NewGuid():N}.{extension}");

            if (SelectedProject.ExportFormat == "STL")
            {
                StlGenerator.CreateBinaryStlWithColor(tempFile, malla, Color.Gray);
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al abrir el slicer: {ex.Message}");
        }
    }
}

