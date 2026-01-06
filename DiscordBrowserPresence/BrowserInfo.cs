using System;

namespace DiscordBrowserPresence
{
    public class BrowserInfo
    {
        public string? BrowserName { get; set; }
        public string? TabTitle { get; set; }
        public string? Url { get; set; }
        public string? IconKey { get; set; }
        public bool IsIncognito { get; set; }
        public bool IsPrivate { get; set; }
        public string? WindowType { get; set; }
        public DateTime LastUpdated { get; set; }

        public BrowserInfo()
        {
            BrowserName = "Unknown";
            TabTitle = "New Tab";
            IconKey = "browser";
            WindowType = "normal";
            LastUpdated = DateTime.Now;
        }

        public string GetStatusText()
        {
            if (IsIncognito || IsPrivate)
                return $"Browsing privately ({BrowserName})";

            return $"Browsing with {BrowserName}";
        }

        public string GetDetails()
        {
            if (string.IsNullOrEmpty(TabTitle) || TabTitle == "New Tab")
                return "New Tab";

            return TabTitle.Length > 128 ? TabTitle.Substring(0, 125) + "..." : TabTitle;
        }
    }
}