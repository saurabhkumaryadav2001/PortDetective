using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PortDetective.Models;

namespace PortDetective.Interfaces
{
    /// <summary>
    /// Monitors active ports on a timer and fires events when the list changes.
    /// </summary>
    public interface IPortMonitorService : IDisposable
    {
        /// <summary>Fired after every successful refresh with the updated port list.</summary>
        event EventHandler<IReadOnlyList<PortInfo>>? PortsRefreshed;

        /// <summary>Fired when a refresh error occurs.</summary>
        event EventHandler<Exception>? RefreshError;

        bool IsRunning { get; }
        DateTime? LastRefreshTime { get; }

        Task StartAsync(CancellationToken cancellationToken = default);
        void Stop();
        Task RefreshNowAsync(CancellationToken cancellationToken = default);

        /// <summary>Changes the polling interval without restarting the monitor.</summary>
        void SetInterval(TimeSpan interval);
    }
}
