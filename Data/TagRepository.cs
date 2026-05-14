using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class TagRepository
    {
        public async Task<IEnumerable<GameTag>> GetAllAsync()
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT t.tag_id AS TagId,
       t.tag_name AS TagName,
       COUNT(gtr.game_id) AS GameCount
FROM game_tags t
LEFT JOIN game_tag_relations gtr ON gtr.tag_id = t.tag_id
GROUP BY t.tag_id, t.tag_name
ORDER BY GameCount DESC, t.tag_name ASC";
            return await conn.QueryAsync<GameTag>(sql);
        }

        public async Task<GameTag?> GetByNameAsync(string tagName)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT t.tag_id AS TagId,
       t.tag_name AS TagName,
       COUNT(gtr.game_id) AS GameCount
FROM game_tags t
LEFT JOIN game_tag_relations gtr ON gtr.tag_id = t.tag_id
WHERE t.tag_name = @TagName
GROUP BY t.tag_id, t.tag_name
LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<GameTag>(sql, new { TagName = tagName });
        }

        public async Task<int> CreateAsync(string tagName)
        {
            var normalized = tagName.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return 0;
            }

            var existing = await GetByNameAsync(normalized);
            if (existing != null)
            {
                return existing.TagId;
            }

            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO game_tags (tag_name)
VALUES (@TagName);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, new { TagName = normalized });
        }

        public async Task<bool> DeleteAsync(int tagId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync("DELETE FROM game_tags WHERE tag_id = @TagId", new { TagId = tagId });
            return rows > 0;
        }

        public async Task<IEnumerable<Game>> GetGamesByTagNameAsync(string tagName)
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
       g.created_at AS CreatedAt,
       COALESCE(stats.total_views, 0) AS PopularityScore,
       COALESCE(stats.article_count, 0) AS ArticleCount
FROM games g
INNER JOIN game_tag_relations gtr ON gtr.game_id = g.game_id
INNER JOIN game_tags t ON t.tag_id = gtr.tag_id
LEFT JOIN (
    SELECT game_id,
           SUM(views_count) AS total_views,
           COUNT(*) AS article_count
    FROM wiki_articles
    GROUP BY game_id
) stats ON stats.game_id = g.game_id
WHERE t.tag_name = @TagName
ORDER BY PopularityScore DESC, g.title ASC";
            var games = (await conn.QueryAsync<Game>(sql, new { TagName = tagName })).ToList();
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

        public async Task<IEnumerable<int>> GetTagIdsByGameIdAsync(int gameId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT tag_id FROM game_tag_relations WHERE game_id = @GameId";
            return await conn.QueryAsync<int>(sql, new { GameId = gameId });
        }

        public async Task<bool> SetGameTagsAsync(int gameId, IEnumerable<int> tagIds)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(
                    "DELETE FROM game_tag_relations WHERE game_id = @GameId",
                    new { GameId = gameId },
                    tran);

                foreach (var tagId in tagIds.Distinct())
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO game_tag_relations (game_id, tag_id) VALUES (@GameId, @TagId)",
                        new { GameId = gameId, TagId = tagId },
                        tran);
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
    }
}
