using Windows.Storage;

namespace Authenticator.Services
{
    public static class SettingsService
    {
        private static readonly ApplicationDataContainer _settings =
            ApplicationData.Current.LocalSettings.CreateContainer("Settings", ApplicationDataCreateDisposition.Always);

        public static bool IsWindowsHelloEnabled
        {
            get => _settings.Values["WindowsHelloEnabled"] is true;
            set => _settings.Values["WindowsHelloEnabled"] = value;
        }

        public static bool HideCodesByDefault
        {
            get => _settings.Values["HideCodesByDefault"] is true;
            set => _settings.Values["HideCodesByDefault"] = value;
        }

        public static int CodeStep
        {
            get => _settings.Values["CodeStep"] is int v ? v : 30;
            set => _settings.Values["CodeStep"] = value;
        }
    }
}
