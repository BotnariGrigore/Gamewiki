using System;
using Avalonia;
using Avalonia.Styling;
using System.IO;
using GameWikiApp.Models;

namespace GameWikiApp;

public static class AppState
{
    public static User? CurrentUser { get; private set; }
    public static string? Token { get; private set; }
    public static string PreferredTheme { get; private set; } = "light";

    public static bool IsAuthenticated => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.RoleId == 1;
    public static bool IsDark => !string.Equals(PreferredTheme, "light", StringComparison.OrdinalIgnoreCase);

    public static void StartSession(User user, string token, string preferredTheme = "light")
    {
        CurrentUser = user;
        Token = token;
        PreferredTheme = string.IsNullOrWhiteSpace(preferredTheme) ? "light" : preferredTheme.Trim();
        ApplyThemeVariant();
        // Persist local copy of preference
        SaveLocalThemePreference();
    }

    public static void ApplyThemePreference(string? theme)
    {
        PreferredTheme = string.IsNullOrWhiteSpace(theme) ? "light" : theme.Trim();
        if (CurrentUser != null)
        {
            CurrentUser.ThemePreference = PreferredTheme;
        }

        ApplyThemeVariant();
        // Save local preference as well so unauthenticated users keep their choice
        SaveLocalThemePreference();
    }

    public static void EndSession()
    {
        CurrentUser = null;
        Token = null;
        PreferredTheme = "light";
        ApplyThemeVariant();
    }

    public static void LoadLocalThemePreference()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameWikiApp");
            var file = Path.Combine(dir, "theme.txt");
            if (File.Exists(file))
            {
                var t = File.ReadAllText(file).Trim();
                if (!string.IsNullOrWhiteSpace(t))
                {
                    PreferredTheme = t;
                }
            }
        }
        catch
        {
            // ignore errors
        }
    }

    public static void SaveLocalThemePreference()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameWikiApp");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "theme.txt");
            File.WriteAllText(file, PreferredTheme ?? "light");
        }
        catch
        {
            // ignore errors
        }
    }

    private static void ApplyThemeVariant()
    {
        if (Application.Current == null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
