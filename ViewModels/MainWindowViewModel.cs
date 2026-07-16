using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Services;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MCto3D.Services.ColorProcesing;
using MCto3D.Services.AssetsProcessing;

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
    public LanguageSetupViewModel LanguageSetupVM { get; }
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
    private bool _isLanguageSetupActive = false;

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

    public MainWindowViewModel(IServiceProvider provider)
    {
        HomeVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<HomeViewModel>(provider, this);
        DashboardVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<DashboardViewModel>(provider, this);
        MyFilesVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<MyFilesViewModel>(provider, this);
        VanillaStructuresVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<VanillaStructuresViewModel>(provider, this);
        DevToolsVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<DevToolsViewModel>(provider, this);
        WelcomeVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<WelcomeViewModel>(provider, this);
        LoadingVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<LoadingViewModel>(provider, this);
        LanguageSetupVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<LanguageSetupViewModel>(provider, this);
        SettingsVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<SettingsViewModel>(provider, this);
        WalkthroughVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<WalkthroughViewModel>(provider, this);
        FaqVM = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<FaqViewModel>(provider, this);
        
        _currentPage = HomeVM;

        var appSettings = provider.GetRequiredService<AppSettingsService>();

        // Comprobar estado inicial
        CheckInitialAssets(appSettings);
        var colorMapping = provider.GetRequiredService<ColorMappingService>();
        colorMapping.BlockColors = ColorGeneratorService.LoadColorsSync(appSettings.LocalFilesPath);
    }

    private void CheckInitialAssets(AppSettingsService appSettings)
    {
        if (appSettings.IsFirstRun)
        {
            appSettings.IsFirstRun = false;
            IsLanguageSetupActive = true;
            IsWelcomeScreenActive = false;
            IsMainContentVisible = false;
        }
        else
        {
            IsLanguageSetupActive = false;
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
