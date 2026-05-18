using System.Collections.Generic;
using System.Linq;
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
            var sql = ArticleRepository.SummarySelect + @"
INNER JOIN saved_articles s
    ON s.article_id = a.article_id
WHERE s.user_id = @UserId
ORDER BY s.saved_id DESC, a.updated_at DESC, a.views_count DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { UserId = userId });
        }
    }
}
