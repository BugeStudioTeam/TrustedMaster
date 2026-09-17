using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;

namespace TrustedMaster.Services
{
    public class PrivilegeManager
    {
        public bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public async Task<bool> RunAsSystem(string command, string arguments)
        {
            return await RunAsAdministrator(command, arguments);
        }

        public async Task<bool> RunAsTrustedInstaller(string command, string arguments)
        {
            return await RunAsAdministrator(command, arguments);
        }

        public async Task<bool> RunAsAdministrator(string command, string arguments)
        {
            try
            {
                if (!IsAdministrator())
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        Verb = "runas",
                        UseShellExecute = true
                    };

                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        return process.ExitCode == 0;
                    }
                    return false;
                }
                else
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };

                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        return process.ExitCode == 0;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}