using System;
using System.IO;
using System.Text.Json;

namespace MCto3D.Services
{
    public class CustomPaletteModel
    {
        public string Name { get; set; } = string.Empty;
        public System.Collections.Generic.List<string> ColorsHex { get; set; } = new();
    }

    public class AppSettings
    {
        public string LocalFilesPath { get; set; } = string.Empty;
        public string Language { get; set; } = "en-US";
        public bool Use3MfAssemblyMode { get; set; } = true;
        public bool ShowFloor { get; set; } = false;
        public string FloorColorHex { get; set; } = "#1A1A1A";
        public string ModelColorHex { get; set; } = "#FFFFFF";
        public System.Collections.Generic.List<CustomPaletteModel> SavedPalettes { get; set; } = new();
    }

    public static class AppSettings_Service
    {
        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCto3D");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
        private static AppSettings _currentSettings;

        public static string LocalFilesPath
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.LocalFilesPath;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.LocalFilesPath = value;
                Save();
            }
        }

        public static string Language
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.Language;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.Language = value;
                Save();
            }
        }

        public static bool Use3MfAssemblyMode
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.Use3MfAssemblyMode;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.Use3MfAssemblyMode = value;
                Save();
            }
        }

        public static bool ShowFloor
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.ShowFloor;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.ShowFloor = value;
                Save();
            }
        }

        public static string FloorColorHex
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.FloorColorHex;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.FloorColorHex = value;
                Save();
            }
        }

        public static string ModelColorHex
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.ModelColorHex;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.ModelColorHex = value;
                Save();
            }
        }

        public static System.Collections.Generic.List<CustomPaletteModel> SavedPalettes
        {
            get
            {
                if (_currentSettings == null) Load();
                return _currentSettings.SavedPalettes;
            }
            set
            {
                if (_currentSettings == null) Load();
                _currentSettings.SavedPalettes = value;
                Save();
            }
        }

        public static void SavePalettes()
        {
            Save();
        }

        public static void Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _currentSettings = JsonSerializer.Deserialize<AppSettings>(json);
                }
                catch
                {
                    _currentSettings = new AppSettings();
                }
            }
            else
            {
                _currentSettings = new AppSettings();
            }

            // Default
            if (string.IsNullOrEmpty(_currentSettings.LocalFilesPath))
            {
                _currentSettings.LocalFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCto3D");
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                    Directory.CreateDirectory(ConfigDirectory);

                string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar config.json: " + ex.Message);
            }
        }
    }
}
