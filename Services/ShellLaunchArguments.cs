using System;

namespace TrustedMaster.Services
{
    internal static class ShellLaunchArguments
    {
        public static string? GetTargetPath(string[] arguments)
        {
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], "--run", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arguments[index], "-run", StringComparison.OrdinalIgnoreCase))
                {
                    string value = arguments[index + 1].Trim().Trim('"');
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }

            return null;
        }
    }
}
