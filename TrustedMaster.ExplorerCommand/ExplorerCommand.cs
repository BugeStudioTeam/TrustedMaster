using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

[assembly: ComVisible(true)]

namespace TrustedMaster.ExplorerCommand
{
    [Flags]
    public enum EXPCMDFLAGS : uint
    {
        ECF_DEFAULT = 0x00000000,
        ECF_HASSUBCOMMANDS = 0x00000001,
        ECF_HASSPLITBUTTON = 0x00000002,
        ECF_HIDELABEL = 0x00000004,
        ECF_ISSEPARATOR = 0x00000008,
        ECF_HASLUASHIELD = 0x00000010,
        ECF_SEPARATORBEFORE = 0x00000020,
        ECF_SEPARATORAFTER = 0x00000040,
        ECF_ISDROPDOWN = 0x00000080,
        ECF_TOGGLEABLE = 0x00000100,
        ECF_AUTOMENUICONS = 0x00000200
    }

    public enum EXPCMDSTATE : uint
    {
        ECS_ENABLED = 0x00000000,
        ECS_DISABLED = 0x00000001,
        ECS_HIDDEN = 0x00000002,
        ECS_CHECKBOX = 0x00000004,
        ECS_CHECKED = 0x00000008,
        ECS_RADIOCHECK = 0x00000010
    }

    [ComImport]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItemArray
    {
        [PreserveSig]
        int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);

        [PreserveSig]
        int GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);

        [PreserveSig]
        int GetCount(out uint pdwNumItems);

        [PreserveSig]
        int GetItemAt(uint dwIndex, out IShellItem ppsi);

        [PreserveSig]
        int EnumItems(out IntPtr ppenumShellItems);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItem
    {
        [PreserveSig]
        int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);

        [PreserveSig]
        int GetParent(out IShellItem ppsi);

        [PreserveSig]
        int GetDisplayName(uint sigdnName, out IntPtr ppszName);

        [PreserveSig]
        int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        [PreserveSig]
        int Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport]
    [Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExplorerCommand
    {
        [PreserveSig]
        int GetTitle(IShellItemArray psiItemArray, out IntPtr ppszName);

        [PreserveSig]
        int GetIcon(IShellItemArray psiItemArray, out IntPtr ppszIcon);

        [PreserveSig]
        int GetToolTip(IShellItemArray psiItemArray, out IntPtr ppszInfotip);

        [PreserveSig]
        int GetCanonicalName(out Guid pguidCommandName);

        [PreserveSig]
        int GetState(IShellItemArray psiItemArray, [MarshalAs(UnmanagedType.Bool)] bool fOkToBeSlow, out EXPCMDSTATE pCmdState);

        [PreserveSig]
        int Invoke(IShellItemArray psiItemArray, IntPtr pbc);

        [PreserveSig]
        int GetFlags(out EXPCMDFLAGS pFlags);

        [PreserveSig]
        int EnumSubCommands(out IntPtr ppEnum);
    }

    [ComVisible(true)]
    [Guid("8B0C9A5E-3F2A-4F6B-9C1D-7A4E2D6B8C31")]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class ExplorerCommand : IExplorerCommand
    {
        private const int S_OK = 0;
        private const int E_FAIL = unchecked((int)0x80004005);
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        private const string CommandText = "Run as TrustedMaster";

        public int GetTitle(IShellItemArray psiItemArray, out IntPtr ppszName)
        {
            ppszName = Marshal.StringToCoTaskMemUni(CommandText);
            return S_OK;
        }

        public int GetIcon(IShellItemArray psiItemArray, out IntPtr ppszIcon)
        {
            string launcherPath = FindLauncherPath();
            if (string.IsNullOrEmpty(launcherPath))
            {
                ppszIcon = IntPtr.Zero;
                return E_FAIL;
            }

            ppszIcon = Marshal.StringToCoTaskMemUni(launcherPath + ",0");
            return S_OK;
        }

        public int GetToolTip(IShellItemArray psiItemArray, out IntPtr ppszInfotip)
        {
            ppszInfotip = Marshal.StringToCoTaskMemUni("Run the selected application with TrustedMaster privileges.");
            return S_OK;
        }

        public int GetCanonicalName(out Guid pguidCommandName)
        {
            pguidCommandName = Guid.Parse("8B0C9A5E-3F2A-4F6B-9C1D-7A4E2D6B8C31");
            return S_OK;
        }

        public int GetState(IShellItemArray psiItemArray, bool fOkToBeSlow, out EXPCMDSTATE pCmdState)
        {
            pCmdState = EXPCMDSTATE.ECS_ENABLED;
            return S_OK;
        }

        public int Invoke(IShellItemArray psiItemArray, IntPtr pbc)
        {
            try
            {
                string targetPath = GetFirstPath(psiItemArray);
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    return E_FAIL;
                }

                string launcherPath = FindLauncherPath();
                if (string.IsNullOrEmpty(launcherPath))
                {
                    return E_FAIL;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo(launcherPath)
                {
                    UseShellExecute = true,
                    Arguments = "--run \"" + targetPath + "\"",
                    WorkingDirectory = Path.GetDirectoryName(launcherPath)
                };

                Process.Start(startInfo);
                return S_OK;
            }
            catch
            {
                return E_FAIL;
            }
        }

        public int GetFlags(out EXPCMDFLAGS pFlags)
        {
            pFlags = EXPCMDFLAGS.ECF_DEFAULT;
            return S_OK;
        }

        public int EnumSubCommands(out IntPtr ppEnum)
        {
            ppEnum = IntPtr.Zero;
            return E_NOTIMPL;
        }

        private static string GetFirstPath(IShellItemArray itemArray)
        {
            if (itemArray is null)
            {
                return null;
            }

            uint count;
            if (itemArray.GetCount(out count) != S_OK || count == 0)
            {
                return null;
            }

            IShellItem item;
            if (itemArray.GetItemAt(0, out item) != S_OK || item is null)
            {
                return null;
            }

            IntPtr buffer;
            if (item.GetDisplayName(SIGDN_FILESYSPATH, out buffer) != S_OK || buffer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
        }

        private static string FindLauncherPath()
        {
            string directory = Path.GetDirectoryName(typeof(ExplorerCommand).Assembly.Location);
            if (string.IsNullOrEmpty(directory))
            {
                directory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string launcherPath = Path.Combine(directory, "TrustedMaster.exe");
            return File.Exists(launcherPath) ? launcherPath : null;
        }
    }
}
