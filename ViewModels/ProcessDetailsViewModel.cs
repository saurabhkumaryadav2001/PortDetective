using PortDetective.Helpers;
using PortDetective.Models;

namespace PortDetective.ViewModels
{
    /// <summary>
    /// Binds the details panel shown when a port row is selected.
    /// </summary>
    public class ProcessDetailsViewModel : ViewModelBase
    {
        private ProcessDetails? _details;
        private PortInfoViewModel? _portInfo;

        public bool HasSelection => _portInfo != null;

        public string ProcessName  => _details?.ProcessName  ?? _portInfo?.ProcessName ?? "-";
        public int ProcessId       => _portInfo?.ProcessId   ?? 0;
        public int Port            => _portInfo?.Port        ?? 0;
        public string ExecutablePath => _details?.ExecutablePath ?? _portInfo?.ExecutablePath ?? "-";
        public string CommandLine  => string.IsNullOrWhiteSpace(_details?.CommandLine) ? "N/A" : _details.CommandLine;
        public string StartTime    => _details?.StartTimeDisplay ?? "N/A";
        public string ConnectionState => _portInfo?.ConnectionState ?? "-";
        public string UserName     => string.IsNullOrWhiteSpace(_details?.UserName) ? "N/A" : _details.UserName;
        public bool IsSystemProcess => _portInfo?.IsSystemProcess ?? false;

        public void Update(PortInfoViewModel? portInfo, ProcessDetails? details)
        {
            _portInfo = portInfo;
            _details  = details;

            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ProcessName));
            OnPropertyChanged(nameof(ProcessId));
            OnPropertyChanged(nameof(Port));
            OnPropertyChanged(nameof(ExecutablePath));
            OnPropertyChanged(nameof(CommandLine));
            OnPropertyChanged(nameof(StartTime));
            OnPropertyChanged(nameof(ConnectionState));
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(IsSystemProcess));
        }

        public void Clear()
        {
            _portInfo = null;
            _details  = null;
            Update(null, null);
        }
    }
}
