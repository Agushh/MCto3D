using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCto3D.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;

    private string? _selectedFilePath;
    [ObservableProperty] private string _statusText = "Esperando archivo NBT...";
    [ObservableProperty] private bool _isFileLoaded = false;
    [ObservableProperty] private float _bs = 1.0f;
    [ObservableProperty] private string _selectedColor = "#808080";
    [ObservableProperty] private string _renderSelection = "Todos los bloques sólidos";

    // Formato de exportación: "STL" o "3MF"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is3MfSelected))]
    [NotifyPropertyChangedFor(nameof(IsColorSelectorVisible))]
    private string _exportFormat = "STL";

    // Modos de color para 3MF
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsColorSelectorVisible))] private bool _isSingleColorMode = true;
    [ObservableProperty] private bool _isMultiColorMode = false;

    // Propiedades calculadas para la interfaz (Getters condicionales)
    public bool Is3MfSelected => ExportFormat == "3MF";
    public bool IsColorSelectorVisible => ExportFormat == "STL" || (ExportFormat == "3MF" && IsSingleColorMode);

    public DashboardViewModel(MainWindowViewModel navigationController)
    {
        _navigationController = navigationController;
    }

    [RelayCommand]
    private async Task LoadFile(Control visualTarget)
    {
        if (visualTarget == null) return;

        // Obtenemos el TopLevel (la ventana o contenedor principal que renderiza el control)
        var topLevel = TopLevel.GetTopLevel(visualTarget);
        if (topLevel == null) return;

        try
        {
            // Configuramos las opciones del explorador de archivos
            var options = new FilePickerOpenOptions
            {
                Title = "Seleccionar archivo NBT de Minecraft",
                AllowMultiple = false, // Solo un archivo a la vez
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Estructura Minecraft (*.nbt)")
                    {
                        Patterns = new[] { "*.nbt" } // Filtro estricto de extensión
                    }
                }
            };

            // Abre el diálogo nativo del sistema operativo (Windows/Mac/Linux)
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            // Si el usuario seleccionó un archivo y no canceló el diálogo
            if (files != null && files.Count > 0)
            {
                // Convertimos la URI de almacenamiento seguro en una ruta física local
                _selectedFilePath = files[0].Path.LocalPath;

                if (!string.IsNullOrEmpty(_selectedFilePath))
                {
                    IsFileLoaded = true;
                    StatusText = $"Archivo cargado: {Path.GetFileName(_selectedFilePath)}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error al abrir el archivo: {ex.Message}";
            IsFileLoaded = false;
        }
    }

    [RelayCommand]
    private void GenerateFile()
    {
        StatusText = "Procesando geometría...";


        List<Triangle> mallaSimulada = Triangle.GenerateListTriangle(_selectedFilePath! , Bs);
        
        // 2. NAVEGACIÓN: Creamos la pantalla de resultados y le pasamos los triángulos calculados
        var resultadoVM = new ExportResultViewModel(_navigationController, mallaSimulada, ExportFormat);
        _navigationController.NavigateTo(resultadoVM);
    }
}