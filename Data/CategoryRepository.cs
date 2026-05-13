using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;
using System.Data;

namespace GameWikiApp.Data
{
    public class CategoryRepository
    {
        public async Task<int> CreateAsync(Category category)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO categories (game_id, category_name, description)
                        VALUES (@GameId, @CategoryName, @Description);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, category);
        }

        public async Task<IEnumerable<Category>> GetByGameIdAsync(int gameId)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM categories WHERE game_id = @GameId ORDER BY category_name";
            return await conn.QueryAsync<Category>(sql, new { GameId = gameId });
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM categories ORDER BY category_name";
            return await conn.QueryAsync<Category>(sql);
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM categories WHERE category_id = @Id LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Category>(sql, new { Id = id });
        }

        public async Task<IEnumerable<WikiArticle>> GetArticlesAsync(int categoryId)
        {
            using var conn = GetOpen();
            var sql = @"SELECT wa.* FROM wiki_articles wa
                        JOIN article_categories ac ON wa.article_id = ac.article_id
                        WHERE ac.category_id = @Cid
                        ORDER BY wa.created_at DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { Cid = categoryId });
        }

        public async Task<IEnumerable<PopularCategory>> GetPopularCategoriesAsync(int limit = 0)
        {
            using var conn = GetOpen();
            var sql = @"SELECT category_name AS CategoryName, COUNT(DISTINCT game_id) AS GameCount
                        FROM categories
                        GROUP BY category_name
                        ORDER BY GameCount DESC";
            if (limit > 0) sql += " LIMIT @Limit";
            return await conn.QueryAsync<PopularCategory>(sql, new { Limit = limit });
        }

        public async Task<IEnumerable<Game>> GetGamesByCategoryNameAsync(string categoryName)
        {
            using var conn = GetOpen();
            var sql = @"SELECT g.* FROM games g
                        JOIN categories c ON c.game_id = g.game_id
                        WHERE c.category_name = @Name
                        ORDER BY g.title";
            return await conn.QueryAsync<Game>(sql, new { Name = categoryName });
        }

        public async Task<bool> AddCategoryToGamesAsync(string categoryName, string? description, IEnumerable<int> gameIds)
        {
            using var conn = GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                foreach (var gid in gameIds)
                {
                    var sql = @"INSERT INTO categories (game_id, category_name, description)
                                VALUES (@Gid, @Name, @Desc)
                                ON DUPLICATE KEY UPDATE description = COALESCE(description, @Desc);";
                    await conn.ExecuteAsync(sql, new { Gid = gid, Name = categoryName, Desc = description }, tran);
                }
                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                return false;
            }
        }

        public async Task<bool> RemoveCategoryFromGameAsync(string categoryName, int gameId)
        {
            using var conn = GetOpen();
            var sql = "DELETE FROM categories WHERE category_name = @Name AND game_id = @Gid";
            var res = await conn.ExecuteAsync(sql, new { Name = categoryName, Gid = gameId });
            return res > 0;
        }

        private IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
