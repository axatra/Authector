using System;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using Authenticator.Models;
using Authenticator.Services;

namespace Authenticator
{
    public sealed partial class AddAccountDialog : ContentDialog
    {
        public Account? NewAccount { get; private set; }

        public AddAccountDialog()
        {
            InitializeComponent();
            PrimaryButtonClick += AddAccountDialog_PrimaryButtonClick;
        }

        private void AddAccountDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var name = AccountNameBox.Text.Trim();
            var key = SecretKeyBox.Text.Replace(" ", "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Account Name is required.");
                args.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(key) || !Regex.IsMatch(key, @"^[A-Z2-7]+=*$", RegexOptions.IgnoreCase))
            {
                ShowError("Secret key must be valid Base32 (A-Z, 2-7).");
                args.Cancel = true;
                return;
            }

            try { TotpService.GenerateCode(key); }
            catch
            {
                ShowError("Secret key is invalid. Could not generate a TOTP code from it.");
                args.Cancel = true;
                return;
            }

            var email = EmailBox.Text.Trim();

            NewAccount = new Account
            {
                Issuer = name,
                Name = name,
                Email = email,
                SecretKey = key
            };
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }
}
