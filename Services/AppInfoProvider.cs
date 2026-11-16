using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace TrustedMaster.Services
{
    public static class AppInfoProvider
    {
        public static string GetPublisher(string applicationPath)
        {
            try
            {
                X509Certificate certificate = X509Certificate.CreateFromSignedFile(applicationPath);
                string commonName = certificate.Subject
                    .Split(',')
                    .Select(part => part.Trim())
                    .Where(part => part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                    .Select(part => part.Substring(3))
                    .FirstOrDefault() ?? string.Empty;

                return string.IsNullOrWhiteSpace(commonName) ? "Unknown publisher" : commonName;
            }
            catch
            {
                return "Unknown publisher";
            }
        }

        public static string GetOrigin(string applicationPath)
        {
            try
            {
                if (applicationPath.StartsWith("\\\\", StringComparison.Ordinal))
                {
                    return "Network";
                }

                string root = Path.GetPathRoot(applicationPath) ?? string.Empty;
                if (string.IsNullOrEmpty(root))
                {
                    return "Hard drive on this computer";
                }

                DriveInfo drive = new DriveInfo(root);
                switch (drive.DriveType)
                {
                    case DriveType.Removable:
                        return "Removable drive";
                    case DriveType.Network:
                        return "Network";
                    case DriveType.CDRom:
                        return "CD drive";
                    default:
                        return "Hard drive on this computer";
                }
            }
            catch
            {
                return "Hard drive on this computer";
            }
        }
    }
}
