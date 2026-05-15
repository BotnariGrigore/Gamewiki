using System;
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

        public async Task<IEnumerable<UserWithStats>> GetAllWithStatsAsync()
        {
            using var conn = GetOpen();
            var sql = @"
SELECT
    u.user_id AS UserId,
    u.role_id AS RoleId,
    r.role_name AS RoleName,
    u.username AS Username,
    u.email AS Email,
    u.profile_image AS ProfileImage,
    u.bio AS Bio,
    u.is_online AS IsOnline,
    u.last_seen AS LastSeen,
    u.theme_preference AS ThemePreference,
    u.created_at AS CreatedAt,
    COALESCE(g.game_count, 0) AS GameCount,
    COALESCE(a.article_count, 0) AS ArticleCount,
    COALESCE(c.comment_count, 0) AS CommentCount,
    COALESCE(f.friend_count, 0) AS FriendCount
FROM users u
INNER JOIN roles r ON r.role_id = u.role_id
LEFT JOIN (
    SELECT created_by, COUNT(*) AS game_count
    FROM games
    GROUP BY created_by
) g ON g.created_by = u.user_id
LEFT JOIN (
    SELECT author_id, COUNT(*) AS article_count
    FROM wiki_articles
    GROUP BY author_id
) a ON a.author_id = u.user_id
LEFT JOIN (
    SELECT user_id, COUNT(*) AS comment_count
    FROM article_comments
    GROUP BY user_id
) c ON c.user_id = u.user_id
LEFT JOIN (
    SELECT user_id, COUNT(*) AS friend_count
    FROM friends
    WHERE status = 'accepted'
    GROUP BY user_id
) f ON f.user_id = u.user_id
ORDER BY u.created_at DESC";
            return await conn.QueryAsync<UserWithStats>(sql);
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
            try
            {
                return await conn.ExecuteScalarAsync<int>(sql, user);
            }
            catch (System.Exception ex)
            {
                if (ex.Message != null && ex.Message.Contains("Unknown column 'theme_preference'"))
                {
                    try
                    {
                        await conn.ExecuteAsync("ALTER TABLE users ADD COLUMN theme_preference VARCHAR(20) DEFAULT 'light'");
                    }
                    catch { }

                    return await conn.ExecuteScalarAsync<int>(sql, user);
                }

                throw;
            }
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var conn = GetOpen();
            var sql = @"UPDATE users SET username = @Username, email = @Email, profile_image = @ProfileImage, bio = @Bio, is_online = @IsOnline, last_seen = @LastSeen, theme_preference = @ThemePreference
                        WHERE user_id = @UserId";
            try
            {
                var res = await conn.ExecuteAsync(sql, user);
                return res > 0;
            }
            catch (System.Exception ex)
            {
                if (ex.Message != null && ex.Message.Contains("Unknown column 'theme_preference'"))
                {
                    try
                    {
                        await conn.ExecuteAsync("ALTER TABLE users ADD COLUMN theme_preference VARCHAR(20) DEFAULT 'light'");
                    }
                    catch { }

                    var res = await conn.ExecuteAsync(sql, user);
                    return res > 0;
                }

                throw;
            }
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
            try
            {
                var res = await conn.ExecuteAsync(sql, new { UserId = userId, ThemePreference = themePreference });
                return res > 0;
            }
            catch (System.Exception ex)
            {
                if (ex.Message != null && ex.Message.Contains("Unknown column 'theme_preference'"))
                {
                    try
                    {
                        await conn.ExecuteAsync("ALTER TABLE users ADD COLUMN theme_preference VARCHAR(20) DEFAULT 'light'");
                    }
                    catch { }

                    var res = await conn.ExecuteAsync(sql, new { UserId = userId, ThemePreference = themePreference });
                    return res > 0;
                }

                throw;
            }
        }

        public async Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash)
        {
            using var conn = GetOpen();
            var sql = "UPDATE users SET password_hash = @PasswordHash WHERE user_id = @UserId";
            var res = await conn.ExecuteAsync(sql, new { UserId = userId, PasswordHash = passwordHash });
            return res > 0;
        }

        public async Task<bool> UpdatePresenceAsync(int userId, bool isOnline, DateTime? lastSeen = null)
        {
            using var conn = GetOpen();
            var sql = @"
UPDATE users
SET is_online = @IsOnline,
    last_seen = @LastSeen
WHERE user_id = @UserId";
            var rows = await conn.ExecuteAsync(sql, new
            {
                UserId = userId,
                IsOnline = isOnline,
                LastSeen = lastSeen ?? DateTime.UtcNow
            });
            return rows > 0;
        }

        private IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
