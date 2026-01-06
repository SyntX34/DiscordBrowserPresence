using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBrowserPresence
{
    public class BrowserMonitorService
    {
        private readonly BrowserDetector _detector;
        private readonly DiscordPresenceManager _discordManager;
        private readonly ConfigManager _configManager;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning = false;

        public event EventHandler<string>? OnStatusUpdate;
        public event EventHandler<BrowserInfo>? OnBrowserChanged;

        public bool IsRunning => _isRunning;

        public BrowserMonitorService(ConfigManager configManager)
        {
            _configManager = configManager;
            _detector = new BrowserDetector();
            _discordManager = new DiscordPresenceManager(configManager);
            _discordManager.OnLogMessage += (sender, msg) => OnStatusUpdate?.Invoke(this, msg);
        }

        public async Task<bool> StartAsync()
        {
            if (_isRunning)
                return true;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            OnStatusUpdate?.Invoke(this, "Connecting to Discord...");
            bool discordConnected = await _discordManager.InitializeAsync();
            if (!discordConnected)
            {
                OnStatusUpdate?.Invoke(this, "Failed to connect to Discord. Make sure Discord is running.");
                _isRunning = false;
                return false;
            }

            OnStatusUpdate?.Invoke(this, "Starting browser monitoring...");
            _ = Task.Run(async () => await MonitorLoopAsync(), _cancellationTokenSource.Token);

            return true;
        }

        private async Task MonitorLoopAsync()
        {
            BrowserInfo? lastBrowser = null;
            int errorCount = 0;
            const int maxErrors = 5;

            while (_cancellationTokenSource != null &&
                   !_cancellationTokenSource.Token.IsCancellationRequested &&
                   errorCount < maxErrors)
            {
                try
                {
                    var activeBrowser = _detector.GetActiveBrowser();

                    if (activeBrowser != null)
                    {
                        if (lastBrowser == null ||
                            lastBrowser.BrowserName != activeBrowser.BrowserName ||
                            lastBrowser.TabTitle != activeBrowser.TabTitle ||
                            lastBrowser.IsIncognito != activeBrowser.IsIncognito)
                        {
                            _discordManager.UpdatePresence(activeBrowser);
                            OnBrowserChanged?.Invoke(this, activeBrowser);
                            string mode = activeBrowser.IsIncognito ? " (Incognito)" :
                                         activeBrowser.IsPrivate ? " (Private)" : "";
                            OnStatusUpdate?.Invoke(this,
                                $"Active: {activeBrowser.BrowserName}{mode} - {activeBrowser.TabTitle}");

                            lastBrowser = activeBrowser;
                            errorCount = 0;
                        }
                    }
                    else if (lastBrowser != null)
                    {
                        _discordManager.ClearPresence();
                        lastBrowser = null;
                        OnStatusUpdate?.Invoke(this, "No active browser detected");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    OnStatusUpdate?.Invoke(this, $"Monitoring error ({errorCount}/{maxErrors}): {ex.Message}");

                    if (errorCount >= maxErrors)
                    {
                        OnStatusUpdate?.Invoke(this, "Too many errors. Stopping monitoring.");
                        break;
                    }
                }

                await Task.Delay(_configManager.Config.UpdateIntervalSeconds * 1000,
                               _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _cancellationTokenSource?.Cancel();
            _isRunning = false;
            _discordManager?.ClearPresence();
            _discordManager?.Dispose();
            OnStatusUpdate?.Invoke(this, "Monitoring stopped");
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}