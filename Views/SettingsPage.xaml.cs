using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TrustedMaster.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }
        private void OnApplySettingsClick(object sender, RoutedEventArgs e)
        {
            var selectedLanguage = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var selectedPrivilege = (DefaultPrivilegeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var autoRunEnabled = AutoRunToggle.IsOn;

            SettingsStatusText.Text = $"Settings applied: Lang={selectedLanguage}, Priv={selectedPrivilege}, AutoRun={autoRunEnabled}";
        }
    }
}