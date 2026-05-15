using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GameWikiApp.Helpers;

public static class ImageValidation
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".webp"
    };

    public const long DefaultMaxFileSizeBytes = 16 * 1024 * 1024;
    public const int DefaultMaxSide = 8192;

    public static bool IsSupportedImage(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return false;
        }

        var extension = Path.GetExtension(sourcePath);
        return AllowedExtensions.Contains(extension);
    }

    public static async Task<(bool Success, string? Error)> ValidateLocalImageAsync(
        string sourcePath,
        long maxFileSizeBytes = DefaultMaxFileSizeBytes,
        int maxSide = DefaultMaxSide)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return (false, "Image path is empty.");
        }

        if (!File.Exists(sourcePath))
        {
            return (false, "The selected image file could not be found.");
        }

        if (!IsSupportedImage(sourcePath))
        {
            return (false, "Unsupported image format. Use JPG, PNG, GIF, BMP, or WEBP.");
        }

        try
        {
            var fileInfo = new FileInfo(sourcePath);
            if (fileInfo.Length <= 0)
            {
                return (false, "The selected image file is empty.");
            }

            if (fileInfo.Length > maxFileSizeBytes)
            {
                return (false, "The selected image is too large.");
            }

            await using var stream = File.OpenRead(sourcePath);
            using var bitmap = new Bitmap(stream);

            if (bitmap.PixelSize.Width > maxSide || bitmap.PixelSize.Height > maxSide)
            {
                return (false, "The selected image dimensions are too large.");
            }
        }
        catch
        {
            return (false, "The selected file is not a valid image.");
        }

        return (true, null);
    }
}
