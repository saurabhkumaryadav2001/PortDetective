using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PortDetective.Helpers;
using PortDetective.Interfaces;
using PortDetective.Models;
using PortDetective.Utilities;

namespace PortDetective.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // ---------------------------------------------------------------
        // Dependencies
        // ---------------------------------------------------------------
        private readonly IPortScannerService _portScanner;
        private readonly IProcessService _processService;
        private readonly IPortMonitorService _monitor;
        private readonly ISettingsService _settings;
        private readonly ILogger<MainViewModel> _logger;

        // ---------------------------------------------------------------
        // Backing fields
        // ---------------------------------------------------------------
        private string _portInput = string.Empty;
        private string _rangeStart = "3000";
        private string _rangeEnd = "9000";
        private string _searchQuery = string.Empty;
        private PortInfoViewModel? _selectedPort;
        private bool _isBusy;
        private string _statusMessage = "Ready";
        private string _lastRefreshTime = "Never";
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private bool _autoRefreshEnabled;
        private int _autoRefreshInterval;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------
        public MainViewModel(
            IPortScannerService portScanner,
            IProcessService processService,
            IPortMonitorService monitor,
            ISettingsService settings,
            ILogger<MainViewModel> logger)
        {
            _portScanner    = portScanner;
            _processService = processService;
            _monitor        = monitor;
            _settings       = settings;
            _logger         = logger;

            // Wire monitor events
            _monitor.PortsRefreshed += OnPortsRefreshed;
            _monitor.RefreshError   += OnRefreshError;

            // Init quick ports
            QuickPorts = new ObservableCollection<QuickPortViewModel>(
                QuickPort.DefaultPorts.Select(p => new QuickPortViewModel(p)));

            // Init from settings
            _autoRefreshEnabled  = settings.Current.AutoRefreshEnabled;
            _autoRefreshInterval = settings.Current.AutoRefreshIntervalSeconds;

            // Commands
            FindProcessCommand       = new AsyncRelayCommand(FindProcessAsync, () => !IsBusy);
            ScanRangeCommand         = new AsyncRelayCommand(ScanRangeAsync, () => !IsBusy);
            RefreshCommand           = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
            KillProcessCommand       = new AsyncRelayCommand(KillProcessAsync, CanKillProcess);
            CopyPidCommand           = new RelayCommand(CopyPid, _ => SelectedPort != null);
            CopyPortCommand          = new RelayCommand(CopyPort, _ => SelectedPort != null);
            CopyPathCommand          = new RelayCommand(CopyPath, _ => SelectedPort != null);
            OpenFileLocationCommand  = new RelayCommand(OpenFileLocation, _ => SelectedPort != null);
            QuickPortCommand         = new AsyncRelayCommand(QuickPortCheckAsync);
            ClearSearchCommand       = new RelayCommand(_ => SearchQuery = string.Empty);
            OpenSettingsCommand      = new RelayCommand(_ => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        }

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------
        public event EventHandler? OpenSettingsRequested;
        public event EventHandler<string>? ShowMessage;
        public event EventHandler<(string Title, string Message, Action OnConfirm)>? ConfirmRequested;

        // ---------------------------------------------------------------
        // Collections
        // ---------------------------------------------------------------
        private ObservableCollection<PortInfoViewModel> _allPorts = new();
        public ObservableCollection<PortInfoViewModel> FilteredPorts { get; } = new();
        public ObservableCollection<QuickPortViewModel> QuickPorts { get; }
        public ProcessDetailsViewModel DetailsViewModel { get; } = new();

        // ---------------------------------------------------------------
        // Properties
        // ---------------------------------------------------------------
        public string PortInput
        {
            get => _portInput;
            set => SetProperty(ref _portInput, value);
        }

        public string RangeStart
        {
            get => _rangeStart;
            set => SetProperty(ref _rangeStart, value);
        }

        public string RangeEnd
        {
            get => _rangeEnd;
            set => SetProperty(ref _rangeEnd, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                    ApplyFilter();
            }
        }

        public PortInfoViewModel? SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (SetProperty(ref _selectedPort, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    _ = LoadDetailsAsync(value);
                }
            }
        }

        public bool HasSelection => _selectedPort != null;

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string LastRefreshTime
        {
            get => _lastRefreshTime;
            private set => SetProperty(ref _lastRefreshTime, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                if (SetProperty(ref _autoRefreshEnabled, value))
                {
                    if (value) _ = _monitor.StartAsync();
                    else _monitor.Stop();
                }
            }
        }

        public int AutoRefreshInterval
        {
            get => _autoRefreshInterval;
            set
            {
                if (SetProperty(ref _autoRefreshInterval, value))
                    _monitor.SetInterval(TimeSpan.FromSeconds(value));
            }
        }

        public int TotalPortCount => _allPorts.Count;
        public int FilteredPortCount => FilteredPorts.Count;

        // ---------------------------------------------------------------
        // Commands
        // ---------------------------------------------------------------
        public ICommand FindProcessCommand      { get; }
        public ICommand ScanRangeCommand        { get; }
        public ICommand RefreshCommand          { get; }
        public ICommand KillProcessCommand      { get; }
        public ICommand CopyPidCommand          { get; }
        public ICommand CopyPortCommand         { get; }
        public ICommand CopyPathCommand         { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand QuickPortCommand        { get; }
        public ICommand ClearSearchCommand      { get; }
        public ICommand OpenSettingsCommand     { get; }

        // ---------------------------------------------------------------
        // Initialization
        // ---------------------------------------------------------------
        public async Task InitializeAsync()
        {
            _logger.LogInformation("MainViewModel initializing");

            if (_autoRefreshEnabled)
                await _monitor.StartAsync().ConfigureAwait(false);
            else
                await RefreshAsync().ConfigureAwait(false);
        }

        // ---------------------------------------------------------------
        // Command handlers
        // ---------------------------------------------------------------
        private async Task FindProcessAsync()
        {
            var error = PortValidator.ValidatePort(PortInput);
            if (error != null)
            {
                SetError(error);
                return;
            }

            int port = int.Parse(PortInput.Trim());
            ClearError();
            IsBusy = true;
            StatusMessage = $"Searching port {port}...";

            try
            {
                var info = await _portScanner.GetPortInfoAsync(port).ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (info == null)
                    {
                        SetError($"Port {port} is not in use.");
                        StatusMessage = $"Port {port} is free";
                    }
                    else
                    {
                        SearchQuery = port.ToString();
                        StatusMessage = $"Port {port} found — PID {info.ProcessId}";
                    }
                });

                // Also do a full refresh to update the grid
                await DoRefreshAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding port {Port}", port);
                SetError($"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ScanRangeAsync()
        {
            var error = PortValidator.ValidateRange(RangeStart, RangeEnd);
            if (error != null)
            {
                SetError(error);
                return;
            }

            int start = int.Parse(RangeStart.Trim());
            int end   = int.Parse(RangeEnd.Trim());

            ClearError();
            IsBusy = true;
            StatusMessage = $"Scanning ports {start}–{end}...";

            try
            {
                var results = await _portScanner.ScanPortRangeAsync(start, end).ConfigureAwait(false);
                UpdatePortList(results);
                StatusMessage = $"Scan complete — {results.Count} port(s) in use";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning range {Start}-{End}", start, end);
                SetError($"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            await _monitor.RefreshNowAsync().ConfigureAwait(false);
        }

        private async Task KillProcessAsync()
        {
            if (_selectedPort == null) return;

            var vm = _selectedPort;

            if (_processService.IsSystemProcess(vm.ProcessId, vm.ProcessName))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    ShowMessage?.Invoke(this, $"'{vm.ProcessName}' is a critical Windows system process and cannot be terminated."));
                return;
            }

            // Ask for confirmation on the UI thread
            bool confirmed = false;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ConfirmRequested?.Invoke(this, (
                    "Stop Process",
                    $"Are you sure you want to stop:\n\n  Process: {vm.ProcessName}\n  PID: {vm.ProcessId}\n  Port: {vm.Port}\n\nThis may cause data loss.",
                    () => confirmed = true
                ));
            });

            if (!confirmed) return;

            IsBusy = true;
            StatusMessage = $"Stopping {vm.ProcessName} (PID {vm.ProcessId})...";

            try
            {
                bool killed = await _processService.KillProcessAsync(vm.ProcessId).ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                    StatusMessage = killed
                        ? $"Process {vm.ProcessName} (PID {vm.ProcessId}) stopped"
                        : $"Process {vm.ProcessId} no longer exists");

                await Task.Delay(500).ConfigureAwait(false);
                await DoRefreshAsync().ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    SetError($"Access denied — run Port Detective as Administrator to stop '{vm.ProcessName}'."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping process {Pid}", vm.ProcessId);
                SetError($"Error stopping process: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanKillProcess() =>
            !IsBusy && SelectedPort != null && !SelectedPort.IsSystemProcess;

        private void CopyPid(object? _)
        {
            if (SelectedPort == null) return;
            ClipboardHelper.SetText(SelectedPort.ProcessId.ToString());
            StatusMessage = $"Copied PID {SelectedPort.ProcessId} to clipboard";
        }

        private void CopyPort(object? _)
        {
            if (SelectedPort == null) return;
            ClipboardHelper.SetText(SelectedPort.Port.ToString());
            StatusMessage = $"Copied port {SelectedPort.Port} to clipboard";
        }

        private void CopyPath(object? _)
        {
            if (SelectedPort == null) return;
            ClipboardHelper.SetText(SelectedPort.ExecutablePath);
            StatusMessage = $"Copied path to clipboard";
        }

        private void OpenFileLocation(object? _)
        {
            if (SelectedPort == null) return;
            try
            {
                _processService.OpenFileLocation(SelectedPort.ExecutablePath);
            }
            catch (Exception ex)
            {
                SetError($"Cannot open location: {ex.Message}");
            }
        }

        private async Task QuickPortCheckAsync(object? parameter)
        {
            if (parameter is not QuickPortViewModel qp) return;

            qp.IsChecking = true;
            try
            {
                var info = await _portScanner.GetPortInfoAsync(qp.Port).ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    qp.IsInUse = info != null;
                    if (info != null)
                    {
                        SearchQuery = qp.Port.ToString();
                        StatusMessage = $"Port {qp.Port} → {info.ProcessName} (PID {info.ProcessId})";
                    }
                    else
                    {
                        StatusMessage = $"Port {qp.Port} is free";
                    }
                });

                await DoRefreshAsync().ConfigureAwait(false);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() => qp.IsChecking = false);
            }
        }

        // ---------------------------------------------------------------
        // Monitor event handlers
        // ---------------------------------------------------------------
        private void OnPortsRefreshed(object? sender, IReadOnlyList<PortInfo> ports)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdatePortList(ports);
                LastRefreshTime = $"Last refresh: {_monitor.LastRefreshTime:HH:mm:ss}";
                StatusMessage = $"{_allPorts.Count} port(s) active";

                // Update quick-port in-use states
                foreach (var qp in QuickPorts)
                    qp.IsInUse = ports.Any(p => p.Port == qp.Port);
            });
        }

        private void OnRefreshError(object? sender, Exception ex)
        {
            Application.Current?.Dispatcher.Invoke(() =>
                SetError($"Refresh failed: {ex.Message}"));
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------
        private async Task DoRefreshAsync()
        {
            try
            {
                var ports = await _portScanner.GetActivePortsAsync().ConfigureAwait(false);
                OnPortsRefreshed(this, ports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh failed");
            }
        }

        private void UpdatePortList(IReadOnlyList<PortInfo> ports)
        {
            // Preserve selection
            int? selectedPid  = _selectedPort?.ProcessId;
            int? selectedPort = _selectedPort?.Port;

            _allPorts = new ObservableCollection<PortInfoViewModel>(
                ports.Select(p => new PortInfoViewModel(
                    p, _processService.IsSystemProcess(p.ProcessId, p.ProcessName))));

            OnPropertyChanged(nameof(TotalPortCount));
            ApplyFilter();

            // Restore selection if still present
            if (selectedPid.HasValue)
            {
                SelectedPort = FilteredPorts.FirstOrDefault(
                    p => p.ProcessId == selectedPid && p.Port == selectedPort);
            }
        }

        private void ApplyFilter()
        {
            var query = _searchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(query)
                ? _allPorts.AsEnumerable()
                : _allPorts.Where(p =>
                    p.Port.ToString().Contains(query) ||
                    p.ProcessName.ToLowerInvariant().Contains(query) ||
                    p.ProcessId.ToString().Contains(query) ||
                    p.ExecutablePath.ToLowerInvariant().Contains(query));

            FilteredPorts.Clear();
            foreach (var item in filtered.OrderBy(p => p.Port))
                FilteredPorts.Add(item);

            OnPropertyChanged(nameof(FilteredPortCount));
        }

        private async Task LoadDetailsAsync(PortInfoViewModel? vm)
        {
            if (vm == null)
            {
                DetailsViewModel.Clear();
                return;
            }

            var details = await _processService.GetProcessDetailsAsync(vm.ProcessId).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
                DetailsViewModel.Update(vm, details));
        }

        private void SetError(string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ErrorMessage = message;
                HasError = true;
                StatusMessage = message;
            });
        }

        private void ClearError()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ErrorMessage = string.Empty;
                HasError = false;
            });
        }
    }
}
