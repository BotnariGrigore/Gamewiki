using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using GameWikiApp.Helpers;

namespace GameWikiApp;

public static class ImageLoader
{
    private const int MaxCacheEntries = 96;
    private const long MaxDownloadBytes = ImageValidation.DefaultMaxFileSizeBytes;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private static readonly object Gate = new();
    private static readonly LinkedList<string> Order = new();
    private static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<Bitmap?>>> InFlight = new(StringComparer.OrdinalIgnoreCase);

    private sealed class CacheEntry
    {
        public required Bitmap Bitmap { get; init; }
        public required LinkedListNode<string> Node { get; init; }
    }

    public static Task<Bitmap?> LoadAsync(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Task.FromResult<Bitmap?>(null);
        }

        if (!TryGetCacheKey(source, out var cacheKey, out var normalizedSource))
        {
            return Task.FromResult<Bitmap?>(null);
        }

        if (TryGetCached(cacheKey, out var cached))
        {
            return Task.FromResult<Bitmap?>(cached);
        }

        var loader = InFlight.GetOrAdd(cacheKey, _ => new Lazy<Task<Bitmap?>>(
            () => LoadAndCacheAsync(normalizedSource, cacheKey),
            LazyThreadSafetyMode.ExecutionAndPublication));

        return AwaitAndCleanupAsync(cacheKey, loader);
    }

    private static async Task<Bitmap?> AwaitAndCleanupAsync(string cacheKey, Lazy<Task<Bitmap?>> loader)
    {
        try
        {
            return await loader.Value.ConfigureAwait(false);
        }
        finally
        {
            InFlight.TryRemove(cacheKey, out _);
        }
    }

    private static bool TryGetCached(string cacheKey, out Bitmap? bitmap)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(cacheKey, out var entry))
            {
                Order.Remove(entry.Node);
                Order.AddFirst(entry.Node);
                bitmap = entry.Bitmap;
                return true;
            }
        }

        bitmap = null;
        return false;
    }

    private static async Task<Bitmap?> LoadAndCacheAsync(string source, string cacheKey)
    {
        Bitmap? bitmap = await LoadUncachedAsync(source).ConfigureAwait(false);
        if (bitmap == null)
        {
            return null;
        }

        if (bitmap.PixelSize.Width > ImageValidation.DefaultMaxSide ||
            bitmap.PixelSize.Height > ImageValidation.DefaultMaxSide)
        {
            bitmap.Dispose();
            return null;
        }

        lock (Gate)
        {
            if (Cache.TryGetValue(cacheKey, out var existing))
            {
                bitmap.Dispose();
                Order.Remove(existing.Node);
                Order.AddFirst(existing.Node);
                return existing.Bitmap;
            }

            var node = new LinkedListNode<string>(cacheKey);
            Order.AddFirst(node);
            Cache[cacheKey] = new CacheEntry
            {
                Bitmap = bitmap,
                Node = node
            };

            TrimCache();
            return bitmap;
        }
    }

    private static void TrimCache()
    {
        while (Order.Count > MaxCacheEntries)
        {
            var last = Order.Last;
            if (last == null)
            {
                break;
            }

            var key = last.Value;
            Order.RemoveLast();
            if (Cache.Remove(key, out var entry))
            {
                entry.Bitmap.Dispose();
            }
        }
    }

    private static async Task<Bitmap?> LoadUncachedAsync(string source)
    {
        try
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    var contentLength = response.Content.Headers.ContentLength;
                    if (contentLength.HasValue && contentLength.Value > MaxDownloadBytes)
                    {
                        return null;
                    }

                    await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await using var memory = new MemoryStream();
                    await responseStream.CopyToAsync(memory).ConfigureAwait(false);

                    if (memory.Length == 0 || memory.Length > MaxDownloadBytes)
                    {
                        return null;
                    }

                    memory.Position = 0;
                    return CreateBitmap(memory);
                }

                if (uri.IsFile)
                {
                    source = uri.LocalPath;
                }
            }

            var path = NormalizeLocalPath(source);
            if (path == null || !File.Exists(path))
            {
                return null;
            }

            if (!ImageValidation.IsSupportedImage(path))
            {
                return null;
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Length <= 0 || fileInfo.Length > MaxDownloadBytes)
            {
                return null;
            }

            await using var fileStream = File.OpenRead(path);
            return CreateBitmap(fileStream);
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? CreateBitmap(Stream stream)
    {
        try
        {
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetCacheKey(string source, out string cacheKey, out string normalizedSource)
    {
        normalizedSource = source.Trim();

        if (Uri.TryCreate(normalizedSource, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                cacheKey = uri.AbsoluteUri;
                normalizedSource = uri.AbsoluteUri;
                return true;
            }

            if (uri.IsFile)
            {
                normalizedSource = uri.LocalPath;
            }
        }

        var path = NormalizeLocalPath(normalizedSource);
        if (path == null)
        {
            cacheKey = string.Empty;
            return false;
        }

        cacheKey = path;
        normalizedSource = path;
        return true;
    }

    private static string? NormalizeLocalPath(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var path = source.Replace('/', Path.DirectorySeparatorChar).Trim();

        // If it's an absolute path, return the full path.
        if (Path.IsPathRooted(path))
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        // Try several candidate base directories where the relative path might live.
        var candidates = new List<string>
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        // Also walk up from AppContext.BaseDirectory and current directory a few levels to find a matching file.
        void AddParents(string start)
        {
            try
            {
                var dir = new DirectoryInfo(start);
                for (var i = 0; i < 6 && dir != null; i++)
                {
                    if (!candidates.Contains(dir.FullName)) candidates.Add(dir.FullName);
                    dir = dir.Parent;
                }
            }
            catch { }
        }

        AddParents(AppContext.BaseDirectory);
        AddParents(Directory.GetCurrentDirectory());

        foreach (var baseDir in candidates)
        {
            try
            {
                var tryPath = Path.GetFullPath(Path.Combine(baseDir, path));
                if (File.Exists(tryPath))
                {
                    return tryPath;
                }
            }
            catch { }
        }

        // If not found, fall back to resolving under AppContext.BaseDirectory (previous behaviour).
        try
        {
            var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
            return fallback;
        }
        catch
        {
            return null;
        }
    }
}
