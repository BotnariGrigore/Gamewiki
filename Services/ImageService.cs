using System;
using System.IO;
using System.Threading.Tasks;
using GameWikiApp.Helpers;

namespace GameWikiApp.Services;

public class ImageService
{
    public string StorageRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GameWikiApp",
        "ProfileImages");

    public bool IsSupportedImage(string? sourcePath) => ImageValidation.IsSupportedImage(sourcePath);

    public async Task<string?> SaveProfileImageAsync(string sourcePath, int userId)
    {
        try
        {
            var validation = await ImageValidation.ValidateLocalImageAsync(sourcePath);
            if (!validation.Success)
            {
                return null;
            }

            Directory.CreateDirectory(StorageRoot);

            var extension = Path.GetExtension(sourcePath);
            var fileName = $"profile_{userId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
            var destination = Path.Combine(StorageRoot, fileName);

            await using var source = File.OpenRead(sourcePath);
            await using var target = File.Create(destination);
            await source.CopyToAsync(target);

            return destination;
        }
        catch
        {
            return null;
        }
    }
}
