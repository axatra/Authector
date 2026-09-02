using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Authenticator
{
    public sealed partial class PasswordDialog : ContentDialog
    {
        public string Password => PasswordBox.Password;

        public PasswordDialog(string prompt = "Enter password", bool requireConfirmation = true)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            ConfirmPasswordBox.Visibility = requireConfirmation
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            PrimaryButtonClick += PasswordDialog_PrimaryButtonClick;
        }

        private void PasswordDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                args.Cancel = true;
                ShowError("Password is required.");
                return;
            }

            if (ConfirmPasswordBox.Visibility == Microsoft.UI.Xaml.Visibility.Visible &&
                PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                args.Cancel = true;
                ShowError("Passwords do not match.");
                return;
            }

            if (PasswordBox.Password.Length < 4)
            {
                args.Cancel = true;
                ShowError("Password must be at least 4 characters.");
                return;
            }

            HideError();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = "";
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}
