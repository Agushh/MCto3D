using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MCto3D.ViewModels;

public partial class VanillaStructuresViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;

    [ObservableProperty]
    private ObservableCollection<SavedProject> _projects = new();

    private List<SavedProject> _allProjects = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    private string _currentVanillaPath = "";

    [ObservableProperty]
    private bool _canGoUp = false;

    [ObservableProperty]
    private string _currentFolderName = "Structures";

    public VanillaStructuresViewModel(MainWindowViewModel navigationController)
    {
        _navigationController = navigationController;
        LoadData();
    }

    public void LoadData()
    {
        _allProjects.Clear();
        string baseDir = Path.Combine(MCto3D.Services.AppSettings_Service.LocalFilesPath, "MinecraftExtractedAssets", "data", "minecraft", "structure");
        
        string targetDir = string.IsNullOrEmpty(_currentVanillaPath) ? baseDir : _currentVanillaPath;
        CanGoUp = targetDir != baseDir;
        CurrentFolderName = CanGoUp ? new DirectoryInfo(targetDir).Name : "Structures";

        if (Directory.Exists(targetDir))
        {
            foreach (var dir in Directory.GetDirectories(targetDir))
            {
                _allProjects.Add(new SavedProject
                {
                    Id = Guid.Empty,
                    Name = Path.GetFileName(dir),
                    IsFolder = true,
                    IsReadOnly = true,
                    OriginalFilePath = dir
                });
            }

            foreach (var file in Directory.GetFiles(targetDir, "*.nbt"))
            {
                _allProjects.Add(new SavedProject
                {
                    Id = Guid.Empty,
                    Name = Path.GetFileNameWithoutExtension(file),
                    IsFolder = false,
                    IsReadOnly = true,
                    OriginalFilePath = file,
                    CreationDate = File.GetCreationTime(file)
                });
            }
        }
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
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

        // Folders first, then alphabetically
        filtered = filtered.OrderByDescending(p => p.IsFolder).ThenBy(p => p.Name);

        Projects = new ObservableCollection<SavedProject>(filtered);
    }

    [RelayCommand]
    private void OpenProject(SavedProject project)
    {
        if (project != null)
        {
            if (project.IsFolder)
            {
                _currentVanillaPath = project.OriginalFilePath;
                LoadData();
                return;
            }

            // Es un archivo, lo mandamos al dashboard
            if (!string.IsNullOrEmpty(project.OriginalFilePath))
            {
                _navigationController.NavigateToDashboardCommand.Execute(null);
                _navigationController.DashboardVM.LoadDirectFile(project.OriginalFilePath);
            }
        }
    }

    [RelayCommand]
    private void GoUpFolder()
    {
        string baseDir = Path.Combine(MCto3D.Services.AppSettings_Service.LocalFilesPath, "MinecraftExtractedAssets", "data", "minecraft", "structure");
        
        if (!string.IsNullOrEmpty(_currentVanillaPath) && _currentVanillaPath != baseDir)
        {
            DirectoryInfo currentInfo = new DirectoryInfo(_currentVanillaPath);
            if (currentInfo.Parent != null && currentInfo.FullName.TrimEnd('\\', '/') != baseDir.TrimEnd('\\', '/'))
            {
                _currentVanillaPath = currentInfo.Parent.FullName;
            }
            else
            {
                _currentVanillaPath = baseDir;
            }
        }
        LoadData();
    }
}
