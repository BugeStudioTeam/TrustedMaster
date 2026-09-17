using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace TrustedMaster.Views
{
    public sealed partial class PermissionsPage : Page
    {
        public class AppItem
        {
            public string Name { get; set; }
            public string Status { get; set; }
        }

        public PermissionsPage()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var items = new List<AppItem>
            {
                new AppItem { Name = "TrustedMaster.Core", Status = "Authorized" },
                new AppItem { Name = "PowerShell (Admin)", Status = "Authorized" },
                new AppItem { Name = "Command Prompt", Status = "Pending" },
                new AppItem { Name = "Windows Explorer", Status = "Denied" },
            };
            AppsListView.ItemsSource = items;
        }
    }
}   