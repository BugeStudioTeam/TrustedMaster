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
            LoadApplications();
        }

        private void LoadApplications()
        {
            var items = AuthorizedAppStore.GetAll();
            AuthorizedAppsListView.ItemsSource = items;
            EmptyStateText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnRevokeClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string path)
            {
                AuthorizedAppStore.Remove(path);
                LoadApplications();
            }
        }
    }
}
