using System;
using System.Collections.Generic;
using System.IO;

namespace DiscordBrowserPresence
{
    public class IconManager
    {
        private readonly string _iconsFolder;

        public IconManager()
        {
            _iconsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
        }

        public string? GetIconPath(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
                return null;

            string iconPath = Path.Combine(_iconsFolder, $"{iconKey.ToLower()}.png");
            return File.Exists(iconPath) ? iconPath : null;
        }

        public bool IconExists(string iconKey)
        {
            string iconPath = Path.Combine(_iconsFolder, $"{iconKey.ToLower()}.png");
            return File.Exists(iconPath);
        }

        public List<string> GetAvailableIcons()
        {
            var icons = new List<string>();

            if (!Directory.Exists(_iconsFolder))
                return icons;

            var files = Directory.GetFiles(_iconsFolder, "*.png");
            foreach (var file in files)
            {
                icons.Add(Path.GetFileNameWithoutExtension(file));
            }

            return icons;
        }

        public string? GetFallbackIcon()
        {
            var fallbackIcons = new[] { "chrome", "firefox", "edge", "brave" };

            foreach (var icon in fallbackIcons)
            {
                if (IconExists(icon))
                    return icon;
            }

            return null;
        }
    }
}