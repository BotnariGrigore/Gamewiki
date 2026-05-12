using System.Text.RegularExpressions;

namespace GameWikiApp.Helpers
{
    public static class Validator
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static bool IsEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex.IsMatch(email.Trim());
        }

        public static bool IsStrongPassword(string? password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8) return false;
            if (!Regex.IsMatch(password, "[A-Z]")) return false;
            if (!Regex.IsMatch(password, "[a-z]")) return false;
            if (!Regex.IsMatch(password, "[0-9]")) return false;
            if (!Regex.IsMatch(password, "[\\W_]")) return false;
            return true;
        }
    }
}
