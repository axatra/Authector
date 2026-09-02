using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Authenticator.Services
{
    public static class ScreenCaptureService
    {
        public static async Task<string?> CaptureFromScreenAsync()
        {
            try
            {
                var overlay = new ScreenCaptureOverlay();
                overlay.Activate();

                var result = await overlay.CaptureResult;
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string?> DecodeFromClipboardAsync()
        {
            try
            {
                var clipboard = Clipboard.GetContent();
                if (clipboard.Contains(StandardDataFormats.Bitmap))
                {
                    var bitmap = await clipboard.GetBitmapAsync();
                    if (bitmap == null) return null;

                    var stream = await bitmap.OpenReadAsync();
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied);

                    var filePath = Path.Combine(Path.GetTempPath(), $"qr_clipboard_{Guid.NewGuid():N}.png");
                    await SaveBitmapToFileAsync(softwareBitmap, filePath);

                    var result = await QrCodeService.DecodeFromFileAsync(filePath);

                    try { File.Delete(filePath); } catch { }

                    return result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task SaveBitmapToFileAsync(SoftwareBitmap bitmap, string filePath)
        {
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync();

            stream.Seek(0);
            using var fileStream = File.Create(filePath);
            using var readStream = stream.AsStream();
            await readStream.CopyToAsync(fileStream);
        }
    }
}
