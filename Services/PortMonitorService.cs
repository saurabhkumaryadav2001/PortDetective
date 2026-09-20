using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PortDetective.Interfaces;
using PortDetective.Models;

namespace PortDetective.Services
{
    /// <summary>
    /// Background monitor that periodically refreshes the port list and raises events.
    /// </summary>
    public sealed class PortMonitorService : IPortMonitorService
    {
        private readonly IPortScannerService _portScanner;
        private readonly ILogger<PortMonitorService> _logger;

        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private TimeSpan _interval;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public event EventHandler<IReadOnlyList<PortInfo>>? PortsRefreshed;
        public event EventHandler<Exception>? RefreshError;

        public bool IsRunning => _pollingTask is { IsCompleted: false };
        public DateTime? LastRefreshTime { get; private set; }

        public PortMonitorService(
            IPortScannerService portScanner,
            ILogger<PortMonitorService> logger,
            TimeSpan? initialInterval = null)
        {
            _portScanner = portScanner;
            _logger       = logger;
            _interval     = initialInterval ?? TimeSpan.FromSeconds(10);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsRunning) return;

            _logger.LogInformation("Port monitor starting with interval {Interval}", _interval);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pollingTask = RunPollingLoopAsync(_cts.Token);

            // Eagerly do a first refresh
            await RefreshNowAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Stop()
        {
            _logger.LogInformation("Port monitor stopping");
            _cts?.Cancel();
        }

        public async Task RefreshNowAsync(CancellationToken cancellationToken = default)
        {
            if (!await _refreshLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
                return; // A refresh is already in progress

            try
            {
                var ports = await _portScanner.GetActivePortsAsync(cancellationToken).ConfigureAwait(false);
                LastRefreshTime = DateTime.Now;
                PortsRefreshed?.Invoke(this, ports);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during port refresh");
                RefreshError?.Invoke(this, ex);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public void SetInterval(TimeSpan interval)
        {
            _interval = interval;
            _logger.LogDebug("Monitor interval updated to {Interval}", interval);
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _refreshLock.Dispose();
        }

        // ---------------------------------------------------------------
        // Private
        // ---------------------------------------------------------------

        private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);
                    await RefreshNowAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in polling loop");
                }
            }

            _logger.LogInformation("Port monitor polling loop exited");
        }
    }
}
