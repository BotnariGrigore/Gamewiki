using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;

namespace GameWikiApp.Helpers
{
    public static class MediaHelper
    {
        public static string BuildPlaceholderLabel(string? source, string fallback = "GAME", int maxWords = 2, int maxLength = 14)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return fallback;
            }

            var parts = source
                .Split(new[] { ' ', '-', '_', '/', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Take(Math.Max(1, maxWords))
                .ToArray();

            var label = string.Join(" ", parts);
            if (label.Length > maxLength)
            {
                label = label[..maxLength].TrimEnd();
            }

            return string.IsNullOrWhiteSpace(label) ? fallback : label;
        }

        public static Image? LoadImage(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            try
            {
                if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    using var client = new WebClient();
                    var data = client.DownloadData(uri);
                    using var ms = new MemoryStream(data);
                    using var image = Image.FromStream(ms);
                    return new Bitmap(image);
                }

                var path = source.Replace('/', Path.DirectorySeparatorChar);
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                }

                if (!File.Exists(path))
                {
                    return null;
                }

                using var fs = File.OpenRead(path);
                using var local = Image.FromStream(fs);
                return new Bitmap(local);
            }
            catch
            {
                return null;
            }
        }

        public static Image CreatePlaceholder(int width, int height, string text)
        {
            var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var brush = new LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                ThemeHelper.BgTertiary,
                ThemeHelper.BgSecondary,
                LinearGradientMode.Vertical);
            g.FillRectangle(brush, 0, 0, width, height);

            using var font = new Font("Segoe UI", Math.Max(14, height / 5f), FontStyle.Bold);
            using var textBrush = new SolidBrush(ThemeHelper.TextSecondary);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            var rect = new RectangleF(0, 0, width, height);
            g.DrawString(text, font, textBrush, rect, format);
            return bmp;
        }
    }
}
