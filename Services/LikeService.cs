using System.Threading.Tasks;
using GameWikiApp.Data;

namespace GameWikiApp.Services
{
    public class LikeService
    {
        private readonly LikeRepository _repo = new();

        public Task<int> GetCountAsync(int articleId) => _repo.GetCountAsync(articleId);
        public Task<bool> HasLikedAsync(int articleId, int userId) => _repo.HasLikedAsync(articleId, userId);
        public Task<bool> ToggleLikeAsync(int articleId, int userId) => _repo.ToggleLikeAsync(articleId, userId);
    }
}
