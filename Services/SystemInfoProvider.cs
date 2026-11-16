using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TrustedMaster.Services
{
    public sealed class SystemInfo
    {
        public string Activation { get; set; } = "Unknown";

        public string OperatingSystem { get; set; } = "Unknown";

        public string Version { get; set; } = "Unknown";

        public string DeviceModel { get; set; } = "Unknown";

        public string Processor { get; set; } = "Unknown";

        public string Memory { get; set; } = "Unknown";

        public string Storage { get; set; } = "Unknown";
    }

    public static class SystemInfoProvider
    {
        private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        private const string BiosKey = @"HARDWARE\DESCRIPTION\System\BIOS";

        private const string ProcessorKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        [DllImport("slwga.dll", ExactSpelling = true)]
        private static extern int SLIsGenuineLocal(ref Guid applicationId, out int genuineState, IntPtr genuineInfo);

        public static SystemInfo Get()
        {
            SystemInfo info = new SystemInfo
            {
                Activation = GetActivation(),
                DeviceModel = GetDeviceModel(),
                Processor = GetProcessor(),
                Memory = GetMemory(),
                Storage = GetStorage()
            };

            PopulateOperatingSystem(info);
            return info;
        }

        private static void PopulateOperatingSystem(SystemInfo info)
        {
            string? productName = ReadValue(CurrentVersionKey, "ProductName") as string;
            string? buildNumber = ReadValue(CurrentVersionKey, "CurrentBuildNumber") as string
                ?? ReadValue(CurrentVersionKey, "CurrentBuild") as string;
            string? displayVersion = ReadValue(CurrentVersionKey, "DisplayVersion") as string
                ?? ReadValue(CurrentVersionKey, "ReleaseId") as string;

            if (!string.IsNullOrWhiteSpace(productName))
            {
                productName = productName.Trim();

                if (int.TryParse(buildNumber, out int build) &&
                    build >= 22000 &&
                    productName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
                {
                    productName = "Windows 11" + productName.Substring("Windows 10".Length);
                }

                info.OperatingSystem = productName;
            }

            string versionText = string.IsNullOrWhiteSpace(displayVersion) ? string.Empty : displayVersion.Trim();

            if (!string.IsNullOrWhiteSpace(buildNumber))
            {
                string buildText = "Build " + buildNumber.Trim();
                object? updateValue = ReadValue(CurrentVersionKey, "UBR");

                if (updateValue is not null &&
                    int.TryParse(updateValue.ToString(), out int update) &&
                    update > 0)
                {
                    buildText += "." + update;
                }

                versionText = string.IsNullOrWhiteSpace(versionText)
                    ? buildText
                    : versionText + " (" + buildText + ")";
            }

            if (!string.IsNullOrWhiteSpace(versionText))
            {
                info.Version = versionText;
            }
        }

        private static string GetActivation()
        {
            try
            {
                Guid applicationId = new Guid("55c92734-d682-4d71-983e-d6ec3f16059f");

                if (SLIsGenuineLocal(ref applicationId, out int state, IntPtr.Zero) == 0)
                {
                    switch (state)
                    {
                        case 0:
                            return "Activated";
                        case 1:
                            return "Not activated";
                        case 2:
                            return "Tampered";
                        case 3:
                            return "Offline";
                    }
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        private static string GetDeviceModel()
        {
            string? manufacturer = ReadValue(BiosKey, "SystemManufacturer") as string;
            string? model = ReadValue(BiosKey, "SystemProductName") as string;

            if (string.IsNullOrWhiteSpace(model) || IsPlaceholder(model))
            {
                manufacturer = ReadValue(BiosKey, "BaseBoardManufacturer") as string;
                model = ReadValue(BiosKey, "BaseBoardProduct") as string;
            }

            manufacturer = manufacturer?.Trim();
            model = model?.Trim();

            if (string.IsNullOrWhiteSpace(model))
            {
                return "Unknown";
            }

            if (string.IsNullOrWhiteSpace(manufacturer) ||
                model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
            {
                return model;
            }

            return manufacturer + " " + model;
        }

        private static bool IsPlaceholder(string value)
        {
            return value.Equals("System Product Name", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Default string", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("None", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetProcessor()
        {
            string? name = ReadValue(ProcessorKey, "ProcessorNameString") as string;

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            string? identifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            return string.IsNullOrWhiteSpace(identifier) ? "Unknown" : identifier.Trim();
        }

        private static string GetMemory()
        {
            try
            {
                MEMORYSTATUSEX status = new MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
                };

                if (GlobalMemoryStatusEx(ref status))
                {
                    return FormatBytes((long)status.ullTotalPhys);
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        private static string GetStorage()
        {
            try
            {
                long total = 0;
                long free = 0;

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                        {
                            total += drive.TotalSize;
                            free += drive.AvailableFreeSpace;
                        }
                    }
                    catch
                    {
                    }
                }

                if (total > 0)
                {
                    return FormatBytes(total) + " (free " + FormatBytes(free) + ")";
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int index = 0;

            while (value >= 1024 && index < units.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return value.ToString("0.#") + " " + units[index];
        }

        private static object? ReadValue(string keyPath, string valueName)
        {
            try
            {
                RegistryView view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default;

                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using RegistryKey? key = baseKey.OpenSubKey(keyPath);
                return key?.GetValue(valueName);
            }
            catch
            {
                return null;
            }
        }
    }
}
