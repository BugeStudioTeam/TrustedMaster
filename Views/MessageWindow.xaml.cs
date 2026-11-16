using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TrustedMaster.Services;

namespace TrustedMaster.Views
{
    public sealed partial class MessageWindow : Window
    {
        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();

        public MessageWindow(string title, string message)
        {
            InitializeComponent();
            WindowIconService.Apply(this);

            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;

            AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 480, Height = 260 });
            Closed += OnClosed;
        }

        public Task WaitForCloseAsync()
        {
            return _completion.Task;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            _completion.TrySetResult(true);
            Close();
        }

        private void OnClosed(object sender, WindowEventArgs args)
        {
            _completion.TrySetResult(true);
        }
    }
}
