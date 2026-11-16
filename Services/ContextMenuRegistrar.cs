using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TrustedMaster.Services
{
    internal static class ContextMenuRegistrar
    {
        private const string CommandClsid = "{8B0C9A5E-3F2A-4F6B-9C1D-7A4E2D6B8C31}";

        private const string VerbLabel = "Run as TrustedMaster";

        private const int SHCNE_ASSOCCHANGED = 0x08000000;

        private const uint SHCNF_IDLIST = 0x0000;

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

        public static void EnsureRegistered()
        {
            try
            {
                string executablePath = Path.Combine(AppContext.BaseDirectory, "TrustedMaster.exe");
                string handlerPath = Path.Combine(AppContext.BaseDirectory, "TrustedMaster.ExplorerCommand.comhost.dll");

                if (!File.Exists(executablePath))
                {
                    return;
                }

                bool hasCommandHandler = File.Exists(handlerPath) &&
                    Environment.Is64BitProcess == Environment.Is64BitOperatingSystem &&
                    HasDotNetRuntime();

                if (hasCommandHandler)
                {
                    using (RegistryKey? classKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\" + CommandClsid + @"\InprocServer32"))
                    {
                        if (classKey is null)
                        {
                            return;
                        }

                        classKey.SetValue(null, handlerPath, RegistryValueKind.String);
                        classKey.SetValue("ThreadingModel", "Both", RegistryValueKind.String);
                    }
                }

                using (RegistryKey? verbKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\*\shell\TrustedMaster"))
                {
                    if (verbKey is null)
                    {
                        return;
                    }

                    verbKey.SetValue("MUIVerb", VerbLabel, RegistryValueKind.String);
                    verbKey.SetValue("Icon", "\"" + executablePath + "\",0", RegistryValueKind.String);
                    verbKey.SetValue("MultiSelectModel", "Single", RegistryValueKind.String);

                    if (hasCommandHandler)
                    {
                        verbKey.SetValue("ExplorerCommandHandler", CommandClsid, RegistryValueKind.String);
                    }
                    else
                    {
                        verbKey.DeleteValue("ExplorerCommandHandler", false);
                    }

                    using (RegistryKey commandKey = verbKey.CreateSubKey("command"))
                    {
                        commandKey.SetValue(null, "\"" + executablePath + "\" --run \"%1\"", RegistryValueKind.String);
                    }
                }

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
            }
        }

        public static void Unregister()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\*\shell\TrustedMaster", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\" + CommandClsid, false);
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
            }
        }

        private static bool HasDotNetRuntime()
        {
            string[] roots =
            {
                Environment.GetEnvironmentVariable("DOTNET_ROOT"),
                Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet")
            };

            foreach (string? root in roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                try
                {
                    string sharedDirectory = Path.Combine(root, "shared", "Microsoft.NETCore.App");

                    if (!Directory.Exists(sharedDirectory))
                    {
                        continue;
                    }

                    foreach (string versionDirectory in Directory.GetDirectories(sharedDirectory))
                    {
                        if (Path.GetFileName(versionDirectory).StartsWith("8.", StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            return false;
        }
    }
}
