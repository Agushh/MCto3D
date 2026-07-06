using System;
using System.Collections.Generic;
using System.Text;

namespace MCto3D.Models
{
    public class AppSettings
    {
        public string LocalFilesPath { get; set; } = string.Empty;
        public string Language { get; set; } = "en-US";
        public bool Use3MfAssemblyMode { get; set; } = true;
        public bool ShowFloor { get; set; } = false;
        public string FloorColorHex { get; set; } = "#1A1A1A";
        public string ModelColorHex { get; set; } = "#FFFFFF";
        public List<CustomPalette> SavedPalettes { get; set; } = new();
    }
}
