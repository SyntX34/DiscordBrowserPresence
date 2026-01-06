using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordBrowserPresence
{
    public class BrowserDetector
    {
        private readonly Dictionary<string, BrowserConfig> _browserConfigs = new()
        {
            {
                "chrome",
                new BrowserConfig
                {
                    Name = "Google Chrome",
                    ProcessNames = new[] { "chrome" },
                    NormalTitles = new[] { " - Google Chrome" },
                    IncognitoTitles = new[] { "Incognito", "Guest" },
                    IconKey = "chrome",
                    IsChromeBased = true
                }
            },
            {
                "msedge",
                new BrowserConfig
                {
                    Name = "Microsoft Edge",
                    ProcessNames = new[] { "msedge" },
                    NormalTitles = new[] { " - Microsoft Edge" },
                    IncognitoTitles = new[] { "InPrivate", "InPrivate Browsing" },
                    IconKey = "edge",
                    IsChromeBased = true
                }
            },
            {
                "firefox",
                new BrowserConfig
                {
                    Name = "Mozilla Firefox",
                    ProcessNames = new[] { "firefox" },
                    NormalTitles = new[] { " - Mozilla Firefox" },
                    PrivateTitles = new[] { "Private Browsing" },
                    IconKey = "firefox",
                    IsChromeBased = false
                }
            },
            {
                "opera",
                new BrowserConfig
                {
                    Name = "Opera",
                    ProcessNames = new[] { "opera" },
                    NormalTitles = new[] { " - Opera" },
                    PrivateTitles = new[] { "Private Mode" },
                    IconKey = "opera",
                    IsChromeBased = true
                }
            },
            {
                "brave",
                new BrowserConfig
                {
                    Name = "Brave",
                    ProcessNames = new[] { "brave" },
                    NormalTitles = new[] { " - Brave" },
                    PrivateTitles = new[] { "Private Window" },
                    IconKey = "brave",
                    IsChromeBased = true
                }
            },
            {
                "vivaldi",
                new BrowserConfig
                {
                    Name = "Vivaldi",
                    ProcessNames = new[] { "vivaldi" },
                    NormalTitles = new[] { " - Vivaldi" },
                    PrivateTitles = new[] { "Private Window" },
                    IconKey = "vivaldi",
                    IsChromeBased = true
                }
            }
        };

        private class BrowserConfig
        {
            public string Name { get; set; } = "Unknown Browser";
            public string[] ProcessNames { get; set; } = Array.Empty<string>();
            public string[] NormalTitles { get; set; } = Array.Empty<string>();
            public string[] IncognitoTitles { get; set; } = Array.Empty<string>();
            public string[] PrivateTitles { get; set; } = Array.Empty<string>();
            public string IconKey { get; set; } = "browser";
            public bool IsChromeBased { get; set; } = false;
        }

        public List<BrowserInfo> GetActiveBrowsers()
        {
            var browsers = new List<BrowserInfo>();
            var allProcesses = Process.GetProcesses();

            foreach (var config in _browserConfigs.Values)
            {
                foreach (var processName in config.ProcessNames)
                {
                    var processes = allProcesses
                        .Where(p => p.ProcessName.ToLower().Contains(processName))
                        .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.Length > 1)
                        .ToList();

                    foreach (var process in processes)
                    {
                        try
                        {
                            var browserInfo = AnalyzeBrowserWindow(process, config);
                            if (browserInfo != null)
                            {
                                browsers.Add(browserInfo);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            return browsers;
        }

        private BrowserInfo? AnalyzeBrowserWindow(Process process, BrowserConfig config)
        {
            string windowTitle = process.MainWindowTitle;

            bool isIncognito = false;
            bool isPrivate = false;
            string windowType = "normal";

            if (config.IncognitoTitles != null && config.IncognitoTitles.Any(t => windowTitle.Contains(t)))
            {
                isIncognito = true;
                windowType = "incognito";
            }
            else if (config.PrivateTitles != null && config.PrivateTitles.Any(t => windowTitle.Contains(t)))
            {
                isPrivate = true;
                windowType = "private";
            }
            string tabTitle = windowTitle;

            if (config.NormalTitles != null)
            {
                foreach (var suffix in config.NormalTitles)
                {
                    tabTitle = tabTitle.Replace(suffix, "");
                }
            }

            if (config.IncognitoTitles != null)
            {
                foreach (var indicator in config.IncognitoTitles)
                {
                    tabTitle = tabTitle.Replace(indicator, "");
                }
            }

            if (config.PrivateTitles != null)
            {
                foreach (var indicator in config.PrivateTitles)
                {
                    tabTitle = tabTitle.Replace(indicator, "");
                }
            }

            tabTitle = tabTitle.Trim();
            if (string.IsNullOrEmpty(tabTitle))
                tabTitle = "New Tab";

            string url = ExtractUrlFromTitle(windowTitle);

            return new BrowserInfo
            {
                BrowserName = config.Name,
                TabTitle = tabTitle,
                Url = url,
                IconKey = config.IconKey,
                IsIncognito = isIncognito,
                IsPrivate = isPrivate,
                WindowType = windowType,
                LastUpdated = DateTime.Now
            };
        }

        private string ExtractUrlFromTitle(string windowTitle)
        {
            var urlPatterns = new[]
            {
                @"(https?://[^\s]+)",
                @"(www\.[^\s]+\.[^\s]+)",
                @"([^\s]+\.[a-z]{2,}/[^\s]*)"
            };

            foreach (var pattern in urlPatterns)
            {
                var match = Regex.Match(windowTitle, pattern);
                if (match.Success)
                {
                    return match.Value;
                }
            }

            return string.Empty;
        }
        public BrowserInfo? GetActiveBrowser()
        {
            var browsers = GetActiveBrowsers();
            return browsers.OrderByDescending(b => b.LastUpdated).FirstOrDefault();
        }
    }
}