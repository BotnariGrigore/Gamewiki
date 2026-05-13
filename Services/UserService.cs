using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class UserService
    {
        private readonly UserRepository _repo = new();

        public Task<User?> GetByUsernameAsync(string username) => _repo.GetByUsernameAsync(username);
        public Task<User?> GetByEmailAsync(string email) => _repo.GetByEmailAsync(email);
        public Task<User?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<IEnumerable<User>> GetAllAsync() => _repo.GetAllAsync();
        public Task<int> CreateAsync(User user) => _repo.CreateAsync(user);
        public Task<bool> UpdateAsync(User user) => _repo.UpdateAsync(user);
        public Task<bool> UpdateRoleAsync(int userId, int roleId) => _repo.UpdateRoleAsync(userId, roleId);
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        public async Task<bool> SetProfileImageAsync(int userId, string imageUrl)
        {
            try
            {
                using var conn = DbConnection.GetOpen();
                var sql = "UPDATE users SET profile_image = @Image WHERE user_id = @UserId";
                var res = await conn.ExecuteAsync(sql, new { UserId = userId, Image = imageUrl });
                if (res > 0)
                {
                    var user = await _repo.GetByIdAsync(userId);
                    if (user != null) await _repo.UpdateAsync(user);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}