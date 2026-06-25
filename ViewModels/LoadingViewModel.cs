using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MCto3D.ViewModels;

public partial class LoadingViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;

    [ObservableProperty]
    private bool _isActive = false;

    [ObservableProperty]
    private string _statusMessage = "Cargando...";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    public LoadingViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public async Task RunTaskAsync(Func<IProgress<string>, Task<(bool success, string errorMsg)>> taskToRun)
    {
        IsActive = true;
        _mainViewModel.IsMainContentVisible = false;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Generando modelos...";

        var progress = new Progress<string>(message => 
        {
            StatusMessage = message;
        });

        var (success, errorMsg) = await taskToRun(progress);

        if (success)
        {
            StatusMessage = "¡Completado con éxito!";
            _mainViewModel.LoadedModelsSuccessfully = true;
            await Task.Delay(1500);
            IsActive = false;
            _mainViewModel.IsMainContentVisible = true;
        }
        else
        {
            _mainViewModel.LoadedModelsSuccessfully = false;
            StatusMessage = "Error crítico durante el procesamiento";
            ErrorMessage = errorMsg;
            HasError = true;
            // La pantalla se quedará activa mostrando el error hasta que se cierre (puedes agregar un comando para cerrar)
        }
    }
}
