using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ArticleRepository
    {
        public async Task<WikiArticle?> GetByIdAsync(int id)
        {
            using var conn = GetOpen();
            var sql = "SELECT * FROM wiki_articles WHERE article_id = @Id LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<WikiArticle>(sql, new { Id = id });
        }

        public async Task<IEnumerable<WikiArticle>> SearchAsync(string query)
        {
            using var conn = GetOpen();
            // Simple LIKE-based search; replace with FULLTEXT MATCH if desired and supported
            var q = "%" + query + "%";
            var sql = "SELECT * FROM wiki_articles WHERE title LIKE @Q OR summary LIKE @Q OR content LIKE @Q ORDER BY created_at DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { Q = q });
        }

        public async Task<int> CreateAsync(WikiArticle article)
        {
            using var conn = GetOpen();
            var sql = @"INSERT INTO wiki_articles (game_id, author_id, title, slug, summary, content, cover_image, is_published)
                        VALUES (@GameId, @AuthorId, @Title, @Slug, @Summary, @Content, @CoverImage, @IsPublished);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, article);
        }

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}
