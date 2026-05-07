using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ChatRepository
    {
        public int CreateConversation(int createdBy, string conversationName, bool isGroup)
        {
            const string sql = @"INSERT INTO conversations (created_by, conversation_name, is_group_chat) VALUES (@created_by, @conversation_name, @is_group_chat);
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@created_by", createdBy);
            cmd.Parameters.AddWithValue("@conversation_name", (object)conversationName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_group_chat", isGroup);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void AddParticipant(int conversationId, int userId)
        {
            const string sql = @"INSERT IGNORE INTO conversation_participants (conversation_id, user_id) VALUES (@conversation_id, @user_id)";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@conversation_id", conversationId);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.ExecuteNonQuery();
        }

        public int SendMessage(int conversationId, int senderId, string messageText, string imageUrl = null)
        {
            const string sql = @"INSERT INTO messages (conversation_id, sender_id, message_text, image_url) VALUES (@conversation_id, @sender_id, @message_text, @image_url);
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@conversation_id", conversationId);
            cmd.Parameters.AddWithValue("@sender_id", senderId);
            cmd.Parameters.AddWithValue("@message_text", (object)messageText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@image_url", (object)imageUrl ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public IEnumerable<Message> GetMessages(int conversationId, int limit = 100)
        {
            var list = new List<Message>();
            const string sql = @"SELECT message_id, conversation_id, sender_id, message_text, image_url, is_edited, is_deleted, sent_at
                                 FROM messages WHERE conversation_id = @conversation_id ORDER BY sent_at DESC LIMIT @limit";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@conversation_id", conversationId);
            cmd.Parameters.AddWithValue("@limit", limit);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Message
                {
                    MessageId = rdr.GetInt32("message_id"),
                    ConversationId = rdr.GetInt32("conversation_id"),
                    SenderId = rdr.GetInt32("sender_id"),
                    MessageText = rdr.IsDBNull(rdr.GetOrdinal("message_text")) ? null : rdr.GetString("message_text"),
                    ImageUrl = rdr.IsDBNull(rdr.GetOrdinal("image_url")) ? null : rdr.GetString("image_url"),
                    IsEdited = rdr.GetBoolean("is_edited"),
                    IsDeleted = rdr.GetBoolean("is_deleted"),
                    SentAt = rdr.GetDateTime("sent_at")
                });
            }
            return list;
        }

        public void MarkMessageRead(int messageId, int userId)
        {
            const string sql = @"INSERT IGNORE INTO message_reads (message_id, user_id, read_at) VALUES (@message_id, @user_id, NOW())";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@message_id", messageId);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.ExecuteNonQuery();
        }
    }
}