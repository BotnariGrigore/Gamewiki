using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using ImageMagick;

namespace GameWikiApp.Helpers;

public static class PdfExporter
{
    public static async Task ExportArticleToPdfAsync(
        string title,
        string summary,
        string content,
        string? coverImagePath,
        IEnumerable<string>? imagePaths,
        string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath is required", nameof(outputPath));

        var images = (imagePaths ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var tempFiles = new List<string>();
        var usedImageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Use the first available image (cover or first gallery image) as the single image to show at the start
        string? firstImagePath = null;
        if (!string.IsNullOrWhiteSpace(coverImagePath)) firstImagePath = coverImagePath.Trim();
        else if (images.Count > 0) firstImagePath = images[0].Trim();
        if (!string.IsNullOrWhiteSpace(firstImagePath))
        {
            var fk = NormalizeImageKey(firstImagePath);
            if (!string.IsNullOrWhiteSpace(fk)) usedImageKeys.Add(fk);
        }
        using var http = new HttpClient();

        var doc = new PdfDocument();
        doc.Info.Title = title ?? string.Empty;

        var margin = 40d;

        var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);
        var fontBody = new XFont("Arial", 11, XFontStyle.Regular);

        var page = doc.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        page.Orientation = PdfSharpCore.PageOrientation.Portrait;

        var gfx = XGraphics.FromPdfPage(page);
        var tf = new XTextFormatter(gfx);

        var y = margin;
        var usableWidth = page.Width - margin * 2;

        // Title
        var titleRect = new XRect(margin, y, usableWidth, 40);
        tf.DrawString(title ?? string.Empty, fontTitle, XBrushes.Black, titleRect, XStringFormats.TopLeft);
        y += 44;

        try
        {
            // First image (cover or first gallery) - show only once at start
            if (!string.IsNullOrWhiteSpace(firstImagePath))
            {
                var prepared = await PrepareImageForPdfAsync(firstImagePath!, tempFiles, http);
                if (!string.IsNullOrWhiteSpace(prepared) && File.Exists(prepared))
                {
                    try
                    {
                        using var ximg = XImage.FromFile(prepared);
                        var pxW = ximg.PixelWidth;
                        var pxH = ximg.PixelHeight;
                        var dpi = ximg.HorizontalResolution > 0 ? ximg.HorizontalResolution : 96.0;
                        var imgPointW = pxW * 72.0 / dpi;
                        var imgPointH = pxH * 72.0 / dpi;
                        var drawW = Math.Min(usableWidth, imgPointW);
                        var drawH = imgPointH * (drawW / imgPointW);
                        var rect = new XRect(margin, y, drawW, drawH);
                        gfx.DrawImage(ximg, rect);
                        y += drawH + 12;
                    }
                    catch
                    {
                        // ignore image errors
                    }
                }
            }

            // Summary
            if (!string.IsNullOrWhiteSpace(summary))
            {
                var summaryRect = new XRect(margin, y, usableWidth, 64);
                tf.DrawString(summary, fontBody, XBrushes.Gray, summaryRect, XStringFormats.TopLeft);
                y += 68;
            }

            // Content with simple pagination
            var remaining = content ?? string.Empty;
            while (true)
            {
                var bodyRect = new XRect(margin, y, usableWidth, page.Height - margin - y);

                var measured = MeasureStringHeight(gfx, remaining, fontBody, usableWidth);
                if (measured <= bodyRect.Height)
                {
                    tf.DrawString(remaining, fontBody, XBrushes.Black, bodyRect, XStringFormats.TopLeft);
                    y += measured;
                    break;
                }

                var fitCount = EstimateFitCharCount(gfx, remaining, fontBody, usableWidth, (int)bodyRect.Height);
                fitCount = Math.Max(1, fitCount);
                var pageText = remaining.Substring(0, Math.Min(fitCount, remaining.Length));
                var lastBreak = pageText.LastIndexOfAny(new[] { '\n', '\r', ' ' });
                if (lastBreak > 0 && lastBreak < pageText.Length - 1)
                {
                    pageText = pageText.Substring(0, lastBreak);
                }

                tf.DrawString(pageText, fontBody, XBrushes.Black, bodyRect, XStringFormats.TopLeft);
                var drawnHeight = MeasureStringHeight(gfx, pageText, fontBody, usableWidth);
                remaining = remaining.Substring(pageText.Length).TrimStart();

                // new page
                page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                gfx = XGraphics.FromPdfPage(page);
                tf = new XTextFormatter(gfx);
                y = margin;
            }

            // Gallery images are skipped — only the first image is shown at the start of the document.

            // Save document
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            using var fs = File.Create(outputPath);
            doc.Save(fs);
        }
        finally
        {
            // cleanup temp files
            foreach (var t in tempFiles)
            {
                try
                {
                    if (File.Exists(t)) File.Delete(t);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }
    }

    private static async Task<string?> PrepareImageForPdfAsync(string originalPath, List<string> tempFiles, HttpClient http)
    {
        if (string.IsNullOrWhiteSpace(originalPath)) return null;

        try
        {
            // HTTP/HTTPS
            if (originalPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                originalPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var response = await http.GetAsync(originalPath);
                if (!response.IsSuccessStatusCode) return null;
                await using var ms = new MemoryStream();
                await response.Content.CopyToAsync(ms);
                ms.Position = 0;
                try
                {
                    using var image = new MagickImage(ms);
                    var tmp = Path.Combine(Path.GetTempPath(), $"gamewiki_{Guid.NewGuid():N}.png");
                    image.Format = MagickFormat.Png;
                    image.Write(tmp);
                    tempFiles.Add(tmp);
                    return tmp;
                }
                catch
                {
                    return null;
                }
            }

            // file:// URI
            if (originalPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(originalPath);
                    originalPath = uri.LocalPath;
                }
                catch
                {
                    // ignore
                }
            }

            var full = ResolveLocalPath(originalPath);
            if (string.IsNullOrWhiteSpace(full) || !File.Exists(full)) return null;

            var ext = Path.GetExtension(full).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
            {
                return full;
            }

            // convert other formats (e.g., webp) to PNG using Magick.NET
            try
            {
                using var image = new MagickImage(full);
                var tmp = Path.Combine(Path.GetTempPath(), $"gamewiki_{Guid.NewGuid():N}.png");
                image.Format = MagickFormat.Png;
                image.Write(tmp);
                tempFiles.Add(tmp);
                return tmp;
            }
            catch
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            // If already absolute path, return it
            if (Path.IsPathRooted(path)) return path;

            // Normalize slashes and remove any leading separator
            var cleaned = path.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(AppContext.BaseDirectory, cleaned);
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeImageKey(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            var p = path.Trim();
            if (p.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || p.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return p.TrimEnd('/');
            }

            if (p.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(p);
                    return Path.GetFullPath(uri.LocalPath);
                }
                catch
                {
                    return p;
                }
            }

            if (Path.IsPathRooted(p)) return Path.GetFullPath(p);

            var resolved = ResolveLocalPath(p);
            if (!string.IsNullOrWhiteSpace(resolved)) return Path.GetFullPath(resolved);
            return p;
        }
        catch
        {
            return path;
        }
    }

    private static double MeasureStringHeight(XGraphics gfx, string text, XFont font, double width)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var size = gfx.MeasureString(text, font);
        var approxLines = Math.Ceiling(size.Width / width);
        return size.Height * Math.Max(1, approxLines);
    }

    private static int EstimateFitCharCount(XGraphics gfx, string text, XFont font, double width, int height)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sampleWidth = gfx.MeasureString(sample, font).Width;
        var avgCharWidth = sampleWidth / sample.Length;
        var charsPerLine = Math.Max(10, (int)(width / avgCharWidth));
        var lineHeight = (int)gfx.MeasureString("A", font).Height;
        var lines = Math.Max(1, height / Math.Max(1, lineHeight));
        return charsPerLine * lines;
    }
}
