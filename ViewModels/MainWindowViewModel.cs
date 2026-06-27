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

    public bool IsDevelopmentMode { get; } =
#if DEBUG
        true;
#else
        false;
#endif

    // Singleton instances for persistence across navigation
    public HomeViewModel HomeVM { get; }
    public DashboardViewModel DashboardVM { get; }
    public MyFilesViewModel MyFilesVM { get; }
    public VanillaStructuresViewModel VanillaStructuresVM { get; }
    public DevToolsViewModel DevToolsVM { get; }
    public WelcomeViewModel WelcomeVM { get; }
    public LoadingViewModel LoadingVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public WalkthroughViewModel WalkthroughVM { get; }
    public FaqViewModel FaqVM { get; }

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
    private bool _isWalkthroughActive = false;

    [ObservableProperty]
    private bool _isFaqActive = false;

    [ObservableProperty]
    private bool _isDevToolsActive = false;

    [ObservableProperty]
    private bool _isSettingsActive = false;

    [ObservableProperty]
    private double _windowWidth = 1300;

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
        WalkthroughVM = new WalkthroughViewModel(this);
        FaqVM = new FaqViewModel(this);
        
        _currentPage = HomeVM;

        // Comprobar estado inicial
        CheckInitialAssets();
        ColorMapping_Service.BlockColors = ColorGenerator_Service.LoadColorsSync(AppSettings_Service.LocalFilesPath);

        DashboardVM.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.Is3MfSelected))
            {
                UpdateWindowWidth();
            }
        };
    }

    private void CheckInitialAssets()
    {
        // TODO: Implementa aquí tu lógica para verificar si los assets ya existen.
        bool assetsCargados = System.IO.Directory.Exists(Path.Combine(MCto3D.Services.AppSettings_Service.LocalFilesPath, "MinecraftExtractedAssets", "assets"));

        if (assetsCargados)
        {
            // Si ya están cargados, ocultamos la pantalla de bienvenida directamente
            IsWelcomeScreenActive = false;
            IsMainContentVisible = true;
        }
    }

    private void UpdateControlsVisibility()
    {
        IsHomeActive = CurrentPage is HomeViewModel;
        IsDashboardActive = CurrentPage is DashboardViewModel;
        IsMyFilesActive = CurrentPage is MyFilesViewModel;
        IsVanillaStructuresActive = CurrentPage is VanillaStructuresViewModel;
        IsWalkthroughActive = CurrentPage is WalkthroughViewModel;
        IsFaqActive = CurrentPage is FaqViewModel;
        IsSettingsActive = CurrentPage is SettingsViewModel;

        UpdateWindowWidth();
    }

    private void UpdateWindowWidth()
    {
        if (IsDashboardActive && DashboardVM.Is3MfSelected)
        {
            WindowWidth = 1650;
        }
        else
        {
            WindowWidth = 1300;
        }
    }

    private void UpdateActiveStates()
    {
        IsHomeActive = CurrentPage == HomeVM;
        IsDashboardActive = CurrentPage == DashboardVM;
        IsMyFilesActive = CurrentPage == MyFilesVM;
        IsVanillaStructuresActive = CurrentPage == VanillaStructuresVM;
        IsWalkthroughActive = CurrentPage == WalkthroughVM;
        IsFaqActive = CurrentPage == FaqVM;
        IsDevToolsActive = CurrentPage == DevToolsVM;
        IsSettingsActive = CurrentPage == SettingsVM;
        
        UpdateWindowWidth();
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
    private void NavigateToWalkthrough()
    {
        CurrentPage = WalkthroughVM;
        UpdateActiveStates();
    }

    [RelayCommand]
    private void NavigateToFaq()
    {
        CurrentPage = FaqVM;
        UpdateActiveStates();
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