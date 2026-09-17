using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class HomePage : Page
    {
        private PrivilegeManager _privilegeManager;

        public HomePage()
        {
            InitializeComponent();
            _privilegeManager = new PrivilegeManager();
            UpdateStatus("Ready");
        }

        private async void OnAdminButtonClick(object sender, RoutedEventArgs e)
        {
            await ExecuteWithPrivilege("Administrator", "cmd.exe", "/k whoami");
        }

        private async void OnSystemButtonClick(object sender, RoutedEventArgs e)
        {
            await ExecuteWithPrivilege("SYSTEM", "cmd.exe", "/k whoami");
        }

        private async void OnTrustedInstallerButtonClick(object sender, RoutedEventArgs e)
        {
            await ExecuteWithPrivilege("TrustedInstaller", "cmd.exe", "/k whoami");
        }

        private async Task ExecuteWithPrivilege(string privilegeLevel, string command, string arguments)
        {
            UpdateStatus($"Attempting to run as {privilegeLevel}...");
            bool success = false;

            try
            {
                switch (privilegeLevel)
                {
                    case "Administrator":
                        success = await _privilegeManager.RunAsAdministrator(command, arguments);
                        break;
                    case "SYSTEM":
                        success = await _privilegeManager.RunAsSystem(command, arguments);
                        break;
                    case "TrustedInstaller":
                        success = await _privilegeManager.RunAsTrustedInstaller(command, arguments);
                        break;
                    default:
                        UpdateStatus("Invalid privilege level selected.");
                        return;
                }

                if (success)
                {
                    UpdateStatus($"Successfully executed as {privilegeLevel}.");
                }
                else
                {
                    UpdateStatus($"Failed to execute as {privilegeLevel}. Check permissions.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}