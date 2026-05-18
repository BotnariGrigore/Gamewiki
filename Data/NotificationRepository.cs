using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class NotificationRepository
    {
        public async Task<IEnumerable<Notification>> GetRecentAsync(int userId, int limit = 20)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT n.notification_id AS NotificationId,
       n.user_id AS UserId,
       n.source_user_id AS SourceUserId,
       su.username AS SourceUsername,
       su.profile_image AS SourceProfileImage,
       COALESCE(n.notification_type, 'general') AS NotificationType,
       COALESCE(n.title, '') AS Title,
       COALESCE(n.message, '') AS Message,
       COALESCE(n.target_type, '') AS TargetType,
       n.target_id AS TargetId,
       COALESCE(n.action_route, '') AS ActionRoute,
       COALESCE(n.is_read, 0) AS IsRead,
       n.created_at AS CreatedAt
FROM notifications n
LEFT JOIN users su
    ON su.user_id = n.source_user_id
WHERE n.user_id = @UserId
ORDER BY COALESCE(n.is_read, 0) ASC, n.created_at DESC, n.notification_id DESC
LIMIT @Limit";
            return await conn.QueryAsync<Notification>(sql, new { UserId = userId, Limit = limit });
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId AND COALESCE(is_read, 0) = 0",
                new { UserId = userId });
        }

        public async Task<Notification?> GetByIdAsync(int notificationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT n.notification_id AS NotificationId,
       n.user_id AS UserId,
       n.source_user_id AS SourceUserId,
       su.username AS SourceUsername,
       su.profile_image AS SourceProfileImage,
       COALESCE(n.notification_type, 'general') AS NotificationType,
       COALESCE(n.title, '') AS Title,
       COALESCE(n.message, '') AS Message,
       COALESCE(n.target_type, '') AS TargetType,
       n.target_id AS TargetId,
       COALESCE(n.action_route, '') AS ActionRoute,
       COALESCE(n.is_read, 0) AS IsRead,
       n.created_at AS CreatedAt
FROM notifications n
LEFT JOIN users su
    ON su.user_id = n.source_user_id
WHERE n.notification_id = @NotificationId
  AND n.user_id = @UserId
LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Notification>(sql, new { NotificationId = notificationId, UserId = userId });
        }

        public async Task<int> CreateAsync(Notification notification)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO notifications
    (user_id, source_user_id, notification_type, title, message, target_type, target_id, action_route, is_read)
VALUES
    (@UserId, @SourceUserId, @NotificationType, @Title, @Message, @TargetType, @TargetId, @ActionRoute, @IsRead);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, notification);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync(
                "UPDATE notifications SET is_read = 1 WHERE notification_id = @NotificationId AND user_id = @UserId",
                new { NotificationId = notificationId, UserId = userId });
            return rows > 0;
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            return await conn.ExecuteAsync(
                "UPDATE notifications SET is_read = 1 WHERE user_id = @UserId AND COALESCE(is_read, 0) = 0",
                new { UserId = userId });
        }
    }
}
