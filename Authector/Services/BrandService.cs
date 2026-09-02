using Microsoft.UI.Xaml.Media;

namespace Authenticator.Services
{
    public static class BrandService
    {
        public static Brush GetBrandBrush(string issuer)
        {
            var normalized = issuer.ToLowerInvariant().Replace(" ", "");
            return normalized switch
            {
                "x" or "twitter" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 29, 161, 242)),
                var s when s.Contains("google") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 66, 133, 244)),
                var s when s.Contains("github") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 36, 41, 46)),
                var s when s.Contains("microsoft") || s.Contains("outlook") || s.Contains("live") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 212)),
                var s when s.Contains("discord") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 88, 101, 242)),
                var s when s.Contains("slack") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 74, 21, 75)),
                var s when s.Contains("steam") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 56, 75)),
                var s when s.Contains("apple") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 142, 142, 147)),
                var s when s.Contains("facebook") || s.Contains("meta") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 24, 119, 242)),
                var s when s.Contains("amazon") => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 153, 0)),
                _ => (Brush)Microsoft.UI.Xaml.Application.Current.Resources["AccentFillColorDefaultBrush"]
            };
        }
    }
}
