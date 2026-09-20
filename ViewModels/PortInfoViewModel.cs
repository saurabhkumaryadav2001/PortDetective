using System.Net.NetworkInformation;
using PortDetective.Helpers;
using PortDetective.Models;

namespace PortDetective.ViewModels
{
    /// <summary>
    /// Wraps a PortInfo model for display in the DataGrid.
    /// </summary>
    public class PortInfoViewModel : ViewModelBase
    {
        private bool _isSelected;

        public PortInfo Model { get; }

        public int Port            => Model.Port;
        public string Protocol     => Model.Protocol;
        public int ProcessId       => Model.ProcessId;
        public string ProcessName  => Model.ProcessName;
        public string ExecutablePath => Model.ExecutablePath;
        public string ConnectionState => Model.ConnectionStateDisplay;
        public string LocalAddress => Model.LocalAddress;

        public bool IsSystemProcess { get; init; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>Short display name (filename only) for the process executable.</summary>
        public string ProcessNameDisplay =>
            string.IsNullOrWhiteSpace(Model.ExecutablePath)
                ? Model.ProcessName
                : System.IO.Path.GetFileName(Model.ExecutablePath);

        public string StatusBadge => Model.ConnectionState switch
        {
            TcpState.Listen      => "Listening",
            TcpState.Established => "Established",
            _                    => Model.ConnectionStateDisplay
        };

        public PortInfoViewModel(PortInfo model, bool isSystemProcess = false)
        {
            Model = model;
            IsSystemProcess = isSystemProcess;
        }
    }
}
