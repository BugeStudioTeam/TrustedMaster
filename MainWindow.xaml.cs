using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Services;
using TrustedMaster.Views;

namespace TrustedMaster
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowIconService.Apply(this);
            AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 960, Height = 680 });
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
        }

        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                Navigate(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                switch (tag)
                {
                    case "home":
                        Navigate(typeof(HomePage));
                        break;
                    case "permissions":
                        Navigate(typeof(PermissionsPage));
                        break;
                    case "modules":
                        Navigate(typeof(ModulesPage));
                        break;
                }
            }
        }

        private void Navigate(Type pageType)
        {
            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
