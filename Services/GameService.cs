using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class GameService
    {
        private readonly GameRepository _repo = new();

        public Task<Game?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public Task<IEnumerable<Game>> GetAllAsync() => _repo.GetAllAsync();

        public Task<int> CreateAsync(Game game) => _repo.CreateAsync(game);

        public Task<bool> UpdateAsync(Game game) => _repo.UpdateAsync(game);

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}