using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBrowserPresence
{
    public class DiscordSetupManager
    {
        private readonly string _iconsFolder;
        private readonly ConfigManager _configManager;

        public DiscordSetupManager(ConfigManager configManager)
        {
            _configManager = configManager;
            _iconsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
        }

        public async Task<bool> PerformFirstTimeSetup()
        {
            Console.Clear();
            Console.WriteLine("=== Discord Browser Presence - First Time Setup ===");
            Console.WriteLine();
            Console.WriteLine("This application needs a Discord Application ID to work.");
            Console.WriteLine();
            Console.WriteLine("Steps:");
            Console.WriteLine("1. I'll help you create a Discord Application");
            Console.WriteLine("2. You'll get an Application ID");
            Console.WriteLine("3. I'll verify your browser icons");
            Console.WriteLine("4. You upload them to Discord");
            Console.WriteLine();
            Console.Write("Press Enter to start setup...");
            Console.ReadLine();
            Console.WriteLine("\nOpening Discord Developer Portal...");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.com/developers/applications",
                    UseShellExecute = true
                });
            }
            catch
            {
                Console.WriteLine("Could not open browser automatically.");
                Console.WriteLine("Please visit: https://discord.com/developers/applications");
            }
            Console.WriteLine("\nPlease follow these steps:");
            Console.WriteLine("1. Click 'New Application'");
            Console.WriteLine("2. Name it 'Browser Presence' (or any name you like)");
            Console.WriteLine("3. Click 'Create'");
            Console.WriteLine();
            Console.Write("Press Enter when you've created the application...");
            Console.ReadLine();
            Console.WriteLine("\nNow, copy your Application ID:");
            Console.WriteLine("1. On the 'General Information' page");
            Console.WriteLine("2. Find 'APPLICATION ID'");
            Console.WriteLine("3. Copy the long number");
            Console.WriteLine();
            string? appId = GetApplicationIdFromUser();
            if (string.IsNullOrWhiteSpace(appId))
            {
                Console.WriteLine("Setup cancelled.");
                return false;
            }
            _configManager.Config.DiscordApplicationId = appId;
            Console.WriteLine("\nNow let's verify your icons and set up Rich Presence:");
            Console.WriteLine("1. In the left menu, click 'Rich Presence'");
            Console.WriteLine("2. Click 'Art Assets'");
            Console.WriteLine("3. You'll upload browser icons from your icons folder");
            Console.WriteLine();
            VerifyIconsExist();
            Console.WriteLine("\nPlease upload these icons from your icons folder to Discord:");
            Console.WriteLine("- brave.png (Brave Browser)");
            Console.WriteLine("- chrome.png (Google Chrome)");
            Console.WriteLine("- edge.png (Microsoft Edge)");
            Console.WriteLine("- firefox.png (Mozilla Firefox)");
            Console.WriteLine("- incognito.png (Private mode icon)");
            Console.WriteLine("- opera.png (Opera Browser)");
            Console.WriteLine("- vivaldi.png (Vivaldi Browser)");
            Console.WriteLine();
            Console.WriteLine("Important: Upload as PNG files, name them exactly as shown above.");
            Console.WriteLine();
            Console.Write("Press Enter when you've uploaded all icons...");
            Console.ReadLine();
            _configManager.Config.FirstTimeSetup = false;
            _configManager.SaveConfig();
            Console.WriteLine("\n✅ Setup complete!");
            Console.WriteLine("Your Discord Application ID has been saved.");
            Console.WriteLine("The application will now start monitoring.");
            Console.WriteLine();
            Console.Write("Press Enter to continue...");
            Console.ReadLine();

            return true;
        }

        private string? GetApplicationIdFromUser()
        {
            string? appId = "";
            while (true)
            {
                Console.Write("Enter your Application ID (or type 'skip' to cancel): ");
                appId = Console.ReadLine()?.Trim();

                if (appId?.ToLower() == "skip")
                    return null;

                if (string.IsNullOrWhiteSpace(appId))
                {
                    Console.WriteLine("Please enter a valid Application ID.");
                    continue;
                }
                if (long.TryParse(appId, out _))
                {
                    return appId;
                }
                else
                {
                    Console.WriteLine("Invalid Application ID. It should be a numeric value.");
                }
            }
        }

        private void VerifyIconsExist()
        {
            try
            {
                var requiredIcons = new[]
                {
                    "brave.png", "chrome.png", "edge.png", "firefox.png",
                    "incognito.png", "opera.png", "vivaldi.png"
                };

                Console.WriteLine("\nChecking icons in 'icons' folder...");
                Console.WriteLine($"Folder path: {_iconsFolder}");
                Console.WriteLine();

                bool allIconsExist = true;

                if (!Directory.Exists(_iconsFolder))
                {
                    Console.WriteLine("❌ 'icons' folder not found!");
                    Console.WriteLine($"Please create an 'icons' folder here and add your PNG files.");
                    Console.WriteLine($"Expected location: {_iconsFolder}");
                    allIconsExist = false;
                }
                else
                {
                    foreach (var icon in requiredIcons)
                    {
                        string iconPath = Path.Combine(_iconsFolder, icon);

                        if (File.Exists(iconPath))
                        {
                            Console.WriteLine($"✓ {icon}");
                        }
                        else
                        {
                            Console.WriteLine($"✗ {icon} (missing)");
                            allIconsExist = false;
                        }
                    }
                }

                if (!allIconsExist)
                {
                    Console.WriteLine("\n⚠ Some icons are missing!");
                    Console.WriteLine($"Please ensure all icons are in: {_iconsFolder}");

                    try
                    {
                        if (Directory.Exists(_iconsFolder))
                        {
                            Console.WriteLine("\nOpening icons folder...");
                            Process.Start("explorer.exe", _iconsFolder);
                        }
                    }
                    catch
                    {
                    }

                    Console.WriteLine("\nPress Enter to continue anyway, or close and add missing icons...");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("\n✅ All icons found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking icons: {ex.Message}");
            }
        }

        public bool ValidateDiscordApp()
        {
            if (string.IsNullOrWhiteSpace(_configManager.Config.DiscordApplicationId))
            {
                Console.WriteLine("No Discord Application ID configured.");
                return false;
            }
            if (!Directory.Exists(_iconsFolder))
            {
                Console.WriteLine("Icons folder not found.");
                return false;
            }
            var iconFiles = Directory.GetFiles(_iconsFolder, "*.png");
            if (iconFiles.Length == 0)
            {
                Console.WriteLine("No icons found in icons folder.");
                return false;
            }

            Console.WriteLine($"Found {iconFiles.Length} icon(s) in icons folder.");
            return true;
        }
        public string[] GetAvailableIcons()
        {
            if (!Directory.Exists(_iconsFolder))
                return Array.Empty<string>();

            var files = Directory.GetFiles(_iconsFolder, "*.png");
            var icons = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                icons[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return icons;
        }
    }
}