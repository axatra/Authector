using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Authenticator.Models;
using Authenticator.Services;

namespace Authenticator
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        private readonly Window _window;
        private Microsoft.UI.Xaml.XamlRoot MainXamlRoot => (App.MainWindow.Content as FrameworkElement)!.XamlRoot;

        public event EventHandler? Done;

        public SettingsDialog(Window window)
        {
            InitializeComponent();
            _window = window;
            Width = 500;

            HelloToggle.IsOn = SettingsService.IsWindowsHelloEnabled;
            HideCodesToggle.IsOn = SettingsService.HideCodesByDefault;

            var currentStep = SettingsService.CodeStep;
            for (int i = 0; i < CodeStepCombo.Items.Count; i++)
            {
                if (CodeStepCombo.Items[i] is ComboBoxItem item && item.Tag is string tag && int.Parse(tag) == currentStep)
                {
                    CodeStepCombo.SelectedIndex = i;
                    break;
                }
            }
        }

        private void HelloToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (HelloToggle.IsLoaded)
                SettingsService.IsWindowsHelloEnabled = HelloToggle.IsOn;
        }

        private void HideCodesToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (HideCodesToggle.IsLoaded)
                SettingsService.HideCodesByDefault = HideCodesToggle.IsOn;
        }

        private void CodeStepCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CodeStepCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                SettingsService.CodeStep = int.Parse(tag);
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/axatra/Authector"));
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            MainScroll.Visibility = Visibility.Collapsed;
            DeleteConfirmPanel.Visibility = Visibility.Visible;
            Title = "Delete All Accounts?";
            PrimaryButtonText = "";
            CloseButtonText = "";
        }

        private void DeleteCancel_Click(object sender, RoutedEventArgs e)
        {
            ShowMainContent();
        }

        private async void DeleteConfirm_Click(object sender, RoutedEventArgs e)
        {
            await SecureStorageService.SaveAllAsync(new List<Account>());
            Hide();
        }

        private void ShowMainContent()
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            MainScroll.Visibility = Visibility.Visible;
            Title = "Settings";
            PrimaryButtonText = "";
            CloseButtonText = "Close";
        }

        private async void ExportBackup_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.FileTypeChoices.Add("Encrypted Backup", new List<string> { ".vault" });
            picker.SuggestedFileName = $"authenticator-backup-{DateTime.Now:yyyyMMdd}";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                Hide();
                var dialog = new PasswordDialog("Set a password for this backup");
                dialog.XamlRoot = MainXamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.Password))
                {
                    var accounts = SecureStorageService.Load();
                    await BackupService.ExportAsync(file.Path, accounts, dialog.Password);

                    await ShowInfoDialog(MainXamlRoot, "Export Complete", $"Backup saved to:\n{file.Path}");
                }

                Done?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void ImportBackup_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".vault");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Hide();
                var dialog = new PasswordDialog("Enter the backup password", requireConfirmation: false);
                dialog.XamlRoot = MainXamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.Password))
                {
                    try
                    {
                        var imported = await BackupService.ImportAsync(file.Path, dialog.Password);
                        var existing = SecureStorageService.Load();

                        int added = 0, skipped = 0;
                        foreach (var account in imported)
                        {
                            if (string.IsNullOrWhiteSpace(account.SecretKey))
                            {
                                skipped++;
                                continue;
                            }

                            try { TotpService.GenerateCode(account.SecretKey); }
                            catch
                            {
                                skipped++;
                                continue;
                            }

                            bool duplicate = existing.Any(e =>
                                e.Issuer == account.Issuer &&
                                e.Name == account.Name &&
                                e.SecretKey == account.SecretKey);

                            if (duplicate)
                                skipped++;
                            else
                            {
                                existing.Add(account);
                                added++;
                            }
                        }

                        if (added > 0)
                            await SecureStorageService.SaveAllAsync(existing);
                    }
                    catch (Exception ex)
                    {
                        await ShowInfoDialog(MainXamlRoot, "Import Failed",
                            $"Could not decrypt backup. Make sure the password is correct.\n\n{ex.Message}");
                    }
                }

                Done?.Invoke(this, EventArgs.Empty);
            }
        }

        private static async Task ShowInfoDialog(Microsoft.UI.Xaml.XamlRoot xamlRoot, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
