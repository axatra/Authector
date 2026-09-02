using System;
using OtpNet;

namespace Authenticator.Services
{
    public static class TotpService
    {
        public static string GenerateCode(string secretKey)
        {
            var bytes = Base32Encoding.ToBytes(secretKey.Replace(" ", "").ToUpperInvariant());
            var step = SettingsService.CodeStep;
            var totp = new Totp(bytes, totpSize: 6, mode: OtpHashMode.Sha1, step: step);
            return totp.ComputeTotp();
        }

        public static int RemainingSeconds()
        {
            var step = SettingsService.CodeStep;
            var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return step - (int)(epoch % step);
        }
    }
}
