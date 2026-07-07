using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
using MCto3D.Services.ColorProcesing;

namespace MCto3D.ViewModels;

public partial class PaletteManagerViewModel : ViewModelBase
{
    private readonly AppSettingsService _appSettings;

    public event EventHandler? PaletteChanged;

    public PaletteManagerViewModel(AppSettingsService appSettings)
    {
        _appSettings = appSettings;
        LoadPalettes();
    }

    [ObservableProperty] private ColorAlgorithm _selectedAlgorithm = ColorAlgorithm.SingleColor;
    
    public bool IsSingleColorMode => SelectedAlgorithm == ColorAlgorithm.SingleColor;
    public bool IsMultiColorMode => SelectedAlgorithm != ColorAlgorithm.SingleColor;

    partial void OnSelectedAlgorithmChanged(ColorAlgorithm value) 
    { 
        OnPropertyChanged(nameof(IsSingleColorMode));
        OnPropertyChanged(nameof(IsMultiColorMode));
        PaletteChanged?.Invoke(this, EventArgs.Empty); 
    }

    [ObservableProperty] private int _kMeansCount = 4;
    partial void OnKMeansCountChanged(int value) 
    { 
        if (SelectedAlgorithm == ColorAlgorithm.KMeans || SelectedAlgorithm == ColorAlgorithm.KMedoids) 
            PaletteChanged?.Invoke(this, EventArgs.Empty); 
    }

    [ObservableProperty] private ObservableCollection<CustomColorItem> _userDefinedColors = new();
    
    [ObservableProperty] private ObservableCollection<CustomPalette> _availablePalettes = new();
    
    [ObservableProperty] private CustomPalette _selectedPalette;

    [ObservableProperty] private string _newPaletteName = string.Empty;
    
    [ObservableProperty] private string _selectedPaletteNameInput = string.Empty;
    partial void OnSelectedPaletteNameInputChanged(string value)
    {
        HasUnsavedPaletteChanges = true;
    }

    [ObservableProperty] private bool _hasUnsavedPaletteChanges = false;

    private string CustomPaletteName => MCto3D.Services.LanguageService.GetString("ConverterPaletteCustom");

    partial void OnSelectedPaletteChanged(CustomPalette value)
    {
        if (value == null) return;
        
        NewPaletteName = string.Empty;
        SelectedPaletteNameInput = value.Name;
        HasUnsavedPaletteChanges = false;

        // Load the colors into UserDefinedColors
        UserDefinedColors.Clear();
        if (value.Name == CustomPaletteName || value.ColorsHex == null || value.ColorsHex.Count == 0)
        {
            // Default blank palette
            var item = new CustomColorItem { Color = Avalonia.Media.Color.Parse("#FFFFFF") };
            item.OnColorChangedCallback = () => { HasUnsavedPaletteChanges = true; if (SelectedAlgorithm == ColorAlgorithm.Palette) PaletteChanged?.Invoke(this, EventArgs.Empty); };
            UserDefinedColors.Add(item);
        }
        else
        {
            foreach (var hex in value.ColorsHex)
            {
                try
                {
                    var item = new CustomColorItem { Color = Avalonia.Media.Color.Parse(hex) };
                    item.OnColorChangedCallback = () => { HasUnsavedPaletteChanges = true; if (SelectedAlgorithm == ColorAlgorithm.Palette) PaletteChanged?.Invoke(this, EventArgs.Empty); };
                    UserDefinedColors.Add(item);
                }
                catch { }
            }
        }
        if (SelectedAlgorithm == ColorAlgorithm.Palette) PaletteChanged?.Invoke(this, EventArgs.Empty);
        
        OnPropertyChanged(nameof(IsSavedPaletteSelected));
        OnPropertyChanged(nameof(IsCustomPaletteSelected));
    }

    public bool IsSavedPaletteSelected => SelectedPalette != null && SelectedPalette.Name != CustomPaletteName;
    public bool IsCustomPaletteSelected => SelectedPalette != null && SelectedPalette.Name == CustomPaletteName;

