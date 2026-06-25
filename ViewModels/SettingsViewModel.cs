using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MCto3D.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;

    public SettingsViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void ReloadBaseFiles()
    {
        // Vuelve a mostrar la pantalla de bienvenida para cargar archivos
        _mainViewModel.IsWelcomeScreenActive = true;
    }

    [RelayCommand]
    private async Task ReloadModelsAsync()
    {
        // Limpiamos el caché del resolutor nativo
        MCto3D.Services.NativeModelResolver_Service.ClearCache();
        await Task.Delay(100); // Pequeña espera para UX
    }
}
