using System;

namespace GameWikiApp.Models
{
    public class Conversation
    {
        public int ConversationId { get; set; }
        public int CreatedBy { get; set; }
        public string? ConversationName { get; set; }
        public bool IsGroupChat { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ParticipantCount { get; set; }

        public int OtherUserId { get; set; }
        public string OtherUsername { get; set; } = string.Empty;
        public string? OtherProfileImage { get; set; }
        public string? OtherBio { get; set; }
        public bool OtherIsOnline { get; set; }
        public DateTime? OtherLastSeen { get; set; }
        public string? OtherRoleName { get; set; }

        public int? LastMessageId { get; set; }
        public int? LastMessageSenderId { get; set; }
        public string? LastMessageSenderUsername { get; set; }
        public string? LastMessageSenderProfileImage { get; set; }
        public string? LastMessageText { get; set; }
        public string? LastMessageImageUrl { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }

        public bool HasUnread => UnreadCount > 0;
        public string DisplayName => string.IsNullOrWhiteSpace(ConversationName) ? OtherUsername : ConversationName;

        public string PreviewText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LastMessageText))
                {
                    return LastMessageText!;
                }

                if (!string.IsNullOrWhiteSpace(LastMessageImageUrl))
                {
                    return "Image";
                }

                return "No messages yet";
            }
        }
    }
}
