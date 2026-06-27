using Avalonia.Controls;

namespace MCto3D.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is ViewModels.DashboardViewModel vm)
            {
                vm.ProjectSaveVM.CaptureThumbnailsFunc = () => MainRenderControl.GenerateThumbnails(512, 512);
            }
        };
    }
}