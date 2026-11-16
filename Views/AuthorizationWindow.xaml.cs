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

        private readonly ElevationUser _user;

        private bool _isRunning;

        public AuthorizationWindow(string applicationPath, ElevationUser user, bool enableAllPrivileges)
        {
            InitializeComponent();
            WindowIconService.Apply(this);

            _applicationPath = applicationPath;
            _user = user;

            ApplicationNameText.Text = GetApplicationName(applicationPath);
            PublisherText.Text = "Publisher: " + AppInfoProvider.GetPublisher(applicationPath);
            OriginText.Text = "Origin: " + AppInfoProvider.GetOrigin(applicationPath);
            UserText.Text = "User: " + user.ToDisplayName();
            QuestionText.Text = "Do you want to allow this app to run with elevated privileges?";
            EnableAllPrivilegesCheckBox.IsChecked = enableAllPrivileges;

            AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 520, Height = 400 });
            Closed += OnClosed;
            _ = LoadIconAsync();
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
            RunProgressRing.IsActive = true;
            StatusInfoBar.IsOpen = false;

            bool enableAllPrivileges = EnableAllPrivilegesCheckBox.IsChecked == true;
            ElevationResult result = await Task.Run(() => _privilegeManager.Run(_applicationPath, _user, enableAllPrivileges));

            if (result.Success)
            {
                AuthorizedAppStore.Record(result.ResolvedPath, _user.ToDisplayName(), enableAllPrivileges);
                _completion.TrySetResult(true);
                Close();
                return;
            }

            _isRunning = false;
            RunProgressRing.IsActive = false;
            RunButton.IsEnabled = true;
            EnableAllPrivilegesCheckBox.IsEnabled = true;
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
