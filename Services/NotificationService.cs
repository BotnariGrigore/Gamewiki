using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class NotificationService
    {
        public static event Action<int>? NotificationsChanged;

        private readonly NotificationRepository _repo = new();

        public Task<IEnumerable<Notification>> GetRecentAsync(int userId, int limit = 20) => _repo.GetRecentAsync(userId, limit);
        public Task<int> GetUnreadCountAsync(int userId) => _repo.GetUnreadCountAsync(userId);

        public async Task<int> CreateAsync(Notification notification)
        {
            if (notification.UserId <= 0)
            {
                return 0;
            }

            var id = await _repo.CreateAsync(notification);
            if (id > 0)
            {
                RaiseChanged(notification.UserId);
            }

            return id;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var marked = await _repo.MarkAsReadAsync(notificationId, userId);
            if (marked)
            {
                RaiseChanged(userId);
            }

            return marked;
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var rows = await _repo.MarkAllAsReadAsync(userId);
            if (rows > 0)
            {
                RaiseChanged(userId);
            }

            return rows;
        }

        public Task<int> NotifyFriendRequestAsync(User sender, User recipient)
        {
            return CreateAsync(new Notification
            {
                UserId = recipient.UserId,
                SourceUserId = sender.UserId,
                NotificationType = "friend_request",
                Title = $"{sender.Username} sent you a friend request",
                Message = $"{sender.Username} wants to connect with you.",
                TargetType = "user",
                TargetId = sender.UserId,
                ActionRoute = "friends",
                IsRead = false
            });
        }

        public Task<int> NotifyFriendAcceptedAsync(User sender, User recipient)
        {
            return CreateAsync(new Notification
            {
                UserId = recipient.UserId,
                SourceUserId = sender.UserId,
                NotificationType = "friend_accepted",
                Title = $"{sender.Username} accepted your request",
                Message = $"You can now chat with {sender.Username}.",
                TargetType = "user",
                TargetId = sender.UserId,
                ActionRoute = "friends-chat",
                IsRead = false
            });
        }

        public Task<int> NotifyMessageAsync(User sender, User recipient, string? previewText = null)
        {
            var message = string.IsNullOrWhiteSpace(previewText)
                ? $"{sender.Username} sent you a new message."
                : $"{sender.Username}: {TrimPreview(previewText)}";

            return CreateAsync(new Notification
            {
                UserId = recipient.UserId,
                SourceUserId = sender.UserId,
                NotificationType = "message",
                Title = $"{sender.Username} sent you a message",
                Message = message,
                TargetType = "user",
                TargetId = sender.UserId,
                ActionRoute = "friends-chat",
                IsRead = false
            });
        }

        public Task<int> NotifyCommentAsync(User actor, User recipient, WikiArticle article, string? commentPreview = null)
        {
            var message = string.IsNullOrWhiteSpace(commentPreview)
                ? $"{actor.Username} commented on {article.Title}."
                : $"{actor.Username}: {TrimPreview(commentPreview)}";

            return CreateAsync(new Notification
            {
                UserId = recipient.UserId,
                SourceUserId = actor.UserId,
                NotificationType = "comment",
                Title = $"New comment on {article.Title}",
                Message = message,
                TargetType = "article",
                TargetId = article.ArticleId,
                ActionRoute = "article",
                IsRead = false
            });
        }

        public Task<int> NotifyLikeAsync(User actor, User recipient, WikiArticle article)
        {
            return CreateAsync(new Notification
            {
                UserId = recipient.UserId,
                SourceUserId = actor.UserId,
                NotificationType = "like",
                Title = $"New like on {article.Title}",
                Message = $"{actor.Username} liked your article.",
                TargetType = "article",
                TargetId = article.ArticleId,
                ActionRoute = "article",
                IsRead = false
            });
        }

        public Task<int> NotifyArticleUpdatedAsync(User actor, User recipient, WikiArticle article)
        {
            return CreateAsync(new Notification
            {
                UserId = recipient.UserId,
                SourceUserId = actor.UserId,
                NotificationType = "article_update",
                Title = $"Article updated: {article.Title}",
                Message = $"{actor.Username} made important changes to {article.Title}.",
                TargetType = "article",
                TargetId = article.ArticleId,
                ActionRoute = "article",
                IsRead = false
            });
        }

        private static void RaiseChanged(int userId)
        {
            NotificationsChanged?.Invoke(userId);
        }

        private static string TrimPreview(string? value, int maxLength = 90)
        {
            var text = value?.Trim() ?? string.Empty;
            if (text.Length <= maxLength)
            {
                return text;
            }

            return text[..maxLength].TrimEnd() + "...";
        }
    }
}
