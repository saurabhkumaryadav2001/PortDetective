using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PortDetective.Interfaces;
using PortDetective.Models;

namespace PortDetective.Services
{
    /// <summary>
    /// Provides process information and safe termination using System.Diagnostics
    /// and WMI (System.Management) for command-line retrieval.
    /// </summary>
    public sealed class ProcessService : IProcessService
    {
        private readonly ILogger<ProcessService> _logger;

        // Well-known critical Windows system process names (lowercase)
        private static readonly HashSet<string> CriticalProcessNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "system", "smss", "csrss", "wininit", "winlogon", "services",
            "lsass", "lsm", "svchost", "ntoskrnl", "registry",
            "dwm", "conhost", "fontdrvhost", "spoolsv"
        };

        // PIDs that are always system-protected
        private static readonly HashSet<int> CriticalPids = new() { 0, 4 };

        public ProcessService(ILogger<ProcessService> logger)
        {
            _logger = logger;
        }

        public Task<ProcessDetails?> GetProcessDetailsAsync(int processId, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GetProcessDetailsCore(processId), cancellationToken);
        }

        public Task<bool> KillProcessAsync(int processId, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                _logger.LogWarning("Attempting to terminate process PID {Pid}", processId);

                try
                {
                    using var process = Process.GetProcessById(processId);
                    process.Kill(entireProcessTree: false);
                    _logger.LogInformation("Process PID {Pid} terminated successfully", processId);
                    return true;
                }
                catch (ArgumentException)
                {
                    _logger.LogWarning("Process PID {Pid} no longer exists", processId);
                    return false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Access denied when terminating PID {Pid}", processId);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error terminating PID {Pid}", processId);
                    throw;
                }
            }, cancellationToken);
        }

        public bool IsSystemProcess(int processId, string processName)
        {
            if (CriticalPids.Contains(processId)) return true;
            if (string.IsNullOrWhiteSpace(processName)) return false;

            string nameWithoutExt = Path.GetFileNameWithoutExtension(processName);
            return CriticalProcessNames.Contains(processName) ||
                   CriticalProcessNames.Contains(nameWithoutExt);
        }

        public void OpenFileLocation(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                _logger.LogWarning("Cannot open file location — path does not exist: {Path}", executablePath);
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"/select,\"{executablePath}\"");
                _logger.LogDebug("Opened Explorer at {Path}", executablePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open file location for {Path}", executablePath);
                throw;
            }
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private ProcessDetails? GetProcessDetailsCore(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);

                var details = new ProcessDetails
                {
                    ProcessId   = processId,
                    ProcessName = process.ProcessName,
                    Status      = "Running",
                    IsSystemProcess = IsSystemProcess(processId, process.ProcessName),
                };

                // Executable path
                try
                {
                    details.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
                {
                    details.ExecutablePath = string.Empty;
                }

                // Start time
                try
                {
                    details.StartTime = process.StartTime;
                }
                catch
                {
                    details.StartTime = null;
                }

                // Command line via WMI
                details.CommandLine = GetCommandLineViaWmi(processId);

                // Username via WMI
                details.UserName = GetUserNameViaWmi(processId);

                return details;
            }
            catch (ArgumentException)
            {
                _logger.LogDebug("Process {Pid} no longer exists when fetching details", processId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching details for PID {Pid}", processId);
                return null;
            }
        }

        private string GetCommandLineViaWmi(int processId)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
                using var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    return obj["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "WMI CommandLine query failed for PID {Pid}", processId);
            }
            return string.Empty;
        }

        private string GetUserNameViaWmi(int processId)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Process WHERE ProcessId = {processId}");
                using var results = searcher.Get();
                foreach (ManagementObject obj in results)
                {
                    string[] argList = { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                        return $"{argList[1]}\\{argList[0]}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "WMI GetOwner query failed for PID {Pid}", processId);
            }
            return string.Empty;
        }
    }
}
