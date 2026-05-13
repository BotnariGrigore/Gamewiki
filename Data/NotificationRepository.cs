using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class NotificationRepository
    {
        public async Task<int> CreateAsync(int userId, string title, string message)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"INSERT INTO notifications (user_id, title, message)
                        VALUES (@UserId, @Title, @Message);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId, Title = title, Message = message });
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, bool unreadOnly = false, int limit = 50)
        {
            using var conn = DbConnection.GetOpen();
            var sql = unreadOnly
                ? "SELECT * FROM notifications WHERE user_id = @UserId AND is_read = 0 ORDER BY created_at DESC LIMIT @Limit"
                : "SELECT * FROM notifications WHERE user_id = @UserId ORDER BY created_at DESC LIMIT @Limit";
            return await conn.QueryAsync<Notification>(sql, new { UserId = userId, Limit = limit });
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId AND is_read = 0";
            return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "UPDATE notifications SET is_read = 1 WHERE notification_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = notificationId });
            return res > 0;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "UPDATE notifications SET is_read = 1 WHERE user_id = @UserId AND is_read = 0";
            var res = await conn.ExecuteAsync(sql, new { UserId = userId });
            return res > 0;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "DELETE FROM notifications WHERE notification_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = notificationId });
            return res > 0;
        }

        public async Task<bool> DeleteAllForUserAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "DELETE FROM notifications WHERE user_id = @UserId";
            var res = await conn.ExecuteAsync(sql, new { UserId = userId });
            return res > 0;
        }
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}