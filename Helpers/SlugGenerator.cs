using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GameWikiApp.Helpers
{
    public static class SlugGenerator
    {
        public static string Generate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", string.Empty);
            cleaned = Regex.Replace(cleaned, @"[\s]+", "-").Trim('-');
            cleaned = Regex.Replace(cleaned, "-+", "-");
            return cleaned;
        }
    }
}
