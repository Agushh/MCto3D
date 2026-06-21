using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    [ObservableProperty]
    private bool _isHomeActive = true;

    [ObservableProperty]
    private bool _isDashboardActive = false;

    [ObservableProperty]
    private bool _isMyFilesActive = false;

    public MainWindowViewModel()
    {
        HomeVM = new HomeViewModel(this);
        DashboardVM = new DashboardViewModel(this);
        MyFilesVM = new MyFilesViewModel(this);
        
        _currentPage = HomeVM;
    }

    private void UpdateActiveStates()
    {
        IsHomeActive = CurrentPage == HomeVM;
        IsDashboardActive = CurrentPage == DashboardVM;
        IsMyFilesActive = CurrentPage == MyFilesVM;
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
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }
}