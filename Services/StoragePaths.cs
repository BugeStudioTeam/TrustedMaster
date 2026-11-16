using System;
using System.IO;

namespace TrustedMaster.Services
{
    internal static class StoragePaths
    {
        public static string RootDirectory
        {
            get
            {
                string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrustedMaster");
                Directory.CreateDirectory(root);
                return root;
            }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(RootDirectory, "settings.json"); }
        }

        public static string AuthorizedAppsFile
        {
            get { return Path.Combine(RootDirectory, "authorized-apps.json"); }
        }
    }
}
