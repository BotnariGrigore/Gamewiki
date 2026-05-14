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
        public Task<bool> UpdateAsync(Category category) => _repo.UpdateAsync(category);
        public Task<IEnumerable<Category>> GetByGameIdAsync(int gameId) => _repo.GetByGameIdAsync(gameId);
        public Task<IEnumerable<Category>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Category?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<Category?> GetByGameAndNameAsync(int gameId, string categoryName) => _repo.GetByGameAndNameAsync(gameId, categoryName);
        public Task<IEnumerable<WikiArticle>> GetArticlesAsync(int categoryId) => _repo.GetArticlesAsync(categoryId);
        public Task<IEnumerable<Game>> GetGamesByCategoryNameAsync(string categoryName) => _repo.GetGamesByCategoryNameAsync(categoryName);
        public Task<bool> DeleteAsync(int categoryId) => _repo.DeleteAsync(categoryId);
    }
}
