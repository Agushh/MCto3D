using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace MCto3D.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;
    private DispatcherTimer _demoTimer;
    private Random _random = new Random();

    [ObservableProperty]
    private List<Triangle> _meshTriangles = new();

    public HomeViewModel(MainWindowViewModel navigationController)
    {
        _navigationController = navigationController;
        LoadRandomDemoModel();
        
        // Timer for changing models every 5 seconds
        _demoTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _demoTimer.Tick += DemoTimer_Tick;
        _demoTimer.Start();
    }

    private void DemoTimer_Tick(object? sender, EventArgs e)
    {
        LoadRandomDemoModel();
    }

    private void LoadRandomDemoModel()
    {
        string demoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DemoFiles");
        
        if (Directory.Exists(demoDir))
        {
            var files = Directory.GetFiles(demoDir, "*.nbt");
            if (files.Length > 0)
            {
                string randomFile = files[_random.Next(files.Length)];
                try
                {
                    MeshTriangles = Triangle.GenerateListTriangle(randomFile, 1.0f);
                    return;
                }
                catch { } // Fallback to cube on error
            }
        }
        
        // Fallback or alternating cube logic if no files exist
        LoadFallbackCube();
    }

    private void LoadFallbackCube()
    {
        var tris = new List<Triangle>();
        float s = 5.0f; // Cube size
        // Front face
        tris.Add(new Triangle(new Vector3(-s, -s, s), new Vector3(s, -s, s), new Vector3(s, s, s)));
        tris.Add(new Triangle(new Vector3(-s, -s, s), new Vector3(s, s, s), new Vector3(-s, s, s)));
        // Back face
        tris.Add(new Triangle(new Vector3(-s, -s, -s), new Vector3(-s, s, -s), new Vector3(s, s, -s)));
        tris.Add(new Triangle(new Vector3(-s, -s, -s), new Vector3(s, s, -s), new Vector3(s, -s, -s)));
        // Left face
        tris.Add(new Triangle(new Vector3(-s, -s, -s), new Vector3(-s, -s, s), new Vector3(-s, s, s)));
        tris.Add(new Triangle(new Vector3(-s, -s, -s), new Vector3(-s, s, s), new Vector3(-s, s, -s)));
        // Right face
        tris.Add(new Triangle(new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(s, s, s)));
        tris.Add(new Triangle(new Vector3(s, -s, -s), new Vector3(s, s, s), new Vector3(s, -s, s)));
        // Top face
        tris.Add(new Triangle(new Vector3(-s, s, -s), new Vector3(-s, s, s), new Vector3(s, s, s)));
        tris.Add(new Triangle(new Vector3(-s, s, -s), new Vector3(s, s, s), new Vector3(s, s, -s)));
        // Bottom face
        tris.Add(new Triangle(new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, -s, s)));
        tris.Add(new Triangle(new Vector3(-s, -s, -s), new Vector3(s, -s, s), new Vector3(-s, -s, s)));
        
        MeshTriangles = tris;
    }

    [RelayCommand]
    private void StartConverter()
    {
        _navigationController.NavigateToDashboardCommand.Execute(null);
    }
}
