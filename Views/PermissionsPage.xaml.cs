using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Models;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class PermissionsPage : Page
    {
        public PermissionsPage()
        {
            InitializeComponent();
            _ = LoadApplicationsAsync();
        }

        private async Task LoadApplicationsAsync()
        {
            IReadOnlyList<AuthorizedApp> applications = AuthorizedAppStore.GetAll();
            AuthorizedAppItem[] items = await Task.WhenAll(applications.Select(AuthorizedAppItem.CreateAsync));

            AuthorizedAppsListView.ItemsSource = items;
            EmptyStateText.Visibility = items.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnRevokeClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string path)
            {
                AuthorizedAppStore.Remove(path);
                _ = LoadApplicationsAsync();
            }
        }
    }
}
