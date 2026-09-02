using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Authenticator.Models;
using Authenticator.Services;

namespace Authenticator
{
    public sealed partial class EditAccountDialog : ContentDialog
    {
        private readonly Account _account;
        private string? _newLogoPath;

        public string Issuer => IssuerBox.Text.Trim();
        public string AccountName => NameBox.Text.Trim();
        public string Email => EmailBox.Text.Trim();
        public string? CustomLogoPath => _newLogoPath;

        public EditAccountDialog(Account account)
        {
            InitializeComponent();
            _account = account;
            IssuerBox.Text = account.Issuer;
            NameBox.Text = account.Name;
            EmailBox.Text = account.Email;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var logoPath = _newLogoPath ?? LogoService.GetLogoPath(_account);
            if (logoPath != null && File.Exists(logoPath))
            {
                PreviewImage.Source = new BitmapImage(new Uri(logoPath));
                PreviewImage.Visibility = Visibility.Visible;
                PreviewEllipse.Visibility = Visibility.Collapsed;
                PreviewInitial.Visibility = Visibility.Collapsed;
            }
            else
            {
                PreviewImage.Visibility = Visibility.Collapsed;
                PreviewEllipse.Visibility = Visibility.Visible;
                PreviewInitial.Visibility = Visibility.Visible;
                PreviewInitial.Text = _account.Initial;
                PreviewEllipse.Fill = GetBrandBrush(_account.Issuer);
            }

            var hasCustom = !string.IsNullOrEmpty(_account.CustomLogoPath) || _newLogoPath != null;
            RemoveLogoBtn.Visibility = hasCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void UploadLogo_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _newLogoPath = await LogoService.SaveCustomLogoAsync(_account.Id, file);
                UpdatePreview();
            }
        }

        private void RemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            LogoService.DeleteCustomLogo(_account.Id);
            _newLogoPath = null;
            _account.CustomLogoPath = string.Empty;
            UpdatePreview();
        }

        private static Brush GetBrandBrush(string issuer) => BrandService.GetBrandBrush(issuer);
    }
}
