using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Authenticator.Models;
using Authenticator.Services;

namespace Authenticator
{
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<Account> _accounts = new();
        private readonly List<(Account Account, TextBlock CodeText, Grid ProgressTrack, Grid ProgressFill, Button EyeBtn, bool IsVisible, Grid Card)> _cardRefs = new();
        private readonly Dictionary<Button, bool> _cardVisibility = new();
        private DispatcherTimer? _timer;
        private bool _rebuilding;

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);

            AppWindow.TitleBar.BackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (presenter == null)
            {
                presenter = OverlappedPresenter.Create();
                AppWindow.SetPresenter(presenter);
            }
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = true;
            presenter.IsResizable = true;

            AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 700 });

            Closed += (_, _) => _timer?.Stop();
            Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
                return;

            Activated -= MainWindow_Activated;

            AccountsGridScroll.SizeChanged += (_, _) => RepositionCards();

            if (SettingsService.IsWindowsHelloEnabled)
            {
                var helloAvailable = await WindowsHelloService.IsAvailableAsync();
                if (helloAvailable && SecureStorageService.HasData())
                {
                    var authenticated = await WindowsHelloService.RequestAuthAsync("Verify your identity to view your authenticator codes");
                    if (!authenticated)
                    {
                        Close();
                        return;
                    }
                }
            }

            LoadAccounts();
            StartTimer();
        }

        private void LoadAccounts()
        {
            var accounts = SecureStorageService.Load();
            _accounts.Clear();

            foreach (var account in accounts)
                _accounts.Add(account);

            RebuildView();
            UpdateEmptyState();
            try { UpdateCodes(); } catch { }
        }

        private void RebuildView()
        {
            if (_rebuilding) return;
            _rebuilding = true;

            _cardRefs.Clear();
            _cardVisibility.Clear();
            AccountsGridPanel.Children.Clear();
            AccountsGridPanel.RowDefinitions.Clear();
            AccountsGridPanel.ColumnDefinitions.Clear();

            var availableWidth = ((FrameworkElement)Content).ActualWidth - 72;
            int cols = availableWidth > 400 ? Math.Max(1, (int)(availableWidth / 240)) : 1;
            for (int c = 0; c < cols; c++)
                AccountsGridPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int i = 0; i < _accounts.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                while (AccountsGridPanel.RowDefinitions.Count <= row)
                    AccountsGridPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var card = CreateGridCard(_accounts[i]);
                card.Margin = new Thickness(6);
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                AccountsGridPanel.Children.Add(card);
            }

            _rebuilding = false;
        }

        private void RepositionCards()
        {
            if (_cardRefs.Count == 0) return;

            AccountsGridPanel.ColumnDefinitions.Clear();
            AccountsGridPanel.RowDefinitions.Clear();

            var availableWidth = ((FrameworkElement)Content).ActualWidth - 72;
            int cols = availableWidth > 400 ? Math.Max(1, (int)(availableWidth / 240)) : 1;
            for (int c = 0; c < cols; c++)
                AccountsGridPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int i = 0; i < _cardRefs.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                while (AccountsGridPanel.RowDefinitions.Count <= row)
                    AccountsGridPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var card = _cardRefs[i].Card;
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
            }
        }

        private static (Grid track, Grid fill) CreateProgressBar()
        {
            var track = new Grid
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = (Brush)Application.Current.Resources["ControlStrongStrokeColorDefaultBrush"]
            };

            var fill = new Grid
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            };

            track.Children.Add(fill);
            return (track, fill);
        }

        private Grid CreateGridCard(Account account)
        {
            var card = new Grid
            {
                Width = 220,
                Padding = new Thickness(16, 16, 16, 10),
                CornerRadius = new CornerRadius(12),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
            };

            var stack = new StackPanel { Spacing = 0 };

            var hasSubline = !string.Equals(account.Issuer, account.Name, StringComparison.OrdinalIgnoreCase)
                             || !string.IsNullOrWhiteSpace(account.Email);

            var headerGrid = new Grid { VerticalAlignment = VerticalAlignment.Center, MinHeight = 36 };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var iconCircle = new Grid { Width = 28, Height = 28, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, hasSubline ? -7 : 0, 0, 0) };

            var logoPath = LogoService.GetLogoPath(account);
            if (logoPath != null)
            {
                var border = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Clip = new Microsoft.UI.Xaml.Media.RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 28, 28) },
                    Child = new Image
                    {
                        Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(logoPath)),
                        Width = 28,
                        Height = 28,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                    }
                };
                iconCircle.Children.Add(border);
            }
            else
            {
                iconCircle.Children.Add(new Ellipse
                {
                    Fill = GetBrandBrush(account.Issuer),
                    Opacity = 0.9
                });
                iconCircle.Children.Add(new TextBlock
                {
                    Text = account.Initial,
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            Grid.SetColumn(iconCircle, 0);
            if (hasSubline)
            {
                Grid.SetRow(iconCircle, 0);
                Grid.SetRowSpan(iconCircle, 3);
            }
            headerGrid.Children.Add(iconCircle);

            var issuerBlock = new TextBlock
            {
                Text = account.Issuer,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(issuerBlock, 2);
            Grid.SetRow(issuerBlock, hasSubline ? 0 : 0);
            headerGrid.Children.Add(issuerBlock);

            if (hasSubline)
            {
                var subText = !string.Equals(account.Issuer, account.Name, StringComparison.OrdinalIgnoreCase)
                    ? account.Name
                    : account.Email;

                var subBlock = new TextBlock
                {
                    Text = subText,
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(subBlock, 2);
                Grid.SetRow(subBlock, 1);
                headerGrid.Children.Add(subBlock);
            }

            stack.Children.Add(headerGrid);

            var codeText = new TextBlock
            {
                FontSize = 32,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontFamily = new FontFamily("Consolas"),
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                Margin = new Thickness(0, 12, 0, 0),
                CharacterSpacing = 50
            };
            stack.Children.Add(codeText);

            var (progressTrack, progressFill) = CreateProgressBar();
            progressTrack.Margin = new Thickness(0, 10, 0, 0);
            stack.Children.Add(progressTrack);

            var bottomRow = new Grid();
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bottomRow.Margin = new Thickness(0, 8, 0, 0);

            var leftButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            var copyBtn = new Button { Tag = account, Padding = new Thickness(6), MinWidth = 0, Style = (Style)Application.Current.Resources["SubtleButtonStyle"] };
            copyBtn.Content = new FontIcon { Glyph = "\uE8C8", FontSize = 14 };
            copyBtn.Click += CopyButton_Click;
            leftButtons.Children.Add(copyBtn);

            var eyeBtn = new Button { Tag = account, Padding = new Thickness(6), MinWidth = 0, Style = (Style)Application.Current.Resources["SubtleButtonStyle"] };
            var isHidden = SettingsService.HideCodesByDefault;
            _cardVisibility[eyeBtn] = !isHidden;
            eyeBtn.Content = new FontIcon { Glyph = isHidden ? "\uED1A" : "\uE890", FontSize = 14 };
            eyeBtn.Click += EyeButton_Click;
            leftButtons.Children.Add(eyeBtn);

            Grid.SetColumn(leftButtons, 0);
            bottomRow.Children.Add(leftButtons);

            var editBtn = new Button { Tag = account, Padding = new Thickness(6), MinWidth = 0, Margin = new Thickness(8, 0, 0, 0), Style = (Style)Application.Current.Resources["SubtleButtonStyle"] };
            editBtn.Content = new FontIcon { Glyph = "\uE70F", FontSize = 14 };
            editBtn.Click += EditButton_Click;
            Grid.SetColumn(editBtn, 1);
            bottomRow.Children.Add(editBtn);

            var deleteBtn = new Button { Padding = new Thickness(6), MinWidth = 0, Style = (Style)Application.Current.Resources["SubtleButtonStyle"] };
            deleteBtn.Content = new FontIcon { Glyph = "\uE74D", FontSize = 14 };
            deleteBtn.Click += DeleteGridCard_Click;
            deleteBtn.Tag = account;
            Grid.SetColumn(deleteBtn, 2);
            bottomRow.Children.Add(deleteBtn);

            stack.Children.Add(bottomRow);

            card.Children.Add(stack);

            _cardRefs.Add((account, codeText, progressTrack, progressFill, eyeBtn, !isHidden, card));

            return card;
        }

        private static Brush GetBrandBrush(string issuer) => BrandService.GetBrandBrush(issuer);

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                try { UpdateCodes(); }
                catch { }
            };
            _timer.Start();
        }

        private void UpdateCodes()
        {
            var remaining = TotpService.RemainingSeconds();
            var refs = _cardRefs.ToArray();

            foreach (var (account, codeText, progressTrack, progressFill, eyeBtn, isVisible, card) in refs)
            {
                try
                {
                    var code = TotpService.GenerateCode(account.SecretKey);
                    var maskedCode = code.Length == 6 ? "\u2022\u2022\u2022 \u2022\u2022\u2022" : "\u2022\u2022\u2022\u2022\u2022\u2022";
                    var displayCode = code.Length == 6 ? $"{code[..3]} {code[3..]}" : code;

                    if (_cardVisibility.TryGetValue(eyeBtn, out var visible) && visible)
                        codeText.Text = displayCode;
                    else
                        codeText.Text = maskedCode;

                    codeText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                }
                catch
                {
                    codeText.Text = "Invalid key";
                    codeText.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }

                try
                {
                    var w = progressTrack.ActualWidth;
                    if (w > 0)
                        progressFill.Width = w * remaining / (double)SettingsService.CodeStep;
                }
                catch { }
            }
        }

        private void EyeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var isVisible = !_cardVisibility.GetValueOrDefault(btn, !SettingsService.HideCodesByDefault);
                _cardVisibility[btn] = isVisible;

                if (btn.Content is FontIcon icon)
                    icon.Glyph = isVisible ? "\uE890" : "\uED1A";

                UpdateCodes();
            }
        }

        private void UpdateEmptyState()
        {
            EmptyState.Visibility = _accounts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AccountsGridScroll.Visibility = _accounts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }


        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddAccountDialog { XamlRoot = Content.XamlRoot };
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.NewAccount != null)
                {
                    _accounts.Add(dialog.NewAccount);
                    RebuildView();
                    await SecureStorageService.SaveAllAsync(new List<Account>(_accounts));
                    UpdateEmptyState();
                    UpdateCodes();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Failed to add account", ex.Message);
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(this) { XamlRoot = Content.XamlRoot };
            dialog.Done += (_, _) => LoadAccounts();
            await dialog.ShowAsync();
            LoadAccounts();
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Account account)
            {
                try
                {
                    var code = TotpService.GenerateCode(account.SecretKey);
                    var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    package.SetText(code);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

                    if (btn.Content is FontIcon icon)
                        icon.Glyph = "\uE73E";
                    await Task.Delay(1000);
                    if (btn.Content is FontIcon icon2)
                        icon2.Glyph = "\uE8C8";
                }
                catch { }
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Account account)
            {
                try
                {
                    var dialog = new EditAccountDialog(account) { XamlRoot = Content.XamlRoot };
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        account.Issuer = dialog.Issuer;
                        account.Name = dialog.AccountName;
                        account.Email = dialog.Email;
                        if (dialog.CustomLogoPath != null)
                            account.CustomLogoPath = dialog.CustomLogoPath;
                        await SecureStorageService.SaveAllAsync(new List<Account>(_accounts));
                        LoadAccounts();
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("Failed to save changes", ex.Message);
                }
            }
        }

        private async void DeleteGridCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Account account)
            {
                try
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Delete Account",
                        Content = $"Are you sure you want to delete \"{account.DisplayName}\"?",
                        PrimaryButtonText = "Delete",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        _accounts.Remove(account);
                        await SecureStorageService.SaveAllAsync(new List<Account>(_accounts));
                        LoadAccounts();
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("Failed to delete account", ex.Message);
                }
            }
        }

        private static async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
