using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ChatRepository
    {
        public async Task<bool> IsParticipantAsync(int conversationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT COUNT(*)
FROM conversation_participants
WHERE conversation_id = @ConversationId
  AND user_id = @UserId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { ConversationId = conversationId, UserId = userId });
            return count > 0;
        }

        public async Task<IEnumerable<int>> GetParticipantIdsAsync(int conversationId, int? excludeUserId = null)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT user_id
FROM conversation_participants
WHERE conversation_id = @ConversationId
  AND (@ExcludeUserId IS NULL OR user_id <> @ExcludeUserId)
ORDER BY participant_id ASC";
            return await conn.QueryAsync<int>(sql, new { ConversationId = conversationId, ExcludeUserId = excludeUserId });
        }

        public async Task<Conversation?> GetPrivateConversationAsync(int userId, int otherUserId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BuildConversationSelect() + @"
WHERE c.is_group_chat = 0
  AND EXISTS (
      SELECT 1
      FROM conversation_participants cp
      WHERE cp.conversation_id = c.conversation_id
        AND cp.user_id = @UserId
  )
  AND EXISTS (
      SELECT 1
      FROM conversation_participants cp
      WHERE cp.conversation_id = c.conversation_id
        AND cp.user_id = @OtherUserId
  )
  AND (
      SELECT COUNT(*)
      FROM conversation_participants cp
      WHERE cp.conversation_id = c.conversation_id
  ) = 2
LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Conversation>(sql, new { UserId = userId, OtherUserId = otherUserId });
        }

        public async Task<Conversation?> GetConversationByIdAsync(int conversationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BuildConversationSelect() + @"
WHERE c.conversation_id = @ConversationId
  AND EXISTS (
      SELECT 1
      FROM conversation_participants cp
      WHERE cp.conversation_id = c.conversation_id
        AND cp.user_id = @UserId
  )
LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Conversation>(sql, new { ConversationId = conversationId, UserId = userId });
        }

        public async Task<IEnumerable<Conversation>> GetRecentConversationsAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BuildConversationSelect() + @"
WHERE c.is_group_chat = 0
  AND EXISTS (
      SELECT 1
      FROM conversation_participants cp
      WHERE cp.conversation_id = c.conversation_id
        AND cp.user_id = @UserId
  )
ORDER BY COALESCE(lm.sent_at, c.created_at) DESC, c.conversation_id DESC";
            return await conn.QueryAsync<Conversation>(sql, new { UserId = userId });
        }

        public async Task<int> CreatePrivateConversationAsync(int createdBy, int otherUserId)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                var sql = @"
INSERT INTO conversations (created_by, conversation_name, is_group_chat)
VALUES (@CreatedBy, NULL, 0);
SELECT LAST_INSERT_ID();";
                var conversationId = await conn.ExecuteScalarAsync<int>(sql, new { CreatedBy = createdBy }, tran);

                await conn.ExecuteAsync(@"
INSERT INTO conversation_participants (conversation_id, user_id)
VALUES (@ConversationId, @UserId),
       (@ConversationId, @OtherUserId)",
                    new { ConversationId = conversationId, UserId = createdBy, OtherUserId = otherUserId },
                    tran);

                tran.Commit();
                return conversationId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int conversationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT m.message_id AS MessageId,
       m.conversation_id AS ConversationId,
       m.sender_id AS SenderId,
       s.username AS SenderUsername,
       s.profile_image AS SenderProfileImage,
       m.message_text AS MessageText,
       m.image_url AS ImageUrl,
       m.is_edited AS IsEdited,
       m.is_deleted AS IsDeleted,
       m.sent_at AS SentAt,
       COALESCE(rc.read_count, 0) AS ReadCount,
       CASE WHEN m.sender_id = @UserId THEN 1 ELSE 0 END AS IsMine,
       CASE WHEN EXISTS (
            SELECT 1
            FROM message_reads mr
            WHERE mr.message_id = m.message_id
              AND mr.user_id = @UserId
       ) THEN 1 ELSE 0 END AS IsReadByMe
FROM messages m
INNER JOIN users s
    ON s.user_id = m.sender_id
