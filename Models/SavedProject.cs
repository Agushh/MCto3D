using System;

namespace MCto3D.Models;

public class SavedProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; } = DateTime.Now;
    
    // Configuraion snapshot
    public float BlockScale { get; set; } = 1.0f;
    public string GeometryMode { get; set; } = "Bloques sólidos";
    public string ExportFormat { get; set; } = "STL";
    public bool IsSingleColorMode { get; set; } = true;
    
    // UI helpers
    public string FormattedDate => CreationDate.ToString("dd MMM yyyy, HH:mm");
    public string FileSizeStr => "Aprox 2.4 MB"; // Mock para el peso
}
