using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ChatRepository
    {
        public async Task<int> CreateConversationAsync(int createdBy, string? name, bool isGroup)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO conversations (created_by, conversation_name, is_group_chat)
                        VALUES (@CreatedBy, @Name, @IsGroup);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, new { CreatedBy = createdBy, Name = name, IsGroup = isGroup });
        }

        public async Task<int> SendMessageAsync(Message message)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO messages (conversation_id, sender_id, message_text, image_url, is_edited, is_deleted)
                        VALUES (@ConversationId, @SenderId, @MessageText, @ImageUrl, @IsEdited, @IsDeleted);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, message);
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int conversationId)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM messages WHERE conversation_id = @Cid ORDER BY sent_at ASC";
            return await conn.QueryAsync<Message>(sql, new { Cid = conversationId });
        }

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
