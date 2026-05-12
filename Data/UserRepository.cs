using System.Threading.Tasks;
using Dapper;
using System.Linq;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class UserRepository
    {
        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM users WHERE username = @Username LIMIT 1";
            return (await conn.QueryAsync<User>(sql, new { Username = username })).FirstOrDefault();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM users WHERE email = @Email LIMIT 1";
            return (await conn.QueryAsync<User>(sql, new { Email = email })).FirstOrDefault();
        }

        public async Task<int> CreateAsync(User user)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO users (role_id, username, email, password_hash, profile_image, bio, is_online, last_seen)
                        VALUES (@RoleId, @Username, @Email, @PasswordHash, @ProfileImage, @Bio, @IsOnline, @LastSeen);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, user);
        }

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
