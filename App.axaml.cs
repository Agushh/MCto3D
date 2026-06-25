using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;

namespace MCto3D
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            ChangeLanguage(Services.AppSettings_Service.Language);
        }

        public static void ChangeLanguage(string cultureCode)
        {
            if (Current != null)
            {
                try
                {
                    var dictionary = new ResourceInclude(new System.Uri("avares://MCto3D/App.axaml"))
                    {
                        Source = new System.Uri($"avares://MCto3D/Assets/Lang/{cultureCode}.axaml")
                    };

                    if (Current.Resources.MergedDictionaries.Count > 0)
                    {
                        Current.Resources.MergedDictionaries[0] = dictionary;
                    }
                    else
                    {
                        Current.Resources.MergedDictionaries.Add(dictionary);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error changing language: {ex.Message}");
                }
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}