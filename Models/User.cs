using System;

namespace GameWikiApp.Models
{
    public class User
    {
        public int UserId { get; set; }
        public int RoleId { get; set; } = 2; // default role = user

        public string Username { get; set; }
        public string Email { get; set; }

        // hashed password
        public string PasswordHash { get; set; }

        public string ProfileImage { get; set; }
        public string Bio { get; set; }

        public bool IsOnline { get; set; } = false;
        public DateTime? LastSeen { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}