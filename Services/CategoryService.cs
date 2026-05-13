using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class CategoryService
    {
        private readonly CategoryRepository _repo = new();

        public Task<int> CreateAsync(Category category) => _repo.CreateAsync(category);

        public Task<IEnumerable<Category>> GetByGameIdAsync(int gameId) => _repo.GetByGameIdAsync(gameId);

        public Task<IEnumerable<Category>> GetAllAsync() => _repo.GetAllAsync();

        public Task<IEnumerable<WikiArticle>> GetArticlesAsync(int categoryId) => _repo.GetArticlesAsync(categoryId);

        public Task<IEnumerable<PopularCategory>> GetPopularCategoriesAsync(int limit = 0) => _repo.GetPopularCategoriesAsync(limit);

        public Task<IEnumerable<Game>> GetGamesByCategoryNameAsync(string categoryName) => _repo.GetGamesByCategoryNameAsync(categoryName);

        public Task<bool> AddCategoryToGamesAsync(string categoryName, string? description, IEnumerable<int> gameIds) => _repo.AddCategoryToGamesAsync(categoryName, description, gameIds);

        public Task<bool> RemoveCategoryFromGameAsync(string categoryName, int gameId) => _repo.RemoveCategoryFromGameAsync(categoryName, gameId);
    }
}
