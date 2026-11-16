using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace TrustedMaster.Services
{
    public enum ElevationUser
    {
        TrustedInstaller,
        System,
        CurrentUser,
        CurrentProcess
    }

    public sealed class ElevationResult
    {
        private ElevationResult(bool success, int processId, string resolvedPath, string? errorMessage)
        {
            Success = success;
            ProcessId = processId;
            ResolvedPath = resolvedPath;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; }

        public int ProcessId { get; }

        public string ResolvedPath { get; }

        public string? ErrorMessage { get; }

        public static ElevationResult Ok(int processId, string resolvedPath)
        {
            return new ElevationResult(true, processId, resolvedPath, null);
        }

        public static ElevationResult Fail(string errorMessage)
        {
            return new ElevationResult(false, 0, string.Empty, errorMessage);
        }
    }

    public sealed class PrivilegeManager
    {
        public bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public ElevationResult Run(string applicationPath, ElevationUser user, bool enableAllPrivileges)
        {
            if (string.IsNullOrWhiteSpace(applicationPath))
            {
                return ElevationResult.Fail("No application was selected.");
            }

            string expandedPath = ResolveApplicationPath(applicationPath);
            if (!File.Exists(expandedPath))
            {
                return ElevationResult.Fail("The selected application could not be found.");
            }

            if (!IsAdministrator())
            {
                return ElevationResult.Fail("TrustedMaster requires administrator privileges.");
            }

            IntPtr currentToken = IntPtr.Zero;
            IntPtr originalToken = IntPtr.Zero;
            IntPtr primaryToken = IntPtr.Zero;
            IntPtr environment = IntPtr.Zero;
            IntPtr impersonationToken = IntPtr.Zero;
            bool impersonating = false;

            try
            {
                if (!NativeMethods.OpenProcessToken(
                    NativeMethods.GetCurrentProcess(),
                    NativeMethods.TOKEN_QUERY | NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_DUPLICATE | NativeMethods.TOKEN_ASSIGN_PRIMARY,
                    out currentToken))
                {
                    return ElevationResult.Fail(Describe("Unable to open the current process token."));
                }

                EnablePrivilege(currentToken, "SeDebugPrivilege");

                if (!TryImpersonateSystem(out impersonationToken))
                {
                    return ElevationResult.Fail("Unable to impersonate SYSTEM.");
                }

                impersonating = true;

                uint sessionId;
                if (!NativeMethods.ProcessIdToSessionId(NativeMethods.GetCurrentProcessId(), out sessionId))
                {
                    return ElevationResult.Fail(Describe("Unable to determine the current session."));
                }

                switch (user)
                {
                    case ElevationUser.TrustedInstaller:
                        originalToken = OpenTrustedInstallerToken();
                        break;
                    case ElevationUser.System:
                        originalToken = OpenSystemToken(sessionId);
                        break;
                    case ElevationUser.CurrentUser:
                        if (!NativeMethods.WTSQueryUserToken(sessionId, out originalToken))
                        {
                            originalToken = IntPtr.Zero;
                        }
                        break;
                    default:
                        if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), NativeMethods.MAXIMUM_ALLOWED, out originalToken))
                        {
                            originalToken = IntPtr.Zero;
                        }
                        break;
                }

                if (originalToken == IntPtr.Zero)
                {
                    return ElevationResult.Fail("Unable to obtain the requested user token.");
                }

                if (!NativeMethods.DuplicateTokenEx(
                    originalToken,
                    NativeMethods.MAXIMUM_ALLOWED,
                    IntPtr.Zero,
                    NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    NativeMethods.TOKEN_TYPE.TokenPrimary,
                    out primaryToken))
                {
                    return ElevationResult.Fail(Describe("Unable to duplicate the user token."));
                }

                if (!NativeMethods.SetTokenInformation(primaryToken, NativeMethods.TOKEN_INFORMATION_CLASS.TokenSessionId, ref sessionId, Marshal.SizeOf<uint>()))
                {
                    return ElevationResult.Fail(Describe("Unable to assign the user token to the current session."));
                }

                if (enableAllPrivileges && !EnableAllPrivileges(primaryToken))
                {
                    return ElevationResult.Fail("Unable to enable all privileges for the user token.");
                }

                if (!NativeMethods.CreateEnvironmentBlock(out environment, currentToken, false))
                {
                    environment = IntPtr.Zero;
                }

                string workingDirectory = Path.GetDirectoryName(expandedPath) ?? Environment.CurrentDirectory;

                NativeMethods.STARTUPINFO startupInfo = new NativeMethods.STARTUPINFO
                {
                    cb = Marshal.SizeOf<NativeMethods.STARTUPINFO>(),
                    lpDesktop = "WinSta0\\Default",
                    dwFlags = NativeMethods.STARTF_USESHOWWINDOW,
                    wShowWindow = NativeMethods.SW_SHOWNORMAL
                };

                string commandLine = "\"" + expandedPath + "\"";

                if (!NativeMethods.CreateProcessAsUserW(
                    primaryToken,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    NativeMethods.CREATE_UNICODE_ENVIRONMENT | NativeMethods.CREATE_NEW_CONSOLE,
                    environment,
                    workingDirectory,
                    ref startupInfo,
                    out NativeMethods.PROCESS_INFORMATION processInformation))
                {
                    return ElevationResult.Fail(Describe("Unable to start the application."));
                }

                int processId = unchecked((int)processInformation.dwProcessId);
                NativeMethods.CloseHandle(processInformation.hThread);
                NativeMethods.CloseHandle(processInformation.hProcess);
                return ElevationResult.Ok(processId, expandedPath);
            }
            finally
            {
                if (impersonating)
                {
                    NativeMethods.RevertToSelf();
                }

                if (environment != IntPtr.Zero)
                {
                    NativeMethods.DestroyEnvironmentBlock(environment);
                }

                if (impersonationToken != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(impersonationToken);
                }

                if (primaryToken != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(primaryToken);
                }

                if (originalToken != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(originalToken);
                }

                if (currentToken != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(currentToken);
                }
            }
        }

        private static bool TryImpersonateSystem(out IntPtr impersonationToken)
        {
            impersonationToken = IntPtr.Zero;

            uint sessionId;
            if (!NativeMethods.ProcessIdToSessionId(NativeMethods.GetCurrentProcessId(), out sessionId))
            {
                return false;
            }

            uint winLogonProcessId = FindWinLogonProcessId(sessionId);
            if (winLogonProcessId == 0)
            {
                return false;
            }

            IntPtr processHandle = NativeMethods.OpenProcess(NativeMethods.MAXIMUM_ALLOWED, false, winLogonProcessId);
            if (processHandle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.MAXIMUM_ALLOWED, out IntPtr processToken))
                {
                    return false;
                }

                try
                {
                    if (!NativeMethods.DuplicateTokenEx(
                        processToken,
                        NativeMethods.MAXIMUM_ALLOWED,
                        IntPtr.Zero,
                        NativeMethods.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        NativeMethods.TOKEN_TYPE.TokenImpersonation,
                        out impersonationToken))
                    {
                        impersonationToken = IntPtr.Zero;
                        return false;
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(processToken);
                }

                EnableAllPrivileges(impersonationToken);

                if (!NativeMethods.SetThreadToken(IntPtr.Zero, impersonationToken))
                {
                    NativeMethods.CloseHandle(impersonationToken);
                    impersonationToken = IntPtr.Zero;
                    return false;
                }

                return true;
            }
            finally
            {
                NativeMethods.CloseHandle(processHandle);
            }
        }

        private static uint FindWinLogonProcessId(uint sessionId)
        {
            if (!NativeMethods.WTSEnumerateProcessesW(NativeMethods.WTS_CURRENT_SERVER_HANDLE, 0, 1, out IntPtr buffer, out int count))
            {
                return 0;
            }

            try
            {
                int itemSize = Marshal.SizeOf<NativeMethods.WTS_PROCESS_INFO>();

                for (int index = 0; index < count; index++)
                {
                    NativeMethods.WTS_PROCESS_INFO processInfo = Marshal.PtrToStructure<NativeMethods.WTS_PROCESS_INFO>(IntPtr.Add(buffer, index * itemSize));
                    if (processInfo.SessionId != sessionId)
                    {
                        continue;
                    }

                    string? processName = processInfo.pProcessName == IntPtr.Zero ? null : Marshal.PtrToStringUni(processInfo.pProcessName);
                    if (string.Equals(processName, "winlogon.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        return processInfo.ProcessId;
                    }
                }
            }
            finally
            {
                NativeMethods.WTSFreeMemory(buffer);
            }

            return 0;
        }

        private static IntPtr OpenSystemToken(uint sessionId)
        {
            uint winLogonProcessId = FindWinLogonProcessId(sessionId);
            if (winLogonProcessId == 0)
            {
                return IntPtr.Zero;
            }

            return OpenProcessTokenForProcess(winLogonProcessId);
        }

        private static IntPtr OpenProcessTokenForProcess(uint processId)
        {
            IntPtr processHandle = NativeMethods.OpenProcess(NativeMethods.MAXIMUM_ALLOWED, false, processId);
            if (processHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                return NativeMethods.OpenProcessToken(processHandle, NativeMethods.MAXIMUM_ALLOWED, out IntPtr tokenHandle) ? tokenHandle : IntPtr.Zero;
            }
            finally
            {
                NativeMethods.CloseHandle(processHandle);
            }
        }

        private static IntPtr OpenTrustedInstallerToken()
        {
            IntPtr scmHandle = NativeMethods.OpenSCManagerW(null, null, NativeMethods.SC_MANAGER_CONNECT);
            if (scmHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                IntPtr serviceHandle = NativeMethods.OpenServiceW(scmHandle, "TrustedInstaller", NativeMethods.SERVICE_QUERY_STATUS | NativeMethods.SERVICE_START);
                if (serviceHandle == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                try
                {
                    if (!EnsureServiceRunning(serviceHandle))
                    {
                        return IntPtr.Zero;
                    }

                    if (!NativeMethods.QueryServiceStatusEx(
                        serviceHandle,
                        NativeMethods.SC_STATUS_PROCESS_INFO,
                        out NativeMethods.SERVICE_STATUS_PROCESS status,
                        Marshal.SizeOf<NativeMethods.SERVICE_STATUS_PROCESS>(),
                        out _))
                    {
                        return IntPtr.Zero;
                    }

                    if (status.dwCurrentState != NativeMethods.SERVICE_RUNNING || status.dwProcessId == 0)
                    {
                        return IntPtr.Zero;
                    }

                    return OpenProcessTokenForProcess(status.dwProcessId);
                }
                finally
                {
                    NativeMethods.CloseServiceHandle(serviceHandle);
                }
            }
            finally
            {
                NativeMethods.CloseServiceHandle(scmHandle);
            }
        }

        private static bool EnsureServiceRunning(IntPtr serviceHandle)
        {
            bool startRequested = false;
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);

            while (true)
            {
                if (!NativeMethods.QueryServiceStatusEx(
                    serviceHandle,
                    NativeMethods.SC_STATUS_PROCESS_INFO,
                    out NativeMethods.SERVICE_STATUS_PROCESS status,
                    Marshal.SizeOf<NativeMethods.SERVICE_STATUS_PROCESS>(),
                    out _))
                {
                    return false;
                }

                if (status.dwCurrentState == NativeMethods.SERVICE_STOPPED)
                {
                    if (startRequested)
                    {
                        return false;
                    }

                    if (!NativeMethods.StartServiceW(serviceHandle, 0, IntPtr.Zero) && Marshal.GetLastWin32Error() != NativeMethods.ERROR_SERVICE_ALREADY_RUNNING)
                    {
                        return false;
                    }

                    startRequested = true;
                }
                else if (status.dwCurrentState == NativeMethods.SERVICE_START_PENDING || status.dwCurrentState == NativeMethods.SERVICE_STOP_PENDING)
                {
                    if (DateTime.UtcNow > deadline)
                    {
                        return false;
                    }

                    Thread.Sleep(250);
                }
                else
                {
                    return true;
                }
            }
        }

        private static bool EnablePrivilege(IntPtr token, string privilegeName)
        {
            if (!NativeMethods.LookupPrivilegeValueW(null, privilegeName, out NativeMethods.LUID luid))
            {
                return false;
            }

            NativeMethods.TOKEN_PRIVILEGES privileges = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new NativeMethods.LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = NativeMethods.SE_PRIVILEGE_ENABLED
                }
            };

            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.TOKEN_PRIVILEGES>());
            try
            {
                Marshal.StructureToPtr(privileges, buffer, false);
                if (!NativeMethods.AdjustTokenPrivileges(token, false, buffer, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    return false;
                }

                return Marshal.GetLastWin32Error() == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool EnableAllPrivileges(IntPtr token)
        {
            NativeMethods.GetTokenInformation(token, NativeMethods.TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, 0, out int length);
            if (length == 0)
            {
                return false;
            }

            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                if (!NativeMethods.GetTokenInformation(token, NativeMethods.TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, length, out length))
                {
                    return false;
                }

                int privilegeCount = Marshal.ReadInt32(buffer);
                int itemSize = Marshal.SizeOf<NativeMethods.LUID_AND_ATTRIBUTES>();
                int attributesOffset = Marshal.OffsetOf<NativeMethods.LUID_AND_ATTRIBUTES>("Attributes").ToInt32();
                IntPtr firstItem = IntPtr.Add(buffer, sizeof(int));

                for (int index = 0; index < privilegeCount; index++)
                {
                    Marshal.WriteInt32(IntPtr.Add(firstItem, index * itemSize + attributesOffset), (int)NativeMethods.SE_PRIVILEGE_ENABLED);
                }

                if (!NativeMethods.AdjustTokenPrivileges(token, false, buffer, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    return false;
                }

                int error = Marshal.GetLastWin32Error();
                return error == 0 || error == NativeMethods.ERROR_NOT_ALL_ASSIGNED;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string ResolveApplicationPath(string applicationPath)
        {
            string expanded = Environment.ExpandEnvironmentVariables(applicationPath.Trim().Trim('"'));
            if (File.Exists(expanded))
            {
                return Path.GetFullPath(expanded);
            }

            if (Path.IsPathRooted(expanded))
            {
                return expanded;
            }

            string systemCandidate = Path.Combine(Environment.SystemDirectory, expanded);
            if (File.Exists(systemCandidate))
            {
                return systemCandidate;
            }

            string? pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrWhiteSpace(pathVariable))
            {
                foreach (string directory in pathVariable.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        continue;
                    }

                    try
                    {
                        string candidate = Path.Combine(directory.Trim().Trim('"'), expanded);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return expanded;
        }

        private static string Describe(string message)
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 0)
            {
                return message;
            }

            return message + " " + new Win32Exception(error).Message + " (0x" + error.ToString("X8") + ")";
        }
    }
}
