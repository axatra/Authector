using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Windowing;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Authenticator
{
    public sealed partial class ScreenCaptureOverlay : Window
    {
        private Windows.Foundation.Point _startPoint;
        private bool _isDrawing;
        private Rectangle? _selectionRect;
        private readonly TaskCompletionSource<string?> _captureResult = new();

        public Task<string?> CaptureResult => _captureResult.Task;

        public ScreenCaptureOverlay()
        {
            InitializeComponent();
            ConfigureWindow();
        }

        private void ConfigureWindow()
        {
            var presenter = OverlappedPresenter.Create();
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            AppWindow.SetPresenter(presenter);

            AppWindow.IsShownInSwitchers = false;

            var displayInfo = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayInfo.WorkArea;
            AppWindow.MoveAndResize(workArea);
        }

        private void OnOverlayBackground_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _startPoint = e.GetCurrentPoint(RootGrid).Position;
            _isDrawing = true;

            _selectionRect = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Cyan),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(ColorHelper.FromArgb(30, 0, 150, 255))
            };
            Canvas.SetLeft(_selectionRect, _startPoint.X);
            Canvas.SetTop(_selectionRect, _startPoint.Y);
            RootGrid.Children.Add(_selectionRect);
        }

        private void OnOverlayBackground_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDrawing || _selectionRect == null) return;

            var current = e.GetCurrentPoint(RootGrid).Position;
            var x = Math.Min(_startPoint.X, current.X);
            var y = Math.Min(_startPoint.Y, current.Y);
            var w = Math.Abs(current.X - _startPoint.X);
            var h = Math.Abs(current.Y - _startPoint.Y);

            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = w;
            _selectionRect.Height = h;
        }

        private async void OnOverlayBackground_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDrawing || _selectionRect == null) return;
            _isDrawing = false;

            var current = e.GetCurrentPoint(RootGrid).Position;
            var x = (int)Math.Min(_startPoint.X, current.X);
            var y = (int)Math.Min(_startPoint.Y, current.Y);
            var w = (int)Math.Abs(current.X - _startPoint.X);
            var h = (int)Math.Abs(current.Y - _startPoint.Y);

            AppWindow.Hide();

            if (w > 10 && h > 10)
            {
                await Task.Delay(100);
                var qrUri = await CaptureRegionAsync(x, y, w, h);
                _captureResult.TrySetResult(qrUri);
            }
            else
            {
                _captureResult.TrySetResult(null);
            }

            Close();
        }

        private async Task<string?> CaptureRegionAsync(int x, int y, int width, int height)
        {
            IntPtr hdcSrc = IntPtr.Zero;
            IntPtr hdcDest = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;

            try
            {
                hdcSrc = GetDC(IntPtr.Zero);
                hdcDest = CreateCompatibleDC(hdcSrc);
                hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                var hOld = SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, x, y, SRCCOPY);
                SelectObject(hdcDest, hOld);

                var pixels = GetBitmapPixels(hBitmap, width, height);

                var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"qr_region_{Guid.NewGuid():N}.png");
                await SavePixelsToFileAsync(pixels, width, height, filePath);

                var result = await Services.QrCodeService.DecodeFromFileAsync(filePath);

                try { File.Delete(filePath); } catch { }

                return result;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (hdcDest != IntPtr.Zero) DeleteDC(hdcDest);
                if (hdcSrc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hdcSrc);
            }
        }

        private static byte[] GetBitmapPixels(IntPtr hBitmap, int width, int height)
        {
            var bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.bmiHeader.biWidth = width;
            bmi.bmiHeader.biHeight = -height;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0;

            var pixels = new byte[width * height * 4];
            var hdc = CreateCompatibleDC(IntPtr.Zero);
            GetDIBits(hdc, hBitmap, 0, (uint)height, pixels, ref bmi, 0);
            DeleteDC(hdc);
            return pixels;
        }

        private static async Task SavePixelsToFileAsync(byte[] bgraPixels, int width, int height, string filePath)
        {
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)width,
                (uint)height,
                96, 96,
                bgraPixels);
            await encoder.FlushAsync();

            stream.Seek(0);
            using var fileStream = File.Create(filePath);
            using var readStream = stream.AsStream();
            await readStream.CopyToAsync(fileStream);
        }

        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height,
            IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines,
            byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
        }
    }
}
