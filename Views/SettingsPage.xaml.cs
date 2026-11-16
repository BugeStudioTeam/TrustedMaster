using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class SettingsPage : Page
    {
        private readonly AppSettings _settings;

        public SettingsPage()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            LoadSettings();
        }

        private void LoadSettings()
        {
            DefaultUserModeComboBox.SelectedIndex = 0;

            foreach (object item in DefaultUserModeComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string tag && string.Equals(tag, _settings.DefaultUserMode, StringComparison.OrdinalIgnoreCase))
                {
                    DefaultUserModeComboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }

            EnableAllPrivilegesToggle.IsOn = _settings.DefaultEnableAllPrivileges;
            UpdateAuthorizedCount();
        }

        private void UpdateAuthorizedCount()
        {
            AuthorizedCountText.Text = AuthorizedAppStore.GetAll().Count + " authorized application(s) recorded.";
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            _settings.DefaultUserMode = DefaultUserModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag ? tag : "TrustedInstaller";
            _settings.DefaultEnableAllPrivileges = EnableAllPrivilegesToggle.IsOn;
            _settings.Save();
            StatusText.Text = "Settings saved.";
        }

        private void OnClearAuthorizedClick(object sender, RoutedEventArgs e)
        {
            AuthorizedAppStore.Clear();
            UpdateAuthorizedCount();
            StatusText.Text = "Authorized application records cleared.";
        }
    }
}