LEFT JOIN (
    SELECT message_id, COUNT(*) AS read_count
    FROM message_reads
    GROUP BY message_id
) rc ON rc.message_id = m.message_id
WHERE m.conversation_id = @ConversationId
ORDER BY m.sent_at ASC, m.message_id ASC";
            return await conn.QueryAsync<Message>(sql, new { ConversationId = conversationId, UserId = userId });
        }

        public async Task<int> SendMessageAsync(int conversationId, int senderId, string? text, string? imageUrl)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO messages (conversation_id, sender_id, message_text, image_url)
VALUES (@ConversationId, @SenderId, @MessageText, @ImageUrl);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                ConversationId = conversationId,
                SenderId = senderId,
                MessageText = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
            });
        }

        public async Task<bool> EditMessageAsync(int messageId, int senderId, string? text, string? imageUrl)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
UPDATE messages
SET message_text = @MessageText,
    image_url = @ImageUrl,
    is_edited = 1
WHERE message_id = @MessageId
  AND sender_id = @SenderId
  AND is_deleted = 0";
            var rows = await conn.ExecuteAsync(sql, new
            {
                MessageId = messageId,
                SenderId = senderId,
                MessageText = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
            });
            return rows > 0;
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int senderId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
UPDATE messages
SET message_text = NULL,
    image_url = NULL,
    is_deleted = 1,
    is_edited = 0
WHERE message_id = @MessageId
  AND sender_id = @SenderId
  AND is_deleted = 0";
            var rows = await conn.ExecuteAsync(sql, new { MessageId = messageId, SenderId = senderId });
            return rows > 0;
        }

        public async Task<int> MarkConversationAsReadAsync(int conversationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT IGNORE INTO message_reads (message_id, user_id)
SELECT m.message_id, @UserId
FROM messages m
WHERE m.conversation_id = @ConversationId
  AND m.sender_id <> @UserId";
            return await conn.ExecuteAsync(sql, new { ConversationId = conversationId, UserId = userId });
        }

        private static string BuildConversationSelect()
        {
            return @"
SELECT c.conversation_id AS ConversationId,
       c.created_by AS CreatedBy,
       c.conversation_name AS ConversationName,
       c.is_group_chat AS IsGroupChat,
       c.created_at AS CreatedAt,
       COALESCE((
           SELECT COUNT(*)
           FROM conversation_participants cp
           WHERE cp.conversation_id = c.conversation_id
       ), 0) AS ParticipantCount,
       (
           SELECT cp.user_id
           FROM conversation_participants cp
           WHERE cp.conversation_id = c.conversation_id
             AND cp.user_id <> @UserId
           ORDER BY cp.participant_id ASC
           LIMIT 1
       ) AS OtherUserId,
       ou.username AS OtherUsername,
       ou.profile_image AS OtherProfileImage,
       ou.bio AS OtherBio,
       ou.is_online AS OtherIsOnline,
       ou.last_seen AS OtherLastSeen,
       orl.role_name AS OtherRoleName,
       lm.message_id AS LastMessageId,
       lm.sender_id AS LastMessageSenderId,
       ls.username AS LastMessageSenderUsername,
       ls.profile_image AS LastMessageSenderProfileImage,
       lm.message_text AS LastMessageText,
       lm.image_url AS LastMessageImageUrl,
       lm.sent_at AS LastMessageAt,
       COALESCE((
           SELECT COUNT(*)
           FROM messages mx
           LEFT JOIN message_reads mrx
             ON mrx.message_id = mx.message_id
            AND mrx.user_id = @UserId
           WHERE mx.conversation_id = c.conversation_id
             AND mx.sender_id <> @UserId
             AND mrx.read_id IS NULL
       ), 0) AS UnreadCount
FROM conversations c
LEFT JOIN users ou
    ON ou.user_id = (
        SELECT cp.user_id
        FROM conversation_participants cp
        WHERE cp.conversation_id = c.conversation_id
          AND cp.user_id <> @UserId
        ORDER BY cp.participant_id ASC
        LIMIT 1
    )
LEFT JOIN roles orl
    ON orl.role_id = ou.role_id
LEFT JOIN messages lm
    ON lm.message_id = (
        SELECT mx.message_id
        FROM messages mx
        WHERE mx.conversation_id = c.conversation_id
        ORDER BY mx.sent_at DESC, mx.message_id DESC
        LIMIT 1
    )
LEFT JOIN users ls
    ON ls.user_id = lm.sender_id";
        }
    }
}
