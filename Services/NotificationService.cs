using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class NotificationService
    {
        private readonly NotificationRepository _repo = new();

        public Task<int> CreateAsync(int userId, string title, string message) => _repo.CreateAsync(userId, title, message);
        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, bool unreadOnly = false, int limit = 50) => _repo.GetByUserIdAsync(userId, unreadOnly, limit);
        public Task<int> GetUnreadCountAsync(int userId) => _repo.GetUnreadCountAsync(userId);
        public Task<bool> MarkAsReadAsync(int notificationId) => _repo.MarkAsReadAsync(notificationId);
        public Task<bool> MarkAllAsReadAsync(int userId) => _repo.MarkAllAsReadAsync(userId);
        public Task<bool> DeleteAsync(int notificationId) => _repo.DeleteAsync(notificationId);
        public Task<bool> DeleteAllForUserAsync(int userId) => _repo.DeleteAllForUserAsync(userId);
    }
}