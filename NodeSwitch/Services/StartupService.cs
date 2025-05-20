using Microsoft.Win32;
using System;
using System.Windows;

namespace NodeSwitch.Services
{
    public static class StartupService
    {
        private const string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ApplicationName = "NodeSwitch";

        public static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey);
            var value = key?.GetValue(ApplicationName);
            return value != null;
        }

        public static void SetStartupEnabled(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                if (enable)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (exePath != null)
                    {
                        key?.SetValue(ApplicationName, exePath);
                    }
                }
                else
                {
                    key?.DeleteValue(ApplicationName, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update startup settings: {ex.Message}", 
                    "Startup Settings Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }
    }
}