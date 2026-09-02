using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Authenticator.Services
{
    public static class QrCodeService
    {
        public static async Task<string?> DecodeFromFileAsync(string filePath)
        {
            using var image = await Image.LoadAsync<Rgba32>(filePath);

            var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>();
            reader.AutoRotate = true;
            reader.Options.TryInverted = true;

            var result = reader.Decode(image);
            return result?.Text;
        }
    }
}
