using System.Threading.Tasks;
using GameWikiApp.Data;

namespace GameWikiApp.Services
{
    public class LikeService
    {
        private readonly LikeRepository _repo = new();
        private readonly ArticleRepository _articles = new();
        private readonly UserRepository _users = new();
        private readonly NotificationService _notifications = new();

        public Task<int> GetCountAsync(int articleId) => _repo.GetCountAsync(articleId);
        public Task<bool> HasLikedAsync(int articleId, int userId) => _repo.HasLikedAsync(articleId, userId);

        public async Task<bool> ToggleLikeAsync(int articleId, int userId)
        {
            var liked = await _repo.ToggleLikeAsync(articleId, userId);
            if (!liked)
            {
                return false;
            }

            var article = await _articles.GetByIdAsync(articleId);
            if (article == null || article.AuthorId == userId)
            {
                return true;
            }

            var actor = await _users.GetByIdAsync(userId);
            var recipient = await _users.GetByIdAsync(article.AuthorId);
            if (actor != null && recipient != null)
            {
                await _notifications.NotifyLikeAsync(actor, recipient, article);
            }

            return true;
        }
    }
}
