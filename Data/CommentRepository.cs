using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class CommentRepository
    {
        public async Task<int> CreateAsync(Comment comment)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO article_comments (article_id, user_id, comment_text)
VALUES (@ArticleId, @UserId, @CommentText);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, comment);
        }

        public async Task<IEnumerable<ArticleComment>> GetByArticleIdAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT c.comment_id AS CommentId,
       c.article_id AS ArticleId,
       c.user_id AS UserId,
       u.username AS Username,
       u.profile_image AS ProfileImage,
       c.comment_text AS CommentText,
       c.created_at AS CreatedAt
FROM article_comments c
INNER JOIN users u ON u.user_id = c.user_id
WHERE c.article_id = @ArticleId
ORDER BY c.created_at ASC";
            return await conn.QueryAsync<ArticleComment>(sql, new { ArticleId = articleId });
        }

        public async Task<bool> DeleteAsync(int commentId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync("DELETE FROM article_comments WHERE comment_id = @Id", new { Id = commentId });
            return rows > 0;
        }
    }
}
