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

        public async Task<bool> UpdateAsync(WikiArticle article)
        {
            using var conn = GetOpen();
            var sql = @"UPDATE wiki_articles SET game_id = @GameId, title = @Title, slug = @Slug, summary = @Summary, content = @Content, cover_image = @CoverImage, is_published = @IsPublished
                        WHERE article_id = @ArticleId";
            var res = await conn.ExecuteAsync(sql, article);
            return res > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = GetOpen();
            var sql = "DELETE FROM wiki_articles WHERE article_id = @Id";
            var res = await conn.ExecuteAsync(sql, new { Id = id });
            return res > 0;
        }

        public async Task<IEnumerable<int>> GetCategoryIdsByArticleIdAsync(int articleId)
        {
            using var conn = GetOpen();
            var sql = "SELECT category_id FROM article_categories WHERE article_id = @Aid";
            return await conn.QueryAsync<int>(sql, new { Aid = articleId });
        }

        public async Task<bool> SetArticleCategoriesAsync(int articleId, IEnumerable<int> categoryIds)
        {
            using var conn = GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM article_categories WHERE article_id = @Aid", new { Aid = articleId }, tran);
                foreach (var cid in categoryIds)
                {
                    await conn.ExecuteAsync("INSERT INTO article_categories (article_id, category_id) VALUES (@Aid, @Cid)", new { Aid = articleId, Cid = cid }, tran);
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

        private System.Data.IDbConnection GetOpen()
        {
            var c = DbConnection.GetConnection();
            c.Open();
            return c;
        }
    }
}