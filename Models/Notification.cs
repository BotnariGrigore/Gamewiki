using System;

namespace GameWikiApp.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public int? SourceUserId { get; set; }
        public string? SourceUsername { get; set; }
        public string? SourceProfileImage { get; set; }
        public string NotificationType { get; set; } = "general";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public string? ActionRoute { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool CanOpen => !string.IsNullOrWhiteSpace(ActionRoute);

        public string TimeLabel
        {
            get
            {
                var delta = DateTime.Now - CreatedAt;
                if (delta.TotalMinutes < 1)
                {
                    return "Just now";
                }

                if (delta.TotalHours < 1)
                {
                    return $"{Math.Max(1, (int)delta.TotalMinutes)}m ago";
                }

                if (delta.TotalDays < 1)
                {
                    return $"{Math.Max(1, (int)delta.TotalHours)}h ago";
                }

                if (delta.TotalDays < 7)
                {
                    return $"{Math.Max(1, (int)delta.TotalDays)}d ago";
                }

                return CreatedAt.ToString("g");
            }
        }
    }
}
