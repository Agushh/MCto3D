using System;
using System.Collections.Generic;
using System.Text;

namespace MCto3D.Models
{
    public class CustomPalette
    {
        public string Name { get; set; } = string.Empty;
        public List<string> ColorsHex { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsPlaceholder { get; set; }
    }
}
