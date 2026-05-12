using System;

namespace GameWikiApp.Models
{
    public enum FriendStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    public class Friend
    {
        public int FriendshipId { get; set; }
        public int UserId { get; set; }
        public int FriendId { get; set; }
        public FriendStatus Status { get; set; } = FriendStatus.Pending;
        public DateTime CreatedAt { get; set; }
    }
}
