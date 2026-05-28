using SkiaSharp;
using System.Drawing.Imaging;

namespace MediaOrcestrator.Runner;

internal static class SkiaInterop
{
    public static Bitmap ToBitmap(SKBitmap skBitmap)
    {
        ArgumentNullException.ThrowIfNull(skBitmap);

        var width = skBitmap.Width;
        var height = skBitmap.Height;
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            using var pixmap = skBitmap.PeekPixels();

            if (pixmap == null || !pixmap.ReadPixels(info, data.Scan0, data.Stride))
            {
                throw new InvalidOperationException("Не удалось скопировать пиксели SKBitmap в GDI-буфер");
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }
}
