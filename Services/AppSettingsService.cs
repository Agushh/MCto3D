using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MCto3D.Services
{
    public class CustomPaletteModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string> ColorsHex { get; set; } = new();
    }

    public class AppSettings
    {
        public string LocalFilesPath { get; set; } = string.Empty;
        public string Language { get; set; } = "en-US";
        public bool Use3MfAssemblyMode { get; set; } = true;
        public bool ShowFloor { get; set; } = false;
        public string FloorColorHex { get; set; } = "#1A1A1A";
        public string ModelColorHex { get; set; } = "#FFFFFF";
        public List<CustomPaletteModel> SavedPalettes { get; set; } = new();
    }

    public interface IAppSettingsService
    {
        string LocalFilesPath { get; set; }
        string Language { get; set; }
        bool Use3MfAssemblyMode { get; set; }
        bool ShowFloor { get; set; }
        string FloorColorHex { get; set; }
        string ModelColorHex { get; set; }
        List<CustomPaletteModel> SavedPalettes { get; set; }
        
        void SavePalettes();
        void Load();
        void Save();
    }

    public class AppSettingsService : IAppSettingsService
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private AppSettings _currentSettings;

        public AppSettingsService()
        {
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCto3D");
            _configFilePath = Path.Combine(_configDirectory, "config.json");
            Load();
        }

        public string LocalFilesPath
        {
            get => _currentSettings.LocalFilesPath;
            set { _currentSettings.LocalFilesPath = value; Save(); }
        }

        public string Language
        {
            get => _currentSettings.Language;
            set { _currentSettings.Language = value; Save(); }
        }

        public bool Use3MfAssemblyMode
        {
            get => _currentSettings.Use3MfAssemblyMode;
            set { _currentSettings.Use3MfAssemblyMode = value; Save(); }
        }

        public bool ShowFloor
        {
            get => _currentSettings.ShowFloor;
            set { _currentSettings.ShowFloor = value; Save(); }
        }

        public string FloorColorHex
        {
            get => _currentSettings.FloorColorHex;
            set { _currentSettings.FloorColorHex = value; Save(); }
        }

        public string ModelColorHex
        {
            get => _currentSettings.ModelColorHex;
            set { _currentSettings.ModelColorHex = value; Save(); }
        }

        public List<CustomPaletteModel> SavedPalettes
        {
            get => _currentSettings.SavedPalettes;
            set { _currentSettings.SavedPalettes = value; Save(); }
        }

        public void SavePalettes() => Save();

        public void Load()
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);
                    _currentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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

            if (string.IsNullOrEmpty(_currentSettings.LocalFilesPath))
            {
                _currentSettings.LocalFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCto3D");
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                    Directory.CreateDirectory(_configDirectory);

                string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar config.json: " + ex.Message);
            }
        }
    }
}
