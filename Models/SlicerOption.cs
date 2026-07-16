using System;
using System.Collections.Generic;
using System.Text;

namespace MCto3D.Models
{
    public class SlicerOption
    {
        public string Name { get; set; } = "";
        public string ColorHex { get; set; } = "";
        public string UriScheme { get; set; } = "";
        public string ButtonText => $"{MCto3D.Services.LanguageService.GetString("ConverterOpenIn")} {Name}";
    }
}
