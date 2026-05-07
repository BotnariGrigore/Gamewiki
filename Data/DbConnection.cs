using System.Configuration;
using MySql.Data.MySqlClient;

namespace GameWikiApp.Data
{
    public static class DbConnection
    {
        private static readonly string _connectionString;

        static DbConnection()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["GameWikiDb"]?.ConnectionString
                ?? throw new ConfigurationErrorsException("Missing connection string 'GameWikiDb' in App.config.");
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}