    [RelayCommand]
    private void SaveCustomPalette(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            int count = _appSettings.SavedPalettes.Count + 1;
            name = $"Personalizada-{count}";
        }
        
        var newPalette = new CustomPalette { Name = name };
        foreach (var c in UserDefinedColors)
        {
            newPalette.ColorsHex.Add(c.Color.ToString());
        }
        
        _appSettings.SavedPalettes.Add(newPalette);
        _appSettings.SavePalettes();
        
        LoadPalettes();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => 
        {
            SelectedPalette = AvailablePalettes.FirstOrDefault(p => p.Name == name);
        });
    }

    [RelayCommand]
    private void UpdateSavedPalette()
    {
        if (SelectedPalette == null || SelectedPalette.Name == CustomPaletteName) return;
        
        var existing = _appSettings.SavedPalettes.FirstOrDefault(p => p.Name == SelectedPalette.Name);
        if (existing != null)
        {
            existing.Name = string.IsNullOrWhiteSpace(SelectedPaletteNameInput) ? existing.Name : SelectedPaletteNameInput;
            SelectedPalette.Name = existing.Name;

            existing.ColorsHex.Clear();
            foreach (var c in UserDefinedColors)
            {
                existing.ColorsHex.Add(c.Color.ToString());
            }
            _appSettings.SavePalettes();
            HasUnsavedPaletteChanges = false;
        }
        LoadPalettes();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => 
        {
            SelectedPalette = AvailablePalettes.FirstOrDefault(p => p.Name == existing?.Name);
        });
    }

    [RelayCommand]
    private void DeleteSavedPalette()
    {
        if (SelectedPalette == null || SelectedPalette.Name == CustomPaletteName) return;
        
        var existing = _appSettings.SavedPalettes.FirstOrDefault(p => p.Name == SelectedPalette.Name);
        if (existing != null)
        {
            _appSettings.SavedPalettes.Remove(existing);
            _appSettings.SavePalettes();
        }
        LoadPalettes();
    }

    public void LoadPalettes()
    {
        var currentSelectionName = SelectedPalette?.Name;
        AvailablePalettes.Clear();
        foreach (var p in _appSettings.SavedPalettes)
        {
            AvailablePalettes.Add(p);
        }
        AvailablePalettes.Add(new CustomPalette { Name = MCto3D.Services.LanguageService.GetString("ConverterPaletteCustom") });
        
        Avalonia.Threading.Dispatcher.UIThread.Post(() => 
        {
            if (currentSelectionName != null)
            {
                var match = AvailablePalettes.FirstOrDefault(p => p.Name == currentSelectionName);
                if (match != null)
                {
                    SelectedPalette = match;
                    return;
                }
            }
            SelectedPalette = AvailablePalettes[^1]; // default to Custom
        });
    }

    [RelayCommand]
    private void AddUserColor()
    {
        var item = new CustomColorItem { Color = Avalonia.Media.Color.Parse("#FFFFFF") };
        item.OnColorChangedCallback = () => { HasUnsavedPaletteChanges = true; if (SelectedAlgorithm == ColorAlgorithm.Palette) PaletteChanged?.Invoke(this, EventArgs.Empty); };
        UserDefinedColors.Add(item);
        HasUnsavedPaletteChanges = true;
        if (SelectedAlgorithm == ColorAlgorithm.Palette) PaletteChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveUserColor(CustomColorItem item)
    {
        if (UserDefinedColors.Count <= 1) return;
        if (UserDefinedColors.Contains(item))
        {
            UserDefinedColors.Remove(item);
            HasUnsavedPaletteChanges = true;
            if (SelectedAlgorithm == ColorAlgorithm.Palette) PaletteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class CustomColorItem : ObservableObject
    {
        [ObservableProperty] private Avalonia.Media.Color _color;
        
        public Action? OnColorChangedCallback { get; set; }
        
        partial void OnColorChanged(Avalonia.Media.Color value)
        {
            OnColorChangedCallback?.Invoke();
        }
    }
}
