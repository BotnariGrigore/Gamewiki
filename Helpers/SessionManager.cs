using GameWikiApp.Models;

namespace GameWikiApp.Helpers
{
    public static class SessionManager
    {
        public static User? CurrentUser { get; private set; }
        public static string? Token { get; private set; }

        public static void StartSession(User user, string token)
        {
            CurrentUser = user;
            Token = token;
        }

        public static void EndSession()
        {
            CurrentUser = null;
            Token = null;
        }

        public static bool IsAuthenticated => CurrentUser != null;
    }
}
