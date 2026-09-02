using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Authenticator.Services
{
    public static class SecureStorageService
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Authenticator");
        private static readonly string AccountsFile = Path.Combine(AppDataPath, "accounts.enc");
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AuthenticatorDPAPI2024");

        public static async Task SaveAllAsync(List<Models.Account> accounts)
        {
            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);

            Directory.CreateDirectory(AppDataPath);
            await File.WriteAllBytesAsync(AccountsFile, protectedBytes);
        }

        public static List<Models.Account> Load()
        {
            if (!File.Exists(AccountsFile))
                return new List<Models.Account>();

            try
            {
                var protectedBytes = File.ReadAllBytes(AccountsFile);
                var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(plainBytes);
                return JsonSerializer.Deserialize<List<Models.Account>>(json)
                    ?? new List<Models.Account>();
            }
            catch
            {
                return new List<Models.Account>();
            }
        }

        public static bool HasData()
        {
            return File.Exists(AccountsFile);
        }
    }
}
