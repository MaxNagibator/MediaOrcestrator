using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MediaOrcestrator.VkVideo;

internal static class ThumbnailCropper
{
    private const double TargetAspectRatio = 9.0 / 16.0;
    private const double AspectTolerance = 0.01;

    public static string CropToShortsIfNeeded(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            return sourcePath;
        }

        using var image = Image.Load(sourcePath);

        var imageHeight = image.Height;
        var imageWidth = image.Width;
        var sourceAspect = (double)imageWidth / imageHeight;
        if (sourceAspect <= TargetAspectRatio + AspectTolerance)
        {
            return sourcePath;
        }

        var targetWidth = (int)Math.Round(imageHeight * TargetAspectRatio);
        if (targetWidth <= 0 || targetWidth >= imageWidth)
        {
            return sourcePath;
        }

        var x = (imageWidth - targetWidth) / 2;
        image.Mutate(ctx => ctx.Crop(new(x, 0, targetWidth, imageHeight)));

        var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
        var croppedPath = Path.Combine(directory, $"{nameWithoutExt}.shorts.jpg");

        var encoder = new JpegEncoder { Quality = 100 };
        image.Save(croppedPath, encoder);

        return croppedPath;
    }
}
