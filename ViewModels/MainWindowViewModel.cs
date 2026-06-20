using CommunityToolkit.Mvvm.ComponentModel;

namespace MCto3D.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Esta propiedad define qué página está activa en la pantalla
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
        // Al iniciar la app, mostramos el panel de configuración (Dashboard)
        _currentPage = new DashboardViewModel(this);
    }

    // Método público para cambiar de pantalla desde cualquier sub-viewmodel
    public void NavigateTo(ViewModelBase nextPage)
    {
        CurrentPage = nextPage;
    }
}