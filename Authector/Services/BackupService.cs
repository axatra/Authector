using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Authenticator.Services
{
    public static class BackupService
    {
        public static async Task ExportAsync(string filePath, List<Models.Account> accounts, string password)
        {
            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            var salt = new byte[32];
            RandomNumberGenerator.Fill(salt);
            var key = DeriveKey(password, salt);

            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            byte[] ciphertext = new byte[jsonBytes.Length];
            byte[] tag = new byte[16];

            using (var aes = new AesGcm(key, 16))
            {
                aes.Encrypt(nonce, jsonBytes, ciphertext, tag);
            }

            var exportData = new
            {
                Version = 1,
                Algorithm = "AES-256-GCM",
                Salt = Convert.ToBase64String(salt),
                Nonce = Convert.ToBase64String(nonce),
                AuthTag = Convert.ToBase64String(tag),
                Data = Convert.ToBase64String(ciphertext)
            };

            var exportJson = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, exportJson);
        }

        public static async Task<List<Models.Account>> ImportAsync(string filePath, string password)
        {
            var exportJson = await File.ReadAllTextAsync(filePath);
            var exportData = JsonSerializer.Deserialize<ExportData>(exportJson);

            if (exportData == null)
                throw new InvalidOperationException("Invalid backup file format.");

            var salt = Convert.FromBase64String(exportData.Salt);
            var nonce = Convert.FromBase64String(exportData.Nonce);
            var tag = Convert.FromBase64String(exportData.AuthTag);
            var ciphertext = Convert.FromBase64String(exportData.Data);

            var key = DeriveKey(password, salt);

            byte[] plaintext = new byte[ciphertext.Length];
            using (var aes = new AesGcm(key, 16))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            var json = Encoding.UTF8.GetString(plaintext);
            return JsonSerializer.Deserialize<List<Models.Account>>(json)
                ?? new List<Models.Account>();
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32);
        }

        private class ExportData
        {
            public int Version { get; set; }
            public string Algorithm { get; set; } = "";
            public string Salt { get; set; } = "";
            public string Nonce { get; set; } = "";
            public string AuthTag { get; set; } = "";
            public string Data { get; set; } = "";
        }
    }
}
