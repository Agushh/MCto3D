using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCto3D.Models;
using MCto3D.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace MCto3D.ViewModels;

public partial class ExportResultViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _navigationController;
    private readonly List<Triangle> _mallaProcesada;
    private readonly string _formatoElegido;

    public ExportResultViewModel(MainWindowViewModel navigationController, List<Triangle> malla, string formato)
    {
        _navigationController = navigationController;
        _mallaProcesada = malla;
        _formatoElegido = formato;
    }

    [RelayCommand]
    private async Task ExportFile(Control visualTarget)
    {
        if (visualTarget == null) return;

        // Obtenemos el TopLevel de la ventana actual
        var topLevel = TopLevel.GetTopLevel(visualTarget);
        if (topLevel == null) return;

        try
        {
            // Definimos la extensión por defecto basada en el formato elegido en el Dashboard
            string extensionPorDefecto = _formatoElegido.ToLower(); // "stl" o "3mf"
            string nombreFiltro = _formatoElegido == "STL" ? "Archivo Estereolitografía (*.stl)" : "3D Manufacturing Format (*.3mf)";
            string patronExtension = $"*.{extensionPorDefecto}";

            // Configuramos las opciones del cuadro de diálogo "Guardar como..."
            var saveOptions = new FilePickerSaveOptions
            {
                Title = $"Exportar modelo como {_formatoElegido}",
                DefaultExtension = extensionPorDefecto,
                SuggestedFileName = "modelo_minecraft", // Nombre sugerido inicial
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(nombreFiltro)
                    {
                        Patterns = new[] { patronExtension }
                    }
                }
            };

            // Abrimos el explorador nativo para guardar el archivo
            var fileLocation = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);

            // Si el usuario no canceló la operación y seleccionó un destino válido
            if (fileLocation != null)
            {
                // Extraemos la ruta física absoluta de tu disco duro
                string rutaDestino = fileLocation.Path.LocalPath;

                // Evaluamos qué generador ejecutar según el formato de la malla
                if (_formatoElegido == "STL")
                {
                    // Ejecutamos tu clase generadora de STL que programamos al inicio
                    //StlGenerator.CreateAsciiStl(rutaDestino, _mallaProcesada);
                    StlGenerator.CreateBinaryStlWithColor(rutaDestino, _mallaProcesada, Color.Red);
                }
                else
                {
                    // Aquí irá tu método para empaquetar el archivo ZIP/XML del 3MF
                    // ThreeMfGenerator.CreateColor3mf(rutaDestino, _mallaProcesada);
                } 

                // Opcional: podrías lanzar un aviso de éxito al usuario aquí
            }
        }
        catch (Exception ex)
        {
            // Captura errores de permisos de escritura o problemas al compilar el archivo
            System.Diagnostics.Debug.WriteLine($"Error al exportar: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenInSlicer()
    {
        // Lógica para lanzar el proceso del Slicer (Cura, PrusaSlicer, BambuStudio)
        // System.Diagnostics.Process.Start("ruta_del_slicer", "ruta_del_archivo_temporal");
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        // Regresa al menú anterior restableciendo el estado de edición
        _navigationController.NavigateTo(new DashboardViewModel(_navigationController));
    }
}