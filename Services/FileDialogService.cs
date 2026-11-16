using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TrustedMaster.Services
{
    public static class FileDialogService
    {
        public static string? PickApplication(IntPtr ownerWindow)
        {
            const int bufferCharacters = 4096;
            IntPtr fileBuffer = Marshal.AllocHGlobal(bufferCharacters * sizeof(char));

            try
            {
                byte[] empty = new byte[bufferCharacters * sizeof(char)];
                Marshal.Copy(empty, 0, fileBuffer, empty.Length);

                NativeMethods.OPENFILENAME dialog = new NativeMethods.OPENFILENAME
                {
                    lStructSize = Marshal.SizeOf<NativeMethods.OPENFILENAME>(),
                    hwndOwner = ownerWindow,
                    lpstrFilter = "Applications\0*.exe;*.com;*.bat;*.cmd;*.msc;*.ps1\0All files\0*.*\0",
                    nFilterIndex = 1,
                    lpstrFile = fileBuffer,
                    nMaxFile = bufferCharacters,
                    lpstrTitle = "Select an application",
                    Flags = NativeMethods.OFN_FILEMUSTEXIST | NativeMethods.OFN_PATHMUSTEXIST | NativeMethods.OFN_EXPLORER | NativeMethods.OFN_HIDEREADONLY
                };

                if (!NativeMethods.GetOpenFileNameW(ref dialog))
                {
                    return null;
                }

                return Marshal.PtrToStringUni(fileBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
            }
        }
    }
}
