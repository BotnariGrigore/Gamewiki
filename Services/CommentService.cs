using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class CommentService
    {
        private readonly CommentRepository _repo = new();

        public Task<int> CreateAsync(Comment comment) => _repo.CreateAsync(comment);
        public Task<IEnumerable<ArticleComment>> GetByArticleIdAsync(int articleId) => _repo.GetByArticleIdAsync(articleId);
        public Task<bool> DeleteAsync(int commentId) => _repo.DeleteAsync(commentId);
    }
}
