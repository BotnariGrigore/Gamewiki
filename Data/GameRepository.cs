using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class GameRepository
    {
        public async Task<Game?> GetByIdAsync(int id)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM games WHERE game_id = @Id LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Game>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM games ORDER BY created_at DESC";
            return await conn.QueryAsync<Game>(sql);
        }

        public async Task<int> CreateAsync(Game game)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO games (created_by, title, slug, short_description, full_description, cover_image, banner_image)
                        VALUES (@CreatedBy, @Title, @Slug, @ShortDescription, @FullDescription, @CoverImage, @BannerImage);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, game);
        }

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
