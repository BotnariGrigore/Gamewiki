using System;

namespace GameWikiApp.Models
{
    public class User
    {
        public int UserId { get; set; }
        public int RoleId { get; set; } = 2;
        public string? RoleName { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public string? ThemePreference { get; set; } = "light";
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
