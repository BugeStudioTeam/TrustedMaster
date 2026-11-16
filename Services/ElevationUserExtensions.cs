using System;

namespace TrustedMaster.Services
{
    public static class ElevationUserExtensions
    {
        public static string ToDisplayName(this ElevationUser user)
        {
            switch (user)
            {
                case ElevationUser.System:
                    return "SYSTEM";
                case ElevationUser.CurrentUser:
                    return "Current User";
                case ElevationUser.CurrentProcess:
                    return "Current Process";
                default:
                    return "TrustedInstaller";
            }
        }

        public static string ToTag(this ElevationUser user)
        {
            switch (user)
            {
                case ElevationUser.System:
                    return "System";
                case ElevationUser.CurrentUser:
                    return "CurrentUser";
                case ElevationUser.CurrentProcess:
                    return "CurrentProcess";
                default:
                    return "TrustedInstaller";
            }
        }

        public static ElevationUser FromTag(this string? tag)
        {
            switch (tag)
            {
                case "System":
                case "SYSTEM":
                    return ElevationUser.System;
                case "CurrentUser":
                case "Current User":
                    return ElevationUser.CurrentUser;
                case "CurrentProcess":
                case "Current Process":
                    return ElevationUser.CurrentProcess;
                default:
                    return ElevationUser.TrustedInstaller;
            }
        }

        public static ElevationUser FromDisplayName(this string? displayName)
        {
            switch (displayName)
            {
                case "SYSTEM":
                    return ElevationUser.System;
                case "Current User":
                    return ElevationUser.CurrentUser;
                case "Current Process":
                    return ElevationUser.CurrentProcess;
                default:
                    return ElevationUser.TrustedInstaller;
            }
        }
    }
}
