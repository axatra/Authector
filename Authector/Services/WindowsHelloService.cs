using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace Authenticator.Services
{
    public static class WindowsHelloService
    {
        public static async Task<bool> IsAvailableAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                return availability == UserConsentVerifierAvailability.Available;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RequestAuthAsync(string message = "Verify your identity to access Authenticator")
        {
            try
            {
                var result = await UserConsentVerifier.RequestVerificationAsync(message);
                return result == UserConsentVerificationResult.Verified;
            }
            catch
            {
                return false;
            }
        }
    }
}
