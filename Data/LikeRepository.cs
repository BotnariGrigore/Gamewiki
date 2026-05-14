using System.Threading.Tasks;
using Dapper;

namespace GameWikiApp.Data
{
    public class LikeRepository
    {
        public async Task<int> GetCountAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId",
                new { ArticleId = articleId });
        }

        public async Task<bool> HasLikedAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId AND user_id = @UserId",
                new { ArticleId = articleId, UserId = userId });
            return count > 0;
        }

        public async Task<bool> ToggleLikeAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId AND user_id = @UserId",
                    new { ArticleId = articleId, UserId = userId },
                    tran) > 0;

                if (exists)
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM article_likes WHERE article_id = @ArticleId AND user_id = @UserId",
                        new { ArticleId = articleId, UserId = userId },
                        tran);
                }
                else
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO article_likes (article_id, user_id) VALUES (@ArticleId, @UserId)",
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
    }
}
