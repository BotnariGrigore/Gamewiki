using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class FriendRepository
    {
        public async Task<int> AddAsync(Friend friend)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO friends (user_id, friend_id, status)
                        VALUES (@UserId, @FriendId, @Status);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, new { friend.UserId, friend.FriendId, Status = friend.Status.ToString().ToLower() });
        }

        public async Task<IEnumerable<Friend>> GetByUserIdAsync(int userId)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM friends WHERE user_id = @Uid OR friend_id = @Uid";
            return await conn.QueryAsync<Friend>(sql, new { Uid = userId });
        }

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
