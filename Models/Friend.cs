using System;

namespace GameWikiApp.Models
{
    public class Friend
    {
        public int FriendshipId { get; set; }
        public int UserId { get; set; }
        public int FriendId { get; set; }
        public string Status { get; set; } = "none";
        public DateTime? CreatedAt { get; set; }

        public int OtherUserId { get; set; }
        public string OtherUsername { get; set; } = string.Empty;
        public string? OtherEmail { get; set; }
        public string? OtherProfileImage { get; set; }
        public string? OtherBio { get; set; }
        public bool OtherIsOnline { get; set; }
        public DateTime? OtherLastSeen { get; set; }
        public string? OtherRoleName { get; set; }

        public bool IsIncomingRequest { get; set; }
        public bool IsOutgoingRequest { get; set; }

        public bool IsAccepted => string.Equals(Status, "accepted", StringComparison.OrdinalIgnoreCase);
        public bool IsPending => string.Equals(Status, "pending", StringComparison.OrdinalIgnoreCase);
        public bool IsBlocked => string.Equals(Status, "blocked", StringComparison.OrdinalIgnoreCase);
        public bool HasRelation => FriendshipId > 0;
    }
}
