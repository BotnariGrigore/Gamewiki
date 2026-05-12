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
    }
}
