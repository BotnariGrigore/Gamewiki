using System;

namespace GameWikiApp.Models
{
    public class UserWithStats
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? ThemePreference { get; set; }
        public DateTime CreatedAt { get; set; }

        public int GameCount { get; set; }
        public int ArticleCount { get; set; }
        public int CommentCount { get; set; }
        public int FriendCount { get; set; }
    }
}