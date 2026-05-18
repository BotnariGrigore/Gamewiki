using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class CategoryRepository
    {
        public async Task<int> CreateAsync(Category category)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO categories (game_id, category_name, description)
VALUES (@GameId, @CategoryName, @Description);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, category);
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
UPDATE categories
SET game_id = @GameId,
    category_name = @CategoryName,
    description = @Description
WHERE category_id = @CategoryId";
            var rows = await conn.ExecuteAsync(sql, category);
            return rows > 0;
        }

        public async Task<IEnumerable<Category>> GetByGameIdAsync(int gameId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT c.category_id AS CategoryId,
       c.game_id AS GameId,
       c.category_name AS CategoryName,
       c.description AS Description,
       g.title AS GameTitle,
       COUNT(ac.article_id) AS ArticleCount
FROM categories c
INNER JOIN games g ON g.game_id = c.game_id
LEFT JOIN article_categories ac ON ac.category_id = c.category_id
WHERE c.game_id = @GameId
GROUP BY c.category_id, c.game_id, c.category_name, c.description, g.title
ORDER BY c.category_name";
            return await conn.QueryAsync<Category>(sql, new { GameId = gameId });
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT c.category_id AS CategoryId,
       c.game_id AS GameId,
       c.category_name AS CategoryName,
       c.description AS Description,
       g.title AS GameTitle,
       COUNT(ac.article_id) AS ArticleCount
FROM categories c
INNER JOIN games g ON g.game_id = c.game_id
LEFT JOIN article_categories ac ON ac.category_id = c.category_id
GROUP BY c.category_id, c.game_id, c.category_name, c.description, g.title
ORDER BY g.title, c.category_name";
            return await conn.QueryAsync<Category>(sql);
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT c.category_id AS CategoryId,
       c.game_id AS GameId,
       c.category_name AS CategoryName,
       c.description AS Description,
       g.title AS GameTitle,
       COUNT(ac.article_id) AS ArticleCount
FROM categories c
INNER JOIN games g ON g.game_id = c.game_id
LEFT JOIN article_categories ac ON ac.category_id = c.category_id
WHERE c.category_id = @Id
GROUP BY c.category_id, c.game_id, c.category_name, c.description, g.title
LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Category>(sql, new { Id = id });
        }

        public async Task<Category?> GetByGameAndNameAsync(int gameId, string categoryName)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT c.category_id AS CategoryId,
       c.game_id AS GameId,
       c.category_name AS CategoryName,
       c.description AS Description,
       g.title AS GameTitle,
       COUNT(ac.article_id) AS ArticleCount
FROM categories c
INNER JOIN games g ON g.game_id = c.game_id
LEFT JOIN article_categories ac ON ac.category_id = c.category_id
WHERE c.game_id = @GameId AND c.category_name = @CategoryName
GROUP BY c.category_id, c.game_id, c.category_name, c.description, g.title
LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Category>(sql, new { GameId = gameId, CategoryName = categoryName });
        }

        public async Task<IEnumerable<WikiArticle>> GetArticlesAsync(int categoryId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = ArticleRepository.SummarySelect + @"
WHERE EXISTS (
    SELECT 1
    FROM article_categories ac
    WHERE ac.article_id = a.article_id
      AND ac.category_id = @CategoryId
)
ORDER BY a.updated_at DESC, a.views_count DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { CategoryId = categoryId });
        }

        public async Task<IEnumerable<Game>> GetGamesByCategoryNameAsync(string categoryName)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT g.game_id AS GameId,
       g.created_by AS CreatedBy,
       g.title AS Title,
       g.slug AS Slug,
       g.short_description AS ShortDescription,
       g.full_description AS FullDescription,
       g.cover_image AS CoverImage,
       g.banner_image AS BannerImage,
       g.created_at AS CreatedAt
FROM games g
INNER JOIN categories c ON c.game_id = g.game_id
WHERE c.category_name = @CategoryName
ORDER BY g.title";
            var games = (await conn.QueryAsync<Game>(sql, new { CategoryName = categoryName })).ToList();
            await LoadGenresAsync(games);
            return games;
        }

        private async Task LoadGenresAsync(List<Game> games)
        {
            if (games.Count == 0)
            {
                return;
            }

            using var conn = DbConnection.GetOpen();
            var ids = games.Select(g => g.GameId).ToList();
            var tagSql = @"
SELECT gtr.game_id AS GameId, t.tag_name AS TagName
FROM game_tag_relations gtr
INNER JOIN game_tags t ON t.tag_id = gtr.tag_id
WHERE gtr.game_id IN @Ids";
            var tagRows = (await conn.QueryAsync<(int GameId, string TagName)>(tagSql, new { Ids = ids })).ToList();

            var lookup = tagRows.GroupBy(r => r.GameId).ToDictionary(g => g.Key, g => g.Select(r => r.TagName).ToList());
            foreach (var game in games)
            {
                if (lookup.TryGetValue(game.GameId, out var genres))
                {
                    game.Genres = genres;
                }
            }
        }

        public async Task<bool> DeleteAsync(int categoryId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync("DELETE FROM categories WHERE category_id = @Id", new { Id = categoryId });
            return rows > 0;
        }
    }
}
