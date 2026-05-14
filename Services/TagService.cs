using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class TagService
    {
        private readonly TagRepository _repo = new();

        public Task<IEnumerable<GameTag>> GetAllAsync() => _repo.GetAllAsync();
        public Task<GameTag?> GetByNameAsync(string tagName) => _repo.GetByNameAsync(tagName);
        public Task<int> CreateAsync(string tagName) => _repo.CreateAsync(tagName);
        public Task<bool> DeleteAsync(int tagId) => _repo.DeleteAsync(tagId);
        public Task<IEnumerable<Game>> GetGamesByTagNameAsync(string tagName) => _repo.GetGamesByTagNameAsync(tagName);
        public Task<IEnumerable<int>> GetTagIdsByGameIdAsync(int gameId) => _repo.GetTagIdsByGameIdAsync(gameId);
        public Task<bool> SetGameTagsAsync(int gameId, IEnumerable<int> tagIds) => _repo.SetGameTagsAsync(gameId, tagIds);
    }
}
