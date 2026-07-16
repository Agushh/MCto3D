using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Services;

namespace MCto3D.ViewModels;

public class LanguageOption
{
    public string Name { get; set; }
    public string Code { get; set; }
}

public partial class LanguageSetupViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly AppSettingsService _appSettings;

    public ObservableCollection<LanguageOption> AvailableLanguages { get; }

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public LanguageSetupViewModel(MainWindowViewModel mainViewModel, AppSettingsService appSettings)
    {
        _mainViewModel = mainViewModel;
        _appSettings = appSettings;

        AvailableLanguages = new ObservableCollection<LanguageOption>
        {
            new LanguageOption { Name = "English", Code = "en-US" },
            new LanguageOption { Name = "Español", Code = "es-ES" }
        };

        _selectedLanguage = AvailableLanguages[0];
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value != null)
        {
            App.ChangeLanguage(value.Code);
        }
    }

    [RelayCommand]
    private void Continue()
    {
        // Save the chosen language
        _appSettings.Language = SelectedLanguage.Code;
        
        // Proceed to Welcome Screen
        _mainViewModel.IsLanguageSetupActive = false;
        _mainViewModel.IsWelcomeScreenActive = true;
    }
}
