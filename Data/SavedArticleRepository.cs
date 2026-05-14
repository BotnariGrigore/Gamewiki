using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class SavedArticleRepository
    {
        public async Task<int> GetCountAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM saved_articles WHERE article_id = @ArticleId",
                new { ArticleId = articleId });
        }

        public async Task<bool> IsSavedAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM saved_articles WHERE article_id = @ArticleId AND user_id = @UserId",
                new { ArticleId = articleId, UserId = userId });
            return count > 0;
        }

        public async Task<bool> ToggleSaveAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM saved_articles WHERE article_id = @ArticleId AND user_id = @UserId",
                    new { ArticleId = articleId, UserId = userId },
                    tran) > 0;

                if (exists)
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM saved_articles WHERE article_id = @ArticleId AND user_id = @UserId",
                        new { ArticleId = articleId, UserId = userId },
                        tran);
                }
                else
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO saved_articles (article_id, user_id) VALUES (@ArticleId, @UserId)",
                        new { ArticleId = articleId, UserId = userId },
                        tran);
                }

                tran.Commit();
                return !exists;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<WikiArticle>> GetByUserIdAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT a.article_id AS ArticleId,
       a.game_id AS GameId,
       a.author_id AS AuthorId,
       a.title AS Title,
       a.slug AS Slug,
       a.summary AS Summary,
       a.content AS Content,
       a.cover_image AS CoverImage,
       a.views_count AS ViewsCount,
       a.is_published AS IsPublished,
       a.created_at AS CreatedAt,
       a.updated_at AS UpdatedAt,
       g.title AS GameTitle,
       u.username AS AuthorUsername
FROM saved_articles s
INNER JOIN wiki_articles a ON a.article_id = s.article_id
INNER JOIN games g ON g.game_id = a.game_id
INNER JOIN users u ON u.user_id = a.author_id
WHERE s.user_id = @UserId
ORDER BY a.updated_at DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { UserId = userId });
        }
    }
}
