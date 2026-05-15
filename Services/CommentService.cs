using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class CommentService
    {
        private readonly CommentRepository _repo = new();
        private readonly ArticleRepository _articles = new();
        private readonly UserRepository _users = new();
        private readonly NotificationService _notifications = new();

        public async Task<int> CreateAsync(Comment comment)
        {
            var id = await _repo.CreateAsync(comment);
            if (id <= 0)
            {
                return id;
            }

            var article = await _articles.GetByIdAsync(comment.ArticleId);
            if (article == null || article.AuthorId == comment.UserId)
            {
                return id;
            }

            var actor = await _users.GetByIdAsync(comment.UserId);
            var recipient = await _users.GetByIdAsync(article.AuthorId);
            if (actor != null && recipient != null)
            {
                await _notifications.NotifyCommentAsync(actor, recipient, article, comment.CommentText);
            }

            return id;
        }

        public Task<IEnumerable<ArticleComment>> GetByArticleIdAsync(int articleId) => _repo.GetByArticleIdAsync(articleId);
        public Task<bool> DeleteAsync(int commentId) => _repo.DeleteAsync(commentId);
    }
}
