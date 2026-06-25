using CommunityToolkit.Mvvm.ComponentModel;

namespace MCto3D.ViewModels;

public partial class FaqViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;

    public FaqViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
    }
}
