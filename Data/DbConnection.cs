using System.Data;
using MySqlConnector;

namespace GameWikiApp.Data
{
    public static class DbConnection
    {
        public static IDbConnection GetConnection()
        {
            return new MySqlConnection(GameWikiApp.Config.DatabaseConfig.ConnectionString);
        }

        public static IDbConnection GetOpen()
        {
            var conn = GetConnection();
            conn.Open();
            return conn;
        }
    }
}