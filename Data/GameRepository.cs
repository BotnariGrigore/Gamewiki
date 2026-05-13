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

        public async Task<bool> UpdateAsync(Game game)
        {
            using var conn = GetOpen();
            var sql = @"UPDATE games SET title = @Title, slug = @Slug, short_description = @ShortDescription, full_description = @FullDescription, cover_image = @CoverImage, banner_image = @BannerImage
                        WHERE game_id = @GameId";
            var res = await conn.ExecuteAsync(sql, game);
            return res > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = GetOpen();
            var sql = "DELETE FROM games WHERE game_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = id });
            return res > 0;
        }

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}