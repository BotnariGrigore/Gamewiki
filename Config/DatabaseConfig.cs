using System.Configuration;

namespace GameWikiApp.Config
{
    public static class DatabaseConfig
    {
        public static string ConnectionString => ConfigurationManager.ConnectionStrings["GameWikiDb"]?.ConnectionString ?? string.Empty;
    }
}
