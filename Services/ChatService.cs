using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class ChatService
    {
        private readonly ChatRepository _repo = new();
        private readonly FriendRepository _friends = new();
        private readonly UserRepository _users = new();
        private readonly NotificationService _notifications = new();

        public Task<IEnumerable<Conversation>> GetRecentConversationsAsync(int userId) => _repo.GetRecentConversationsAsync(userId);

        public async Task<Conversation?> GetConversationAsync(int conversationId, int userId, bool markAsRead = true)
        {
            if (!await _repo.IsParticipantAsync(conversationId, userId))
            {
                return null;
            }

            if (markAsRead)
            {
                await _repo.MarkConversationAsReadAsync(conversationId, userId);
            }

            return await _repo.GetConversationByIdAsync(conversationId, userId);
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int conversationId, int userId, bool markAsRead = true)
        {
            if (!await _repo.IsParticipantAsync(conversationId, userId))
            {
                return Enumerable.Empty<Message>();
            }

            if (markAsRead)
            {
                await _repo.MarkConversationAsReadAsync(conversationId, userId);
            }

            return await _repo.GetMessagesAsync(conversationId, userId);
        }

        public async Task<(Conversation? conversation, string? error)> OpenPrivateConversationAsync(int userId, int otherUserId)
        {
            if (userId == otherUserId)
            {
                return (null, "You cannot open a conversation with yourself.");
            }

            var current = await _users.GetByIdAsync(userId);
            if (current == null)
            {
                return (null, "Current user not found.");
            }

            var other = await _users.GetByIdAsync(otherUserId);
            if (other == null)
            {
                return (null, "The selected user does not exist.");
            }

            var existing = await _repo.GetPrivateConversationAsync(userId, otherUserId);
            if (existing != null)
            {
                await _repo.MarkConversationAsReadAsync(existing.ConversationId, userId);
                var loaded = await _repo.GetConversationByIdAsync(existing.ConversationId, userId);
                return loaded != null ? (loaded, null) : (null, "The conversation could not be loaded.");
            }

            var isFriend = await _friends.GetRelationshipRowsAsync(userId, otherUserId);
            if (!isFriend.Any(row => row.IsAccepted))
            {
                return (null, "You can start a new private chat only with a friend.");
            }

            var conversationId = await _repo.CreatePrivateConversationAsync(userId, otherUserId);
            var conversation = await _repo.GetConversationByIdAsync(conversationId, userId);
            if (conversation != null)
            {
                await _repo.MarkConversationAsReadAsync(conversationId, userId);
                conversation = await _repo.GetConversationByIdAsync(conversationId, userId);
            }

            return conversation != null
                ? (conversation, null)
                : (null, "The conversation could not be created.");
        }

        public async Task<(bool success, string message)> SendMessageAsync(int conversationId, int senderId, string? text, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return (false, "Write a message or attach an image.");
            }

            if (!await _repo.IsParticipantAsync(conversationId, senderId))
            {
                return (false, "You are not part of this conversation.");
            }

            // Try to insert the message; if insertion fails, return failure.
            int messageId;
            try
            {
                messageId = await _repo.SendMessageAsync(conversationId, senderId, text, imageUrl);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("chat_errors.log", DateTime.UtcNow + " SEND MESSAGE DB ERROR: " + ex + Environment.NewLine); } catch { }
                return (false, "An unexpected error occurred while sending the message.");
            }

            if (messageId <= 0)
            {
                return (false, "The message could not be sent.");
            }

            // Message stored successfully — notify recipients in a best-effort manner.
            try
            {
                var sender = await _users.GetByIdAsync(senderId);
                if (sender != null)
                {
                    var preview = !string.IsNullOrWhiteSpace(text)
                        ? text!.Trim()
                        : (!string.IsNullOrWhiteSpace(imageUrl) ? "Image attachment" : null);

                    var recipientIds = await _repo.GetParticipantIdsAsync(conversationId, senderId);
                    foreach (var recipientId in recipientIds)
                    {
                        try
                        {
                            var recipient = await _users.GetByIdAsync(recipientId);
                            if (recipient != null)
                            {
                                await _notifications.NotifyMessageAsync(sender, recipient, preview);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            try { File.AppendAllText("chat_errors.log", DateTime.UtcNow + " NOTIFY RECIPIENT ERROR: " + innerEx + Environment.NewLine); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("chat_errors.log", DateTime.UtcNow + " SEND MESSAGE NOTIFY ERROR: " + ex + Environment.NewLine); } catch { }
            }

            return (true, "Message sent.");
        }

        public async Task<(bool success, string message)> EditMessageAsync(int messageId, int senderId, string? text, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return (false, "Write a message or attach an image.");
            }

            var edited = await _repo.EditMessageAsync(messageId, senderId, text, imageUrl);
            return edited
                ? (true, "Message updated.")
                : (false, "The message could not be updated.");
        }

        public async Task<(bool success, string message)> DeleteMessageAsync(int messageId, int senderId)
        {
            var deleted = await _repo.DeleteMessageAsync(messageId, senderId);
            return deleted
                ? (true, "Message deleted.")
                : (false, "The message could not be deleted.");
        }
    }
}
