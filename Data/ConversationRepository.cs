using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ConversationRepository
    {
        public async Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"SELECT c.*,
                        (SELECT COUNT(*) FROM conversation_participants cp WHERE cp.conversation_id = c.conversation_id) as participant_count,
                        (SELECT MAX(sent_at) FROM messages m WHERE m.conversation_id = c.conversation_id) as last_message_at,
                        (SELECT message_text FROM messages m WHERE m.conversation_id = c.conversation_id ORDER BY sent_at DESC LIMIT 1) as last_message_text
                        FROM conversations c
                        WHERE c.conversation_id IN (
                            SELECT cp.conversation_id FROM conversation_participants cp WHERE cp.user_id = @UserId
                        )
                        ORDER BY last_message_at DESC";
            return await conn.QueryAsync<Conversation>(sql, new { UserId = userId });
        }

        public async Task<Conversation?> GetByIdAsync(int id)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT * FROM conversations WHERE conversation_id = @Id LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Conversation>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Conversation conversation)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"INSERT INTO conversations (created_by, conversation_name, is_group_chat)
                        VALUES (@CreatedBy, @ConversationName, @IsGroupChat);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, conversation);
        }

        public async Task<IEnumerable<ConversationParticipant>> GetParticipantsAsync(int conversationId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"SELECT cp.*, u.username, u.profile_image, u.is_online
                        FROM conversation_participants cp
                        JOIN users u ON cp.user_id = u.user_id
                        WHERE cp.conversation_id = @ConversationId";
            return await conn.QueryAsync<ConversationParticipant>(sql, new { ConversationId = conversationId });
        }

        public async Task AddParticipantAsync(int conversationId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"INSERT IGNORE INTO conversation_participants (conversation_id, user_id) VALUES (@Cid, @Uid)";
            await conn.ExecuteAsync(sql, new { Cid = conversationId, Uid = userId });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "DELETE FROM conversations WHERE conversation_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = id });
            return res > 0;
        }
    }

    public class Conversation
    {
        public int ConversationId { get; set; }
        public int CreatedBy { get; set; }
        public string ConversationName { get; set; } = string.Empty;
        public bool IsGroupChat { get; set; }
        public DateTime CreatedAt { get; set; }
        // Runtime properties (not in DB)
        public int ParticipantCount { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessageText { get; set; }
    }

    public class ConversationParticipant
    {
        public int ParticipantId { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public bool IsOnline { get; set; }
    }
}