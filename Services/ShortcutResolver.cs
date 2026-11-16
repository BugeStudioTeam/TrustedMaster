using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace TrustedMaster.Services
{
    internal static class ShortcutResolver
    {
        public static string Resolve(string applicationPath)
        {
            if (!string.Equals(Path.GetExtension(applicationPath), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return applicationPath;
            }

            try
            {
                ShellLink shellLink = new ShellLink();
                ((IPersistFile)shellLink).Load(applicationPath, 0);

                StringBuilder target = new StringBuilder(32768);
                ((IShellLinkW)shellLink).GetPath(target, target.Capacity, IntPtr.Zero, 0);

                string resolved = Environment.ExpandEnvironmentVariables(target.ToString());
                return File.Exists(resolved) ? Path.GetFullPath(resolved) : applicationPath;
            }
            catch
            {
                return applicationPath;
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        }
    }
}
