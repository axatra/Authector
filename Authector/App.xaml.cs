using Microsoft.UI.Xaml;

namespace Authenticator
{
    public partial class App : Application
    {
        internal Window? m_window;
        public static Window MainWindow => ((App)Current).m_window!;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
