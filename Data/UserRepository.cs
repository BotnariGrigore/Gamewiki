using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class UserRepository
    {
        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var conn = GetOpen();
            var sql = @"SELECT u.*, r.role_name AS RoleName
                        FROM users u
                        INNER JOIN roles r ON r.role_id = u.role_id
                        WHERE u.username = @Username
                        LIMIT 1";
            return (await conn.QueryAsync<User>(sql, new { Username = username })).FirstOrDefault();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var conn = GetOpen();
            var sql = @"SELECT u.*, r.role_name AS RoleName
                        FROM users u
                        INNER JOIN roles r ON r.role_id = u.role_id
                        WHERE u.email = @Email
                        LIMIT 1";
            return (await conn.QueryAsync<User>(sql, new { Email = email })).FirstOrDefault();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var conn = GetOpen();
            var sql = @"SELECT u.*, r.role_name AS RoleName
                        FROM users u
                        INNER JOIN roles r ON r.role_id = u.role_id
                        WHERE u.user_id = @Id
                        LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            using var conn = GetOpen();
            var sql = @"SELECT u.*, r.role_name AS RoleName
                        FROM users u
                        INNER JOIN roles r ON r.role_id = u.role_id
                        ORDER BY u.created_at DESC";
            return await conn.QueryAsync<User>(sql);
        }

        public async Task<int> GetAdminCountAsync()
        {
            using var conn = GetOpen();
            var sql = "SELECT COUNT(*) FROM users WHERE role_id = 1";
            return await conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> CreateAsync(User user)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO users (role_id, username, email, password_hash, profile_image, bio, is_online, last_seen, theme_preference)
                        VALUES (@RoleId, @Username, @Email, @PasswordHash, @ProfileImage, @Bio, @IsOnline, @LastSeen, @ThemePreference);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, user);
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var conn = GetOpen();
            var sql = @"UPDATE users SET username = @Username, email = @Email, profile_image = @ProfileImage, bio = @Bio, is_online = @IsOnline, last_seen = @LastSeen, theme_preference = @ThemePreference
                        WHERE user_id = @UserId";
            var res = await conn.ExecuteAsync(sql, user);
            return res > 0;
        }

        public async Task<bool> UpdateRoleAsync(int userId, int roleId)
        {
            using var conn = GetOpen();
            var sql = "UPDATE users SET role_id = @RoleId WHERE user_id = @UserId";
            var res = await conn.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
            return res > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = GetOpen();
            var sql = "DELETE FROM users WHERE user_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = id });
            return res > 0;
        }

        public async Task<bool> UpdateThemePreferenceAsync(int userId, string themePreference)
        {
            using var conn = GetOpen();
            var sql = "UPDATE users SET theme_preference = @ThemePreference WHERE user_id = @UserId";
            var res = await conn.ExecuteAsync(sql, new { UserId = userId, ThemePreference = themePreference });
            return res > 0;
        }

        private IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
