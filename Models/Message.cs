using System;

namespace GameWikiApp.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string? SenderProfileImage { get; set; }
        public string? MessageText { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
        public int ReadCount { get; set; }
        public bool IsMine { get; set; }
        public bool IsReadByMe { get; set; }

        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
        public bool HasText => !string.IsNullOrWhiteSpace(MessageText);
        public bool HasReadReceipt => ReadCount > 0;

        public string Body => IsDeleted
            ? "Message deleted"
            : (MessageText ?? string.Empty);
    }
}
