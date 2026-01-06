using DiscordRPC;
using System;
using System.Threading.Tasks;

namespace DiscordBrowserPresence
{
    public class DiscordPresenceManager : IDisposable
    {
        private DiscordRpcClient? _client;
        private bool _isInitialized = false;
        private readonly ConfigManager _configManager;

        public event EventHandler<string>? OnLogMessage;
        public event EventHandler<bool>? OnConnectionStatusChanged;

        public bool IsConnected => _isInitialized && _client?.IsDisposed == false;

        public DiscordPresenceManager(ConfigManager configManager)
        {
            _configManager = configManager;
        }

        public async Task<bool> InitializeAsync()
        {
            if (string.IsNullOrWhiteSpace(_configManager.Config.DiscordApplicationId))
            {
                LogMessage("No Discord Application ID configured.");
                return false;
            }

            try
            {
                _client = new DiscordRpcClient(_configManager.Config.DiscordApplicationId)
                {
                    SkipIdenticalPresence = false
                };

                _client.OnReady += (sender, e) =>
                {
                    _isInitialized = true;
                    LogMessage($"Connected to Discord as {e.User.Username}");
                    OnConnectionStatusChanged?.Invoke(this, true);
                };

                _client.OnClose += (sender, e) =>
                {
                    _isInitialized = false;
                    LogMessage("Disconnected from Discord");
                    OnConnectionStatusChanged?.Invoke(this, false);
                };

                _client.OnError += (sender, e) =>
                {
                    LogMessage($"Discord error: {e.Message}");
                };

                var timeoutTask = Task.Delay(5000);
                var initializeTask = Task.Run(() => _client.Initialize());

                var completedTask = await Task.WhenAny(initializeTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    LogMessage("Discord connection timeout. Make sure Discord is running.");
                    return false;
                }

                LogMessage("Discord client initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to initialize Discord: {ex.Message}");
                return false;
            }
        }

        public void UpdatePresence(BrowserInfo? browserInfo)
        {
            if (!IsConnected || _client == null || browserInfo == null)
                return;

            try
            {
                var presence = new RichPresence
                {
                    Details = browserInfo.GetDetails(),
                    State = browserInfo.GetStatusText(),
                    Timestamps = Timestamps.Now,
                    Assets = new Assets()
                };

                if (!string.IsNullOrEmpty(browserInfo.IconKey))
                {
                    presence.Assets.LargeImageKey = browserInfo.IconKey.ToLower();
                    presence.Assets.LargeImageText = browserInfo.BrowserName;

                    if (browserInfo.IsIncognito || browserInfo.IsPrivate)
                    {
                        presence.Assets.SmallImageKey = "incognito";
                        presence.Assets.SmallImageText = "Private Browsing";
                    }
                }

                _client.SetPresence(presence);
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating presence: {ex.Message}");
            }
        }

        public void ClearPresence()
        {
            if (_client != null && IsConnected)
            {
                _client.ClearPresence();
                LogMessage("Cleared Discord presence");
            }
        }

        private void LogMessage(string message)
        {
            Console.WriteLine($"[Discord] {message}");
            OnLogMessage?.Invoke(this, message);
        }

        public void Dispose()
        {
            try
            {
                ClearPresence();
                _client?.Dispose();
                _isInitialized = false;
                GC.SuppressFinalize(this);
            }
            catch
            {
            }
        }
    }
}