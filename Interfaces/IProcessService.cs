using System.Threading;
using System.Threading.Tasks;
using PortDetective.Models;

namespace PortDetective.Interfaces
{
    /// <summary>
    /// Provides process information and management capabilities.
    /// </summary>
    public interface IProcessService
    {
        /// <summary>Returns extended details for a process by PID.</summary>
        Task<ProcessDetails?> GetProcessDetailsAsync(int processId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Terminates a process by PID.
        /// Returns true on success, false if the process no longer exists.
        /// Throws UnauthorizedAccessException if access is denied.
        /// </summary>
        Task<bool> KillProcessAsync(int processId, CancellationToken cancellationToken = default);

        /// <summary>Returns true if the given PID belongs to a critical Windows system process.</summary>
        bool IsSystemProcess(int processId, string processName);

        /// <summary>Opens Windows Explorer at the file's containing folder.</summary>
        void OpenFileLocation(string executablePath);
    }
}
