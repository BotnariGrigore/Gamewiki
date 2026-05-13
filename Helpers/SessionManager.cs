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
            // Enable simplified UI after login so controls behave in a more standard way.
            ThemeHelper.SimplifiedUI = true;
        }

        public static void SetPreferredTheme(string theme)
        {
            PreferredTheme = theme ?? "dark";
        }

        public static void EndSession()
        {
            CurrentUser = null;
            Token = null;
            PreferredTheme = "dark";
            // Restore visuals for auth screens / next session
            ThemeHelper.SimplifiedUI = false;
        }

        public static bool IsAuthenticated => CurrentUser != null;
    }
}