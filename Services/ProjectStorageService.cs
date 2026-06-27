using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MCto3D.Services;

public interface IProjectStorageService
{
    List<SavedProject> LoadProjects();
    void SaveProjects(List<SavedProject> projects);
    void AddProject(SavedProject project);
    void UpdateProject(SavedProject project);
    void DeleteProject(Guid id);
}

public class ProjectStorageService : IProjectStorageService
{
    private readonly IAppSettingsService _appSettings;

    public ProjectStorageService(IAppSettingsService appSettings)
    {
        _appSettings = appSettings;
    }

    private string AppDataFolder => _appSettings.LocalFilesPath;
    private string DbFilePath => Path.Combine(AppDataFolder, "projects.json");

    public List<SavedProject> LoadProjects()
    {
        if (!Directory.Exists(AppDataFolder))
        {
            Directory.CreateDirectory(AppDataFolder);
        }

        if (!File.Exists(DbFilePath))
        {
            return new List<SavedProject>();
        }

        try
        {
            string json = File.ReadAllText(DbFilePath);
            return JsonSerializer.Deserialize<List<SavedProject>>(json) ?? new List<SavedProject>();
        }
        catch
        {
            return new List<SavedProject>();
        }
    }

    public void SaveProjects(List<SavedProject> projects)
    {
        if (!Directory.Exists(AppDataFolder))
        {
            Directory.CreateDirectory(AppDataFolder);
        }

        try
        {
            string json = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DbFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar DB: {ex.Message}");
        }
    }

    public void AddProject(SavedProject project)
    {
        var projects = LoadProjects();
        projects.Insert(0, project);
        SaveProjects(projects);
    }

    public void UpdateProject(SavedProject project)
    {
        var projects = LoadProjects();
        var index = projects.FindIndex(p => p.Id == project.Id);
        if (index >= 0)
        {
            projects[index] = project;
            SaveProjects(projects);
        }
    }

    public void DeleteProject(Guid id)
    {
        var projects = LoadProjects();
        projects.RemoveAll(p => p.Id == id);
        SaveProjects(projects);
    }
}

