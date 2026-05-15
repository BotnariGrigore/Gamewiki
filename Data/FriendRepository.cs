using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class FriendRepository
    {
        public async Task<IEnumerable<Friend>> GetFriendsAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT f.friendship_id AS FriendshipId,
       f.user_id AS UserId,
       f.friend_id AS FriendId,
       f.status AS Status,
       f.created_at AS CreatedAt,
       CASE WHEN f.user_id = @UserId THEN f.friend_id ELSE f.user_id END AS OtherUserId,
       u.username AS OtherUsername,
       u.email AS OtherEmail,
       u.profile_image AS OtherProfileImage,
       u.bio AS OtherBio,
       u.is_online AS OtherIsOnline,
       u.last_seen AS OtherLastSeen,
       r.role_name AS OtherRoleName,
       CASE WHEN f.friend_id = @UserId THEN 1 ELSE 0 END AS IsIncomingRequest,
       CASE WHEN f.user_id = @UserId THEN 1 ELSE 0 END AS IsOutgoingRequest
FROM friends f
INNER JOIN users u
    ON u.user_id = CASE WHEN f.user_id = @UserId THEN f.friend_id ELSE f.user_id END
LEFT JOIN roles r
    ON r.role_id = u.role_id
WHERE f.status = 'accepted'
  AND (f.user_id = @UserId OR f.friend_id = @UserId)
ORDER BY u.is_online DESC, u.username ASC";
            return await conn.QueryAsync<Friend>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<Friend>> GetIncomingRequestsAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT f.friendship_id AS FriendshipId,
       f.user_id AS UserId,
       f.friend_id AS FriendId,
       f.status AS Status,
       f.created_at AS CreatedAt,
       u.user_id AS OtherUserId,
       u.username AS OtherUsername,
       u.email AS OtherEmail,
       u.profile_image AS OtherProfileImage,
       u.bio AS OtherBio,
       u.is_online AS OtherIsOnline,
       u.last_seen AS OtherLastSeen,
       r.role_name AS OtherRoleName,
       1 AS IsIncomingRequest,
       0 AS IsOutgoingRequest
FROM friends f
INNER JOIN users u
    ON u.user_id = f.user_id
LEFT JOIN roles r
    ON r.role_id = u.role_id
WHERE f.friend_id = @UserId
  AND f.status = 'pending'
ORDER BY f.created_at DESC, f.friendship_id DESC";
            return await conn.QueryAsync<Friend>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<Friend>> GetOutgoingRequestsAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT f.friendship_id AS FriendshipId,
       f.user_id AS UserId,
       f.friend_id AS FriendId,
       f.status AS Status,
       f.created_at AS CreatedAt,
       u.user_id AS OtherUserId,
       u.username AS OtherUsername,
       u.email AS OtherEmail,
       u.profile_image AS OtherProfileImage,
       u.bio AS OtherBio,
       u.is_online AS OtherIsOnline,
       u.last_seen AS OtherLastSeen,
       r.role_name AS OtherRoleName,
       0 AS IsIncomingRequest,
       1 AS IsOutgoingRequest
FROM friends f
INNER JOIN users u
    ON u.user_id = f.friend_id
LEFT JOIN roles r
    ON r.role_id = u.role_id
WHERE f.user_id = @UserId
  AND f.status = 'pending'
ORDER BY f.created_at DESC, f.friendship_id DESC";
            return await conn.QueryAsync<Friend>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<Friend>> SearchUsersAsync(int userId, string query, int limit = 20)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT 0 AS FriendshipId,
       @UserId AS UserId,
       u.user_id AS FriendId,
       COALESCE((
           SELECT f.status
           FROM friends f
           WHERE (f.user_id = @UserId AND f.friend_id = u.user_id)
              OR (f.user_id = u.user_id AND f.friend_id = @UserId)
           ORDER BY f.created_at DESC, f.friendship_id DESC
           LIMIT 1
       ), 'none') AS Status,
       (
           SELECT f.created_at
           FROM friends f
           WHERE (f.user_id = @UserId AND f.friend_id = u.user_id)
              OR (f.user_id = u.user_id AND f.friend_id = @UserId)
           ORDER BY f.created_at DESC, f.friendship_id DESC
           LIMIT 1
       ) AS CreatedAt,
       u.user_id AS OtherUserId,
       u.username AS OtherUsername,
       u.email AS OtherEmail,
       u.profile_image AS OtherProfileImage,
       u.bio AS OtherBio,
       u.is_online AS OtherIsOnline,
       u.last_seen AS OtherLastSeen,
       r.role_name AS OtherRoleName,
       CASE WHEN EXISTS(
           SELECT 1
           FROM friends f
           WHERE f.user_id = @UserId
             AND f.friend_id = u.user_id
             AND f.status = 'pending'
       ) THEN 1 ELSE 0 END AS IsOutgoingRequest,
       CASE WHEN EXISTS(
           SELECT 1
           FROM friends f
           WHERE f.user_id = u.user_id
             AND f.friend_id = @UserId
             AND f.status = 'pending'
       ) THEN 1 ELSE 0 END AS IsIncomingRequest
FROM users u
LEFT JOIN roles r
    ON r.role_id = u.role_id
WHERE u.user_id <> @UserId
  AND (u.username LIKE @Query OR u.email LIKE @Query)
ORDER BY u.is_online DESC, u.username ASC
LIMIT @Limit";
            return await conn.QueryAsync<Friend>(sql, new { UserId = userId, Query = "%" + query.Trim() + "%", Limit = limit });
        }

        public async Task<IEnumerable<Friend>> GetRelationshipRowsAsync(int userId, int otherUserId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT friendship_id AS FriendshipId,
       user_id AS UserId,
       friend_id AS FriendId,
       status AS Status,
       created_at AS CreatedAt,
       CASE WHEN user_id = @UserId THEN friend_id ELSE user_id END AS OtherUserId,
       CASE WHEN user_id = @UserId THEN 1 ELSE 0 END AS IsOutgoingRequest,
       CASE WHEN friend_id = @UserId THEN 1 ELSE 0 END AS IsIncomingRequest
FROM friends
WHERE (user_id = @UserId AND friend_id = @OtherUserId)
   OR (user_id = @OtherUserId AND friend_id = @UserId)
ORDER BY created_at DESC, friendship_id DESC";
            return await conn.QueryAsync<Friend>(sql, new { UserId = userId, OtherUserId = otherUserId });
        }

        public async Task<int> CreateRequestAsync(int userId, int friendId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO friends (user_id, friend_id, status)
VALUES (@UserId, @FriendId, 'pending');
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId, FriendId = friendId });
        }

        public async Task<bool> AcceptRequestAsync(int friendshipId, int currentUserId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
UPDATE friends
SET status = 'accepted'
WHERE friendship_id = @FriendshipId
  AND friend_id = @CurrentUserId
  AND status = 'pending'";
            var rows = await conn.ExecuteAsync(sql, new { FriendshipId = friendshipId, CurrentUserId = currentUserId });
            return rows > 0;
        }

        public async Task<bool> DeclineRequestAsync(int friendshipId, int currentUserId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
DELETE FROM friends
WHERE friendship_id = @FriendshipId
  AND friend_id = @CurrentUserId
  AND status = 'pending'";
            var rows = await conn.ExecuteAsync(sql, new { FriendshipId = friendshipId, CurrentUserId = currentUserId });
            return rows > 0;
        }

        public async Task<bool> DeleteRelationshipAsync(int userId, int otherUserId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
DELETE FROM friends
WHERE (user_id = @UserId AND friend_id = @OtherUserId)
   OR (user_id = @OtherUserId AND friend_id = @UserId)";
            var rows = await conn.ExecuteAsync(sql, new { UserId = userId, OtherUserId = otherUserId });
            return rows > 0;
        }
    }
}
