using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class AuthorizationWindow : Window
    {
        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();

        private readonly PrivilegeManager _privilegeManager = new PrivilegeManager();

        private readonly string _applicationPath;

        private bool _isRunning;

        public AuthorizationWindow(string applicationPath, ElevationUser user, bool enableAllPrivileges)
        {
            InitializeComponent();
            WindowIconService.Apply(this);

            _applicationPath = applicationPath;

            ApplicationNameText.Text = GetApplicationName(applicationPath);
            PublisherText.Text = "Publisher: " + AppInfoProvider.GetPublisher(applicationPath);
            OriginText.Text = "Origin: " + AppInfoProvider.GetOrigin(applicationPath);
            QuestionText.Text = "Do you want to allow this app to run with elevated privileges?";
            EnableAllPrivilegesCheckBox.IsChecked = enableAllPrivileges;

            ApplyUserSelection(user);

            AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 520, Height = 420 });
            Closed += OnClosed;
            _ = LoadIconAsync();
        }

        private void ApplyUserSelection(ElevationUser user)
        {
            UserModeComboBox.SelectedIndex = 0;

            foreach (object item in UserModeComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string tag && ElevationUserExtensions.FromTag(tag) == user)
                {
                    UserModeComboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }

            if (UserModeComboBox.SelectedIndex < 0)
            {
                UserModeComboBox.SelectedIndex = 0;
            }
        }

        private ElevationUser GetSelectedUser()
        {
            if (UserModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                return ElevationUserExtensions.FromTag(tag);
            }

            return ElevationUser.TrustedInstaller;
        }

        private async Task LoadIconAsync()
        {
            Microsoft.UI.Xaml.Media.ImageSource? icon = await FileIconProvider.GetIconAsync(_applicationPath, 32);

            if (icon is not null)
            {
                ApplicationIcon.Source = icon;
                ApplicationIcon.Visibility = Visibility.Visible;
                FallbackIcon.Visibility = Visibility.Collapsed;
            }
        }

        public Task<bool> WaitForLaunchAsync()
        {
            return _completion.Task;
        }

        private async void OnRunClick(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            RunButton.IsEnabled = false;
            EnableAllPrivilegesCheckBox.IsEnabled = false;
            UserModeComboBox.IsEnabled = false;
            RunProgressRing.IsActive = true;
            StatusInfoBar.IsOpen = false;

            ElevationUser user = GetSelectedUser();
            bool enableAllPrivileges = EnableAllPrivilegesCheckBox.IsChecked == true;
            ElevationResult result = await Task.Run(() => _privilegeManager.Run(_applicationPath, user, enableAllPrivileges));

            if (result.Success)
            {
                AuthorizedAppStore.Record(result.ResolvedPath, user.ToDisplayName(), enableAllPrivileges);
                _completion.TrySetResult(true);
                Close();
                return;
            }

            _isRunning = false;
            RunProgressRing.IsActive = false;
            RunButton.IsEnabled = true;
            EnableAllPrivilegesCheckBox.IsEnabled = true;
            UserModeComboBox.IsEnabled = true;
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Message = result.ErrorMessage ?? "Unable to launch the application.";
            StatusInfoBar.IsOpen = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            _completion.TrySetResult(false);
            Close();
        }

        private void OnClosed(object sender, WindowEventArgs args)
        {
            _completion.TrySetResult(false);
        }

        private static string GetApplicationName(string applicationPath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(applicationPath);
                string? name = string.IsNullOrWhiteSpace(versionInfo.FileDescription) ? versionInfo.ProductName : versionInfo.FileDescription;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name!;
                }
            }
            catch
            {
            }

            return Path.GetFileNameWithoutExtension(applicationPath);
        }
    }
}
