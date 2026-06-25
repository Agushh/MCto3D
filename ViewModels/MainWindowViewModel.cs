using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Services;
using System;
using System.IO;

namespace MCto3D.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    // Singleton instances for persistence across navigation
    public HomeViewModel HomeVM { get; }
    public DashboardViewModel DashboardVM { get; }
    public MyFilesViewModel MyFilesVM { get; }
    public VanillaStructuresViewModel VanillaStructuresVM { get; }
    public DevToolsViewModel DevToolsVM { get; }
    public WelcomeViewModel WelcomeVM { get; }
    public LoadingViewModel LoadingVM { get; }
    public SettingsViewModel SettingsVM { get; }

    [ObservableProperty]
    private bool _loadedModelsSuccessfully = false;

    [ObservableProperty]
    private bool _isMainContentVisible = false;

    [ObservableProperty]
    private bool _isHomeActive = true;

    [ObservableProperty]
    private bool _isWelcomeScreenActive = true;

    [ObservableProperty]
    private bool _isDashboardActive = false;

    [ObservableProperty]
    private bool _isMyFilesActive = false;

    [ObservableProperty]
    private bool _isVanillaStructuresActive = false;

    [ObservableProperty]
    private bool _isDevToolsActive = false;

    [ObservableProperty]
    private bool _isSettingsActive = false;

    public MainWindowViewModel()
    {
        HomeVM = new HomeViewModel(this);
        DashboardVM = new DashboardViewModel(this);
        MyFilesVM = new MyFilesViewModel(this);
        VanillaStructuresVM = new VanillaStructuresViewModel(this);
        DevToolsVM = new DevToolsViewModel(this);
        WelcomeVM = new WelcomeViewModel(this);
        LoadingVM = new LoadingViewModel(this);
        SettingsVM = new SettingsViewModel(this);
        
        _currentPage = HomeVM;

        // Comprobar estado inicial
        CheckInitialAssets();
    }

    private async void CheckInitialAssets()
    {
        // TODO: Implementa aquí tu lógica para verificar si los assets ya existen.
        // Ejemplo: bool assetsCargados = System.IO.Directory.Exists(@"ruta\a\los\archivos");
        bool assetsCargados = System.IO.Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCto3D", "MinecraftExtractedAssets", "assets")); // Cambia esto por tu comprobación real

        if (assetsCargados)
        {
            // Si ya están cargados, ocultamos la pantalla de bienvenida directamente
            IsWelcomeScreenActive = false;
            IsMainContentVisible = true;
        }
    }

    private void UpdateActiveStates()
    {
        IsHomeActive = CurrentPage == HomeVM;
        IsDashboardActive = CurrentPage == DashboardVM;
        IsMyFilesActive = CurrentPage == MyFilesVM;
        IsVanillaStructuresActive = CurrentPage == VanillaStructuresVM;
        IsDevToolsActive = CurrentPage == DevToolsVM;
        IsSettingsActive = CurrentPage == SettingsVM;
    }

    public void NavigateTo(ViewModelBase nextPage)
    {
        CurrentPage = nextPage;
        UpdateActiveStates();
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        CurrentPage = HomeVM;
        UpdateActiveStates();
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentPage = DashboardVM;
        UpdateActiveStates();
    }

    [RelayCommand]
    private void NavigateToMyFiles()
    {
        CurrentPage = MyFilesVM;
        UpdateActiveStates();
        MyFilesVM.LoadData();
    }

    [RelayCommand]
    private void NavigateToVanillaStructures()
    {
        CurrentPage = VanillaStructuresVM;
        UpdateActiveStates();
        VanillaStructuresVM.LoadData();
    }

    [RelayCommand]
    private void NavigateToDevTools()
    {
        CurrentPage = DevToolsVM;
        UpdateActiveStates();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPage = SettingsVM;
        UpdateActiveStates();
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void HideLoadingScreen()
    {
        LoadingVM.IsActive = false;
        IsMainContentVisible = true;
    }
}