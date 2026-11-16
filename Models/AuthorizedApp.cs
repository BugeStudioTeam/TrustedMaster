using System;

namespace TrustedMaster.Models
{
    public sealed class AuthorizedApp
    {
        public string Path { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string UserMode { get; set; } = string.Empty;

        public bool EnableAllPrivileges { get; set; }

        public DateTimeOffset LastRun { get; set; }

        public int RunCount { get; set; }

        public string LastRunText
        {
            get
            {
                return LastRun == default ? string.Empty : LastRun.LocalDateTime.ToString("g");
            }
        }
    }
}
