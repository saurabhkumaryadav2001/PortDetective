using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PortDetective.Interfaces;
using PortDetective.Models;

namespace PortDetective.Services
{
    /// <summary>
    /// Persists AppSettings as JSON in %AppData%\PortDetective\settings.json.
    /// </summary>
    public sealed class SettingsService : ISettingsService
    {
        private const string AppName      = "PortDetective";
        private const string RegistryKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryName = "PortDetective";

        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public AppSettings Current { get; private set; } = new();

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, AppName);
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogInformation("No settings file found; using defaults");
                    Current = new AppSettings();
                    return;
                }

                string json = File.ReadAllText(_settingsFilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                _logger.LogInformation("Settings loaded from {Path}", _settingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings; using defaults");
                Current = new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                Current = settings;
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);
                _logger.LogInformation("Settings saved to {Path}", _settingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                throw;
            }
        }

        public void SetStartWithWindows(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
                if (key == null)
                {
                    _logger.LogWarning("Could not open registry key for startup");
                    return;
                }

                if (enabled)
                {
                    string exe = System.Reflection.Assembly.GetExecutingAssembly().Location
                        .Replace(".dll", ".exe");
                    key.SetValue(RegistryName, $"\"{exe}\"");
                    _logger.LogInformation("Added to Windows startup");
                }
                else
                {
                    key.DeleteValue(RegistryName, throwOnMissingValue: false);
                    _logger.LogInformation("Removed from Windows startup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set startup registry value");
            }
        }
    }
}
