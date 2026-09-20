using System;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PortDetective.Helpers;
using PortDetective.Interfaces;
using PortDetective.Models;

namespace PortDetective.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IPortMonitorService _monitor;
        private readonly ILogger<SettingsViewModel> _logger;

        // Backing fields
        private int _autoRefreshInterval;
        private bool _autoRefreshEnabled;
        private int _defaultPortRangeStart;
        private int _defaultPortRangeEnd;
        private bool _startMinimized;
        private bool _startWithWindows;
        private string _theme = "Dark";
        private bool _showSystemProcesses;
        private string _statusMessage = string.Empty;

        public event EventHandler? CloseRequested;

        public SettingsViewModel(
            ISettingsService settingsService,
            IPortMonitorService monitor,
            ILogger<SettingsViewModel> logger)
        {
            _settingsService = settingsService;
            _monitor         = monitor;
            _logger          = logger;

            SaveCommand   = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));

            LoadFromSettings();
        }

        // ---------------------------------------------------------------
        // Properties
        // ---------------------------------------------------------------
        public int AutoRefreshInterval
        {
            get => _autoRefreshInterval;
            set => SetProperty(ref _autoRefreshInterval, Math.Clamp(value, 1, 3600));
        }

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set => SetProperty(ref _autoRefreshEnabled, value);
        }

        public int DefaultPortRangeStart
        {
            get => _defaultPortRangeStart;
            set => SetProperty(ref _defaultPortRangeStart, value);
        }

        public int DefaultPortRangeEnd
        {
            get => _defaultPortRangeEnd;
            set => SetProperty(ref _defaultPortRangeEnd, value);
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set => SetProperty(ref _startMinimized, value);
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set => SetProperty(ref _startWithWindows, value);
        }

        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        public bool ShowSystemProcesses
        {
            get => _showSystemProcesses;
            set => SetProperty(ref _showSystemProcesses, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string[] ThemeOptions { get; } = { "Dark", "Light" };

        // ---------------------------------------------------------------
        // Commands
        // ---------------------------------------------------------------
        public ICommand SaveCommand   { get; }
        public ICommand CancelCommand { get; }

        // ---------------------------------------------------------------
        // Private
        // ---------------------------------------------------------------
        private void LoadFromSettings()
        {
            var s = _settingsService.Current;
            AutoRefreshInterval     = s.AutoRefreshIntervalSeconds;
            AutoRefreshEnabled      = s.AutoRefreshEnabled;
            DefaultPortRangeStart   = s.DefaultPortRangeStart;
            DefaultPortRangeEnd     = s.DefaultPortRangeEnd;
            StartMinimized          = s.StartMinimized;
            StartWithWindows        = s.StartWithWindows;
            Theme                   = s.Theme;
            ShowSystemProcesses     = s.ShowSystemProcesses;
        }

        private void Save()
        {
            try
            {
                var settings = new AppSettings
                {
                    AutoRefreshIntervalSeconds = AutoRefreshInterval,
                    AutoRefreshEnabled         = AutoRefreshEnabled,
                    DefaultPortRangeStart      = DefaultPortRangeStart,
                    DefaultPortRangeEnd        = DefaultPortRangeEnd,
                    StartMinimized             = StartMinimized,
                    StartWithWindows           = StartWithWindows,
                    Theme                      = Theme,
                    ShowSystemProcesses        = ShowSystemProcesses,
                };

                _settingsService.Save(settings);
                _settingsService.SetStartWithWindows(StartWithWindows);
                _monitor.SetInterval(TimeSpan.FromSeconds(AutoRefreshInterval));

                StatusMessage = "Settings saved successfully.";
                _logger.LogInformation("Settings saved");

                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                StatusMessage = $"Error saving settings: {ex.Message}";
            }
        }
    }
}
