using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Authenticator.Services
{
    public static class LogoService
    {
        private static readonly string LogosDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Authenticator", "Logos");

        private static readonly HashSet<string> BuiltInLogos = new(StringComparer.OrdinalIgnoreCase)
        {
            "google", "microsoft", "github", "discord", "instagram",
            "twitter", "x", "facebook", "meta", "amazon", "apple",
            "steam", "snapchat", "twitch", "reddit", "linkedin",
            "netflix", "spotify", "paypal", "dropbox", "zoom",
            "telegram", "whatsapp", "slack", "notion", "cloudflare",
            "gitlab", "bitbucket", "docker", "figma", "adobe"
        };

        public static string? GetLogoPath(Models.Account account)
        {
            if (!string.IsNullOrEmpty(account.CustomLogoPath) && File.Exists(account.CustomLogoPath))
                return account.CustomLogoPath;

            var issuer = account.Issuer?.ToLowerInvariant().Replace(" ", "") ?? "";
            if (BuiltInLogos.Contains(issuer))
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", $"{issuer}.png");
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        public static async Task<string?> SaveCustomLogoAsync(string accountId, StorageFile file)
        {
            Directory.CreateDirectory(LogosDir);
            var destPath = Path.Combine(LogosDir, $"{accountId}.png");
            await file.CopyAsync(
                await StorageFolder.GetFolderFromPathAsync(LogosDir),
                $"{accountId}.png",
                NameCollisionOption.ReplaceExisting);
            return destPath;
        }

        public static void DeleteCustomLogo(string accountId)
        {
            var path = Path.Combine(LogosDir, $"{accountId}.png");
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
