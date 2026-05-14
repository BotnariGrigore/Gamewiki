using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GameWikiApp;

public static class ImageLoader
{
    private static readonly HttpClient Http = new();

    public static async Task<Bitmap?> LoadAsync(string? source)
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
                var bytes = await Http.GetByteArrayAsync(uri).ConfigureAwait(false);
                await using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }

            var path = source.Replace('/', Path.DirectorySeparatorChar);
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, path);
            }

            if (!File.Exists(path))
            {
                return null;
            }

            await using var fs = File.OpenRead(path);
            return new Bitmap(fs);
        }
        catch
        {
            return null;
        }
    }
}
