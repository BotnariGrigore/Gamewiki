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

        public Task<IEnumerable<WikiArticle>> SearchAsync(string query) => _repo.SearchAsync(query);

        public Task<int> CreateAsync(WikiArticle article) => _repo.CreateAsync(article);
    }
}
