using CommunityToolkit.Mvvm.ComponentModel;

namespace MCto3D.ViewModels;

public partial class WalkthroughViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;

    public WalkthroughViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
    }
}
