using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using TrustedMaster.Services;

namespace TrustedMaster.Models
{
    public sealed class AuthorizedAppItem
    {
        public string Path { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string UserMode { get; set; } = string.Empty;

        public string LastRunText { get; set; } = string.Empty;

        public ImageSource? Icon { get; set; }

        public static async Task<AuthorizedAppItem> CreateAsync(AuthorizedApp app)
        {
            return new AuthorizedAppItem
            {
                Path = app.Path,
                Name = app.Name,
                UserMode = app.UserMode,
                LastRunText = app.LastRunText,
                Icon = await FileIconProvider.GetIconAsync(app.Path, 32)
            };
        }
    }
}
