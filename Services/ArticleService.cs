using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class ArticleService
    {
        private readonly ArticleRepository _repo = new();

        public Task<WikiArticle?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<WikiArticle?> GetBySlugAsync(string slug) => _repo.GetBySlugAsync(slug);
        public Task<IEnumerable<WikiArticle>> GetByGameIdAsync(int gameId, bool onlyPublished = true) => _repo.GetByGameIdAsync(gameId, onlyPublished);
        public Task<IEnumerable<WikiArticle>> SearchAsync(string query, bool onlyPublished = true) => _repo.SearchAsync(query, onlyPublished);
        public Task<IEnumerable<WikiArticle>> GetPopularAsync(int limit = 10) => _repo.GetPopularAsync(limit);
        public Task<IEnumerable<WikiArticle>> GetRecentAsync(int limit = 10) => _repo.GetRecentAsync(limit);
        public Task<IEnumerable<WikiArticle>> GetRelatedAsync(int articleId, int gameId, int limit = 6) => _repo.GetRelatedAsync(articleId, gameId, limit);
        public Task<IEnumerable<ArticleLink>> GetLinkedArticlesAsync(int articleId) => _repo.GetLinkedArticlesAsync(articleId);
        public Task<int> CreateAsync(WikiArticle article) => _repo.CreateAsync(article);
        public Task<bool> UpdateAsync(WikiArticle article) => _repo.UpdateAsync(article);
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
        public Task<bool> IncrementViewsAsync(int id) => _repo.IncrementViewsAsync(id);
        public Task<Dictionary<string, int>> ResolveTitlesToIdsAsync(int gameId, IEnumerable<string> titles) => _repo.ResolveTitlesToIdsAsync(gameId, titles);
        public Task<bool> ReplaceLinksAsync(int fromArticleId, IEnumerable<(int ToArticleId, string LinkText)> links) => _repo.ReplaceLinksAsync(fromArticleId, links);
        public Task<IEnumerable<int>> GetCategoryIdsAsync(int articleId) => _repo.GetCategoryIdsByArticleIdAsync(articleId);
        public Task<bool> SetCategoriesAsync(int articleId, IEnumerable<int> categoryIds) => _repo.SetArticleCategoriesAsync(articleId, categoryIds);
    }
}
