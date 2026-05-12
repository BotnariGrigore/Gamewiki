using System.Collections.Generic;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class FriendService
    {
        private readonly FriendRepository _repo = new();

        public Task<int> AddFriendAsync(Friend f) => _repo.AddAsync(f);

        public Task<IEnumerable<Friend>> GetFriendsAsync(int userId) => _repo.GetByUserIdAsync(userId);
    }
}
