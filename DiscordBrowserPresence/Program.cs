using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DiscordBrowserPresence
{
    class Program
    {
        private static ConfigManager? _configManager;
        private static BrowserMonitorService? _monitorService;
        private static bool _isRunning = false;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--silent")
            {
                await RunSilentMode();
                return;
            }
            _configManager = new ConfigManager();
            if (_configManager.Config.StartMinimized)
            {
                HideConsole();
            }
            if (_configManager.Config.FirstTimeSetup)
            {
                var setupManager = new DiscordSetupManager(_configManager);
                bool setupComplete = await setupManager.PerformFirstTimeSetup();

                if (!setupComplete)
                {
                    Console.WriteLine("Setup was not completed. Application will exit.");
                    Console.ReadKey();
                    return;
                }
            }
            var validator = new DiscordSetupManager(_configManager);
            if (!validator.ValidateDiscordApp())
            {
                Console.WriteLine("Discord setup validation failed.");
                Console.WriteLine("Please run the application without --silent flag to setup.");
                Console.ReadKey();
                return;
            }
            if (_configManager.Config.AutoStart)
            {
                SetAutoStart(true);
            }
            _monitorService = new BrowserMonitorService(_configManager);
            _monitorService.OnStatusUpdate += OnStatusUpdate;
            _monitorService.OnBrowserChanged += OnBrowserChanged;
            if (_configManager.Config.StartMinimized)
            {
                await StartMonitoringSilently();
            }
            else
            {
                await ShowMenuAsync();
            }
        }

        private static async Task RunSilentMode()
        {
            _configManager = new ConfigManager();

            if (_configManager.Config.FirstTimeSetup)
            {
                Environment.Exit(1);
                return;
            }
            _monitorService = new BrowserMonitorService(_configManager);
            await _monitorService.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task StartMonitoringSilently()
        {
            Console.WriteLine("Starting in silent mode...");
            bool started = await _monitorService!.StartAsync();

            if (started)
            {
                Console.WriteLine("Monitoring started. Application is running in background.");
                Console.WriteLine("Press Ctrl+C to exit.");
                await Task.Delay(-1);
            }
        }

        private static async Task ShowMenuAsync()
        {
            while (true)
            {
                Console.Clear();
                ShowHeader();

                var choice = ShowMenu();

                switch (choice)
                {
                    case 1:
                        await StartMonitoringAsync();
                        break;
                    case 2:
                        StopMonitoring();
                        break;
                    case 3:
                        await ShowSettingsAsync();
                        break;
                    case 4:
                        TestDetection();
                        break;
                    case 5:
                        ExitApplication();
                        return;
                }
            }
        }

        private static void ShowHeader()
        {
            Console.WriteLine("=== Discord Browser Presence ===");
            Console.WriteLine($"Status: {(_isRunning ? "RUNNING" : "STOPPED")}");
            Console.WriteLine($"Config: {ConfigManager.GetConfigPath()}");
            Console.WriteLine();
        }

        private static int ShowMenu()
        {
            Console.WriteLine("1. Start Monitoring");
            Console.WriteLine("2. Stop Monitoring");
            Console.WriteLine("3. Settings");
            Console.WriteLine("4. Test Browser Detection");
            Console.WriteLine("5. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");

            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 5)
                return choice;

            return 0;
        }

        private static async Task StartMonitoringAsync()
        {
            if (_isRunning)
            {
                Console.WriteLine("Monitoring is already running.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Starting browser monitoring...");

            bool started = await _monitorService!.StartAsync();
            if (started)
            {
                _isRunning = true;
                Console.WriteLine("\nMonitoring started successfully!");
                Console.WriteLine("Minimize this window to run in background.");
                Console.WriteLine("Press any key to return to menu...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("\nFailed to start monitoring.");
                Console.WriteLine("Make sure Discord is running and setup is complete.");
                Console.ReadKey();
            }
        }

        private static void StopMonitoring()
        {
            if (!_isRunning)
            {
                Console.WriteLine("Monitoring is not running.");
                Console.ReadKey();
                return;
            }

            _monitorService!.Stop();
            _isRunning = false;

            Console.WriteLine("Monitoring stopped.");
            Console.ReadKey();
        }

        private static async Task ShowSettingsAsync()
        {
            Console.Clear();
            Console.WriteLine("=== Settings ===");
            Console.WriteLine();
            Console.WriteLine($"1. Discord App ID: {_configManager!.Config.DiscordApplicationId}");
            Console.WriteLine($"2. Update Interval: {_configManager.Config.UpdateIntervalSeconds} seconds");
            Console.WriteLine($"3. Start Minimized: {_configManager.Config.StartMinimized}");
            Console.WriteLine($"4. Auto-start with Windows: {_configManager.Config.AutoStart}");
            Console.WriteLine($"5. Run First-time Setup Again");
            Console.WriteLine($"6. Back to Menu");
            Console.WriteLine();
            Console.Write("Select setting to change: ");

            if (int.TryParse(Console.ReadLine(), out int choice))
            {
                switch (choice)
                {
                    case 1:
                        Console.Write("Enter new Discord Application ID: ");
                        _configManager.Config.DiscordApplicationId = Console.ReadLine() ?? "";
                        break;
                    case 2:
                        Console.Write("Enter update interval (seconds): ");
                        if (int.TryParse(Console.ReadLine(), out int interval))
                            _configManager.Config.UpdateIntervalSeconds = Math.Max(2, interval);
                        break;
                    case 3:
                        _configManager.Config.StartMinimized = !_configManager.Config.StartMinimized;
                        break;
                    case 4:
                        _configManager.Config.AutoStart = !_configManager.Config.AutoStart;
                        SetAutoStart(_configManager.Config.AutoStart);
                        break;
                    case 5:
                        var setupManager = new DiscordSetupManager(_configManager);
                        await setupManager.PerformFirstTimeSetup();
                        break;
                }

                _configManager.SaveConfig();
            }
        }

        private static void TestDetection()
        {
            Console.Clear();
            Console.WriteLine("Testing browser detection...");
            Console.WriteLine();

            var detector = new BrowserDetector();
            var browsers = detector.GetActiveBrowsers();

            if (browsers.Count == 0)
            {
                Console.WriteLine("No browsers detected. Make sure a browser window is open.");
            }
            else
            {
                Console.WriteLine($"Detected {browsers.Count} browser(s):");
                Console.WriteLine();

                foreach (var browser in browsers)
                {
                    string mode = browser.IsIncognito ? "[Incognito]" :
                                 browser.IsPrivate ? "[Private]" : "[Normal]";

                    Console.WriteLine($"{mode} {browser.BrowserName}");
                    Console.WriteLine($"  Title: {browser.TabTitle}");
                    Console.WriteLine($"  Icon: {browser.IconKey}");
                    if (!string.IsNullOrEmpty(browser.Url))
                        Console.WriteLine($"  URL: {browser.Url}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ExitApplication()
        {
            Console.WriteLine("Shutting down...");
            _monitorService?.Dispose();
            if (!_configManager!.Config.AutoStart)
            {
                SetAutoStart(false);
            }

            Environment.Exit(0);
        }

        private static void SetAutoStart(bool enable)
        {
            try
            {
                string appName = "DiscordBrowserPresence";
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue(appName, $"\"{appPath}\" --silent");
                            Console.WriteLine("✓ Auto-start enabled");
                        }
                        else
                        {
                            key.DeleteValue(appName, false);
                            Console.WriteLine("✓ Auto-start disabled");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto-start configuration failed: {ex.Message}");
            }
        }

        private static void HideConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        private static void ShowConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
        }

        private static void OnStatusUpdate(object? sender, string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private static void OnBrowserChanged(object? sender, BrowserInfo browserInfo)
        {
            string mode = browserInfo.IsIncognito ? "(Incognito)" :
                         browserInfo.IsPrivate ? "(Private)" : "";

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {browserInfo.BrowserName} {mode}: {browserInfo.TabTitle}");
        }
    }
}