using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class SettingsPage : Page
    {
        private readonly AppSettings _settings;

        private bool _isLoading;

        public SettingsPage()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _isLoading = true;
            DefaultUserModeComboBox.SelectedIndex = 0;
            ElevationUser defaultUser = _settings.DefaultUserMode.FromTag();

            foreach (object item in DefaultUserModeComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string tag && ElevationUserExtensions.FromTag(tag) == defaultUser)
                {
                    DefaultUserModeComboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }

            EnableAllPrivilegesToggle.IsOn = _settings.DefaultEnableAllPrivileges;
            ContextMenuToggle.IsOn = _settings.ContextMenuEnabled;
            _isLoading = false;
            UpdateAuthorizedCount();
        }

        private void OnDefaultUserModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            _settings.DefaultUserMode = DefaultUserModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag ? tag : "TrustedInstaller";
            _settings.Save();
            StatusText.Text = "Default user mode saved.";
        }

        private void OnEnableAllPrivilegesToggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            _settings.DefaultEnableAllPrivileges = EnableAllPrivilegesToggle.IsOn;
            _settings.Save();
            StatusText.Text = "Default privileges setting saved.";
        }

        private void OnContextMenuToggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            _settings.ContextMenuEnabled = ContextMenuToggle.IsOn;
            _settings.Save();

            if (ContextMenuToggle.IsOn)
            {
                _ = Task.Run(ContextMenuRegistrar.EnsureRegistered);
                StatusText.Text = "Context menu enabled.";
            }
            else
            {
                _ = Task.Run(ContextMenuRegistrar.Unregister);
                StatusText.Text = "Context menu disabled and removed from the registry.";
            }
        }

        private void UpdateAuthorizedCount()
        {
            AuthorizedCountText.Text = AuthorizedAppStore.GetAll().Count + " authorized application(s) recorded.";
        }

        private void OnClearAuthorizedClick(object sender, RoutedEventArgs e)
        {
            AuthorizedAppStore.Clear();
            UpdateAuthorizedCount();
            StatusText.Text = "Authorized application records cleared.";
        }
    }
}
