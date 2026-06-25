using CommunityToolkit.Mvvm.Input;
using MCto3D.Services;

namespace MCto3D.ViewModels;

public partial class DevToolsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;

    public DevToolsViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }
}
