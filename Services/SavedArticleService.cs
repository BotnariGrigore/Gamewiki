using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class SavedArticleService
    {
        private readonly SavedArticleRepository _repo = new();

        public Task<int> GetCountAsync(int articleId) => _repo.GetCountAsync(articleId);
        public Task<bool> IsSavedAsync(int articleId, int userId) => _repo.IsSavedAsync(articleId, userId);
        public Task<bool> ToggleSaveAsync(int articleId, int userId) => _repo.ToggleSaveAsync(articleId, userId);
        public Task<IEnumerable<WikiArticle>> GetByUserIdAsync(int userId) => _repo.GetByUserIdAsync(userId);
    }
}
