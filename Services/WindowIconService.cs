using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace TrustedMaster.Services
{
    internal static class WindowIconService
    {
        public static void Apply(Window window)
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "trustedmaster.ico");

                if (File.Exists(iconPath))
                {
                    window.AppWindow.SetIcon(iconPath);
                }
            }
            catch
            {
            }
        }
    }
}
