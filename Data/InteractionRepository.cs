using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class CommentRepository
    {
        public async Task<IEnumerable<CommentWithUser>> GetByArticleIdAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"SELECT c.*, u.username, u.profile_image
                        FROM article_comments c
                        JOIN users u ON c.user_id = u.user_id
                        WHERE c.article_id = @ArticleId
                        ORDER BY c.created_at DESC";
            return await conn.QueryAsync<CommentWithUser>(sql, new { ArticleId = articleId });
        }

        public async Task<int> CreateAsync(Comment comment)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"INSERT INTO article_comments (article_id, user_id, comment_text)
                        VALUES (@ArticleId, @UserId, @CommentText);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, comment);
        }

        public async Task<bool> DeleteAsync(int commentId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "DELETE FROM article_comments WHERE comment_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = commentId });
            return res > 0;
        }
    }

    public class LikeRepository
    {
        public async Task<int> GetCountAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId";
            return await conn.ExecuteScalarAsync<int>(sql, new { ArticleId = articleId });
        }

        public async Task<bool> HasLikedAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT COUNT(*) FROM article_likes WHERE article_id = @ArticleId AND user_id = @UserId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { ArticleId = articleId, UserId = userId });
            return count > 0;
        }

        public async Task<bool> ToggleLikeAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            if (await HasLikedAsync(articleId, userId))
            {
                var sql = "DELETE FROM article_likes WHERE article_id = @ArticleId AND user_id = @UserId";
                var res = await conn.ExecuteAsync(sql, new { ArticleId = articleId, UserId = userId });
                return res > 0;
            }
            else
            {
                var sql = "INSERT INTO article_likes (article_id, user_id) VALUES (@ArticleId, @UserId)";
                var res = await conn.ExecuteAsync(sql, new { ArticleId = articleId, UserId = userId });
                return res > 0;
            }
        }
    }

    public class SavedArticleRepository
    {
        public async Task<bool> IsSavedAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT COUNT(*) FROM saved_articles WHERE article_id = @ArticleId AND user_id = @UserId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { ArticleId = articleId, UserId = userId });
            return count > 0;
        }

        public async Task<bool> ToggleSaveAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            if (await IsSavedAsync(articleId, userId))
            {
                var sql = "DELETE FROM saved_articles WHERE article_id = @ArticleId AND user_id = @UserId";
                var res = await conn.ExecuteAsync(sql, new { ArticleId = articleId, UserId = userId });
                return res > 0;
            }
            else
            {
                var sql = "INSERT INTO saved_articles (article_id, user_id) VALUES (@ArticleId, @UserId)";
                var res = await conn.ExecuteAsync(sql, new { ArticleId = articleId, UserId = userId });
                return res > 0;
            }
        }

        public async Task<IEnumerable<WikiArticle>> GetSavedArticlesAsync(int userId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"SELECT wa.* FROM wiki_articles wa
                        JOIN saved_articles sa ON wa.article_id = sa.article_id
                        WHERE sa.user_id = @UserId
                        ORDER BY wa.created_at DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { UserId = userId });
        }
    }

    public class CommentWithUser
    {
        public int CommentId { get; set; }
        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
    }
}