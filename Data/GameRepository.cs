using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Helpers;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class GameRepository
    {
        private const string BaseSelect = @"
SELECT g.game_id AS GameId,
       g.created_by AS CreatedBy,
       g.title AS Title,
       g.slug AS Slug,
       g.short_description AS ShortDescription,
       g.full_description AS FullDescription,
       g.cover_image AS CoverImage,
       g.banner_image AS BannerImage,
       g.created_at AS CreatedAt,
       COALESCE(stats.total_views, 0) AS PopularityScore,
       COALESCE(stats.article_count, 0) AS ArticleCount
FROM games g
LEFT JOIN (
    SELECT game_id,
           SUM(views_count) AS total_views,
           COUNT(*) AS article_count
    FROM wiki_articles
    GROUP BY game_id
) stats ON stats.game_id = g.game_id";

        public async Task<Game?> GetByIdAsync(int id)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + " WHERE g.game_id = @Id LIMIT 1";
            var game = await conn.QueryFirstOrDefaultAsync<Game>(sql, new { Id = id });
            if (game != null)
            {
                game.Genres = (await GetGenresAsync(game.GameId)).ToList();
            }
            return game;
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + " ORDER BY g.created_at DESC, g.title ASC";
            var games = (await conn.QueryAsync<Game>(sql)).ToList();
            await LoadGenresAsync(games);
            return games;
        }

        public async Task<IEnumerable<Game>> GetPopularAsync(int limit = 10)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + " ORDER BY PopularityScore DESC, g.title ASC LIMIT @Limit";
            var games = (await conn.QueryAsync<Game>(sql, new { Limit = limit })).ToList();
            await LoadGenresAsync(games);
            return games;
        }

        public async Task<IEnumerable<Game>> SearchAsync(string query)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + @"
WHERE g.title LIKE @Query
   OR g.short_description LIKE @Query
   OR g.full_description LIKE @Query
ORDER BY g.title ASC";
            var games = (await conn.QueryAsync<Game>(sql, new { Query = "%" + query.Trim() + "%" })).ToList();
            await LoadGenresAsync(games);
            return games;
        }

        public async Task<int> CreateAsync(Game game)
        {
            using var conn = DbConnection.GetOpen();
            if (string.IsNullOrWhiteSpace(game.Slug))
            {
                game.Slug = SlugGenerator.Generate(game.Title);
            }

            var sql = @"
INSERT INTO games (created_by, title, slug, short_description, full_description, cover_image, banner_image)
VALUES (@CreatedBy, @Title, @Slug, @ShortDescription, @FullDescription, @CoverImage, @BannerImage);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, game);
        }

        public async Task<bool> UpdateAsync(Game game)
        {
            using var conn = DbConnection.GetOpen();
            if (string.IsNullOrWhiteSpace(game.Slug))
            {
                game.Slug = SlugGenerator.Generate(game.Title);
            }

            var sql = @"
UPDATE games
SET title = @Title,
    slug = @Slug,
    short_description = @ShortDescription,
    full_description = @FullDescription,
    cover_image = @CoverImage,
    banner_image = @BannerImage
WHERE game_id = @GameId";
            var res = await conn.ExecuteAsync(sql, game);
            return res > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = DbConnection.GetOpen();
            var res = await conn.ExecuteAsync("DELETE FROM games WHERE game_id = @Id", new { Id = id });
            return res > 0;
        }

        public async Task<bool> TrackViewOnceAsync(int gameId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                var inserted = await conn.ExecuteAsync(
                    @"INSERT IGNORE INTO page_views (page_type, page_id, user_id)
                      VALUES ('game', @GameId, @UserId)",
                    new { GameId = gameId, UserId = userId },
                    tran) > 0;

                tran.Commit();
                return inserted;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        private async Task LoadGenresAsync(List<Game> games)
        {
            if (games.Count == 0) return;

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

        private async Task<IEnumerable<string>> GetGenresAsync(int gameId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT t.tag_name
FROM game_tag_relations gtr
INNER JOIN game_tags t ON t.tag_id = gtr.tag_id
WHERE gtr.game_id = @GameId
ORDER BY t.tag_name";
            return await conn.QueryAsync<string>(sql, new { GameId = gameId });
        }
    }
}
