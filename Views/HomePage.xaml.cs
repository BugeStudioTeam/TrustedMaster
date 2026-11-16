using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Models;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly PrivilegeManager _privilegeManager = new PrivilegeManager();

        private readonly AppSettings _settings;

        public HomePage()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            LoadAuthorizedApplications();
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            foreach (object item in UserModeComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string tag && string.Equals(tag, _settings.DefaultUserMode, StringComparison.OrdinalIgnoreCase))
                {
                    UserModeComboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }

            if (UserModeComboBox.SelectedIndex < 0)
            {
                UserModeComboBox.SelectedIndex = 0;
            }

            EnableAllPrivilegesCheckBox.IsChecked = _settings.DefaultEnableAllPrivileges;
        }

        private void LoadAuthorizedApplications()
        {
            string currentText = ApplicationComboBox.Text ?? string.Empty;
            ApplicationComboBox.Items.Clear();

            foreach (AuthorizedApp app in AuthorizedAppStore.GetAll())
            {
                if (!string.IsNullOrWhiteSpace(app.Path))
                {
                    ApplicationComboBox.Items.Add(app.Path);
                }
            }

            if (!string.IsNullOrEmpty(currentText))
            {
                ApplicationComboBox.Text = currentText;
            }
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            Window? window = App.MainWindow;
            IntPtr ownerWindow = window is null ? IntPtr.Zero : WinRT.Interop.WindowNative.GetWindowHandle(window);
            string? path = FileDialogService.PickApplication(ownerWindow);

            if (!string.IsNullOrWhiteSpace(path))
            {
                ApplicationComboBox.Text = path;
            }
        }

        private async void OnRunClick(object sender, RoutedEventArgs e)
        {
            string applicationPath = (ApplicationComboBox.Text ?? string.Empty).Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(applicationPath))
            {
                ShowStatus(InfoBarSeverity.Warning, "Select an application first.");
                return;
            }

            ElevationUser user = GetSelectedUser();
            bool enableAllPrivileges = EnableAllPrivilegesCheckBox.IsChecked == true;
            string userDisplayName = GetUserDisplayName(user);
            string fileName = Path.GetFileName(applicationPath);

            RunButton.IsEnabled = false;
            RunProgressRing.IsActive = true;
            ShowStatus(InfoBarSeverity.Informational, "Launching " + fileName + " as " + userDisplayName + "...");

            ElevationResult result = await Task.Run(() => _privilegeManager.Run(applicationPath, user, enableAllPrivileges));

            RunProgressRing.IsActive = false;
            RunButton.IsEnabled = true;

            if (!result.Success)
            {
                ShowStatus(InfoBarSeverity.Error, result.ErrorMessage ?? "Unable to launch the application.");
                return;
            }

            AuthorizedAppStore.Record(result.ResolvedPath, userDisplayName, enableAllPrivileges);
            LoadAuthorizedApplications();
            ApplicationComboBox.Text = result.ResolvedPath;
            ShowStatus(InfoBarSeverity.Success, fileName + " started as " + userDisplayName + ".");
        }

        private ElevationUser GetSelectedUser()
        {
            if (UserModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                switch (tag)
                {
                    case "System":
                        return ElevationUser.System;
                    case "CurrentUser":
                        return ElevationUser.CurrentUser;
                    case "CurrentProcess":
                        return ElevationUser.CurrentProcess;
                }
            }

            return ElevationUser.TrustedInstaller;
        }

        private static string GetUserDisplayName(ElevationUser user)
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

        private void ShowStatus(InfoBarSeverity severity, string message)
        {
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }
    }
}
