using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MCto3D.Services;

public static class ProjectStorageService
{
    private static readonly string AppDataFolder = MCto3D.Services.AppSettings_Service.LocalFilesPath;
    private static readonly string DbFilePath = Path.Combine(AppDataFolder, "projects.json");

    public static List<SavedProject> LoadProjects()
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

    public static void SaveProjects(List<SavedProject> projects)
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

    public static void AddProject(SavedProject project)
    {
        var projects = LoadProjects();
        projects.Insert(0, project); // Prepend to list
        SaveProjects(projects);
    }

    public static void UpdateProject(SavedProject project)
    {
        var projects = LoadProjects();
        var index = projects.FindIndex(p => p.Id == project.Id);
        if (index >= 0)
        {
            projects[index] = project;
            SaveProjects(projects);
        }
    }

    public static void DeleteProject(Guid id)
    {
        var projects = LoadProjects();
        projects.RemoveAll(p => p.Id == id);
        SaveProjects(projects);
    }
}
