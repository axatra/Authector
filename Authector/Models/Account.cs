using System;
using System.Text.Json.Serialization;

namespace Authenticator.Models
{
    public class Account
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Issuer { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string CustomLogoPath { get; set; } = string.Empty;

        [JsonIgnore]
        public string DisplayName => string.IsNullOrEmpty(Issuer) ? Name : Issuer;

        [JsonIgnore]
        public string Initial => string.IsNullOrEmpty(Issuer)
            ? (string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper())
            : Issuer[0].ToString().ToUpper();
    }
}
