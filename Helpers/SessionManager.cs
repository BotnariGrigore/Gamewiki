using System;
using GameWikiApp.Models;

namespace GameWikiApp.Helpers
{
    public static class SessionManager
    {
        public static User? CurrentUser { get; private set; }
        public static string? Token { get; private set; }
        public static string PreferredTheme { get; private set; } = "dark";

        public static void StartSession(User user, string token, string preferredTheme = "dark")
        {
            CurrentUser = user;
            Token = token;
            PreferredTheme = preferredTheme ?? "dark";
            ThemeHelper.CurrentTheme = string.Equals(PreferredTheme, "light", StringComparison.OrdinalIgnoreCase)
                ? ThemeHelper.ThemeMode.Light
                : ThemeHelper.ThemeMode.Dark;
            ThemeHelper.SimplifiedUI = true;
        }

        public static void SetPreferredTheme(string theme)
        {
            PreferredTheme = theme ?? "dark";
            if (CurrentUser != null)
            {
                CurrentUser.ThemePreference = PreferredTheme;
            }
        }

        public static bool IsAdmin => CurrentUser?.RoleId == 1;

        public static void ApplyThemePreference(string? theme)
        {
            SetPreferredTheme(theme ?? "dark");
            ThemeHelper.CurrentTheme = string.Equals(PreferredTheme, "light", StringComparison.OrdinalIgnoreCase)
                ? ThemeHelper.ThemeMode.Light
                : ThemeHelper.ThemeMode.Dark;
        }

        public static void EndSession()
        {
            CurrentUser = null;
            Token = null;
            PreferredTheme = "dark";
            // Restore visuals for auth screens / next session
            ThemeHelper.SimplifiedUI = false;
            ThemeHelper.CurrentTheme = ThemeHelper.ThemeMode.Dark;
        }

        public static bool IsAuthenticated => CurrentUser != null;
    }
}
