using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using TrustedMaster.Models;

namespace TrustedMaster.Services
{
    public static class AuthorizedAppStore
    {
        private static readonly object SyncRoot = new object();

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        public static IReadOnlyList<AuthorizedApp> GetAll()
        {
            lock (SyncRoot)
            {
                return Read().OrderByDescending(item => item.LastRun).ToList();
            }
        }

        public static void Record(string applicationPath, string userMode, bool enableAllPrivileges)
        {
            lock (SyncRoot)
            {
                List<AuthorizedApp> items = Read();
                AuthorizedApp? existing = items.FirstOrDefault(item => string.Equals(item.Path, applicationPath, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    existing = new AuthorizedApp();
                    items.Add(existing);
                }

                existing.Path = applicationPath;
                existing.Name = GetDisplayName(applicationPath);
                existing.UserMode = userMode;
                existing.EnableAllPrivileges = enableAllPrivileges;
                existing.LastRun = DateTimeOffset.Now;
                existing.RunCount += 1;

                Write(items);
            }
        }

        public static void Remove(string applicationPath)
        {
            lock (SyncRoot)
            {
                List<AuthorizedApp> items = Read();
                items.RemoveAll(item => string.Equals(item.Path, applicationPath, StringComparison.OrdinalIgnoreCase));
                Write(items);
            }
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                Write(new List<AuthorizedApp>());
            }
        }

        private static List<AuthorizedApp> Read()
        {
            try
            {
                string file = StoragePaths.AuthorizedAppsFile;
                if (!File.Exists(file))
                {
                    return new List<AuthorizedApp>();
                }

                return JsonSerializer.Deserialize<List<AuthorizedApp>>(File.ReadAllText(file)) ?? new List<AuthorizedApp>();
            }
            catch
            {
                return new List<AuthorizedApp>();
            }
        }

        private static void Write(List<AuthorizedApp> items)
        {
            try
            {
                File.WriteAllText(StoragePaths.AuthorizedAppsFile, JsonSerializer.Serialize(items, SerializerOptions));
            }
            catch
            {
            }
        }

        private static string GetDisplayName(string applicationPath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(applicationPath);
                string? name = string.IsNullOrWhiteSpace(versionInfo.FileDescription) ? versionInfo.ProductName : versionInfo.FileDescription;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Path.GetFileNameWithoutExtension(applicationPath);
                }

                return name!;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(applicationPath);
            }
        }
    }
}
