using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
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

    public List<Bitmap> CarouselImages { get; } = new();

    [ObservableProperty]
    private int _currentCarouselIndex = 0;

    [ObservableProperty]
    private Bitmap? _prevImage;

    [ObservableProperty]
    private Bitmap? _currentImage;

    [ObservableProperty]
    private Bitmap? _nextImage;

    public bool IsDot0Active => CurrentCarouselIndex == 0;
    public bool IsDot1Active => CurrentCarouselIndex == 1;
    public bool IsDot2Active => CurrentCarouselIndex == 2;
    private readonly IProjectStorageService _projectStorage;
    private readonly IModelReader _modelReader;

    public HomeViewModel(MainWindowViewModel navigationController, IProjectStorageService projectStorage, IModelReader modelReader)
    {
        _navigationController = navigationController;
        _projectStorage = projectStorage;
        _modelReader = modelReader;
        LoadRandomDemoModel();
        InitializeCarousel();
        
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

    [ObservableProperty]
    private Dictionary<System.Drawing.Color, List<Triangle>> _coloredMeshes = new();

    private void LoadRandomDemoModel()
    {
        string demoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DemoFiles");
        
        if (Directory.Exists(demoDir))
        {
            var files = Directory.GetFiles(demoDir, "*.3mf");
            if (files.Length > 0)
            {
                string randomFile = files[_random.Next(files.Length)];
                try
                {
                    if (randomFile != null)
                    {
                        ColoredMeshes = _modelReader.Read(randomFile);
                    }
                    MeshTriangles = new List<Triangle>(); // Clear raw triangles if using colored
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

    private void InitializeCarousel()
    {
        try
        {
            CarouselImages.Add(new Bitmap(AssetLoader.Open(new Uri("avares://MCto3D/Assets/Images/Carousel/mc_house.png"))));
            CarouselImages.Add(new Bitmap(AssetLoader.Open(new Uri("avares://MCto3D/Assets/Images/Carousel/mc_castle.png"))));
            CarouselImages.Add(new Bitmap(AssetLoader.Open(new Uri("avares://MCto3D/Assets/Images/Carousel/mc_village.png"))));
            
            UpdateCarouselImages();
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void UpdateCarouselImages()
    {
        if (CarouselImages.Count == 0) return;

        int count = CarouselImages.Count;
        int prevIdx = (CurrentCarouselIndex - 1 + count) % count;
        int nextIdx = (CurrentCarouselIndex + 1) % count;

        PrevImage = CarouselImages[prevIdx];
        CurrentImage = CarouselImages[CurrentCarouselIndex];
        NextImage = CarouselImages[nextIdx];

        OnPropertyChanged(nameof(IsDot0Active));
        OnPropertyChanged(nameof(IsDot1Active));
        OnPropertyChanged(nameof(IsDot2Active));
    }

    [RelayCommand]
    private void NextCarouselImage()
    {
        if (CarouselImages.Count == 0) return;
        CurrentCarouselIndex = (CurrentCarouselIndex + 1) % CarouselImages.Count;
        UpdateCarouselImages();
    }

    [RelayCommand]
    private void PrevCarouselImage()
    {
        if (CarouselImages.Count == 0) return;
        CurrentCarouselIndex = (CurrentCarouselIndex - 1 + CarouselImages.Count) % CarouselImages.Count;
        UpdateCarouselImages();
    }
}

