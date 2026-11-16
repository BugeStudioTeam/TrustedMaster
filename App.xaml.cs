using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using TrustedMaster.Models;
using TrustedMaster.Services;
using TrustedMaster.Views;

namespace TrustedMaster
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            AppSettings startupSettings = AppSettings.Load();

            if (startupSettings.ContextMenuEnabled)
            {
                _ = Task.Run(ContextMenuRegistrar.EnsureRegistered);
            }
            else
            {
                _ = Task.Run(ContextMenuRegistrar.Unregister);
            }

            string? shellTargetPath = ShellLaunchArguments.GetTargetPath(Environment.GetCommandLineArgs());
            if (shellTargetPath is null)
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
                return;
            }

            try
            {
                await RunFromShellAsync(shellTargetPath);
            }
            catch
            {
                Exit();
            }
        }

        private async Task RunFromShellAsync(string targetPath)
        {
            string expandedPath;
            try
            {
                expandedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(targetPath));
                expandedPath = ShortcutResolver.Resolve(expandedPath);
            }
            catch
            {
                await ShowMessageAsync("The selected path is not valid.");
                Exit();
                return;
            }

            if (!File.Exists(expandedPath))
            {
                await ShowMessageAsync("The selected application could not be found.");
                Exit();
                return;
            }

            AuthorizedApp? authorizedApp = AuthorizedAppStore.Find(expandedPath);
            if (authorizedApp is not null)
            {
                ElevationResult result = await Task.Run(() => new PrivilegeManager().Run(expandedPath, authorizedApp.UserMode.FromTag(), authorizedApp.EnableAllPrivileges));

                if (result.Success)
                {
                    AuthorizedAppStore.Record(result.ResolvedPath, authorizedApp.UserMode, authorizedApp.EnableAllPrivileges);
                    Exit();
                    return;
                }

                await ShowMessageAsync(result.ErrorMessage ?? "Unable to launch the application.");
                Exit();
                return;
            }

            AppSettings settings = AppSettings.Load();
            AuthorizationWindow window = new AuthorizationWindow(expandedPath, settings.DefaultUserMode.FromTag(), settings.DefaultEnableAllPrivileges);
            window.Activate();
            await window.WaitForLaunchAsync();
            Exit();
        }

        private static async Task ShowMessageAsync(string message)
        {
            MessageWindow window = new MessageWindow("Run as TrustedMaster", message);
            window.Activate();
            await window.WaitForCloseAsync();
        }
    }
}
