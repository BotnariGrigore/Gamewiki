using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Helpers;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ArticleRepository
    {
        private const string BaseSelect = @"
SELECT a.article_id AS ArticleId,
       a.game_id AS GameId,
       a.author_id AS AuthorId,
       a.title AS Title,
       a.slug AS Slug,
       a.summary AS Summary,
       a.content AS Content,
       a.cover_image AS CoverImage,
       a.views_count AS ViewsCount,
       COALESCE(lk.like_count, 0) AS LikeCount,
       COALESCE(cm.comment_count, 0) AS CommentCount,
       a.is_published AS IsPublished,
       a.created_at AS CreatedAt,
       a.updated_at AS UpdatedAt,
       g.title AS GameTitle,
       u.username AS AuthorUsername
FROM wiki_articles a
INNER JOIN games g ON g.game_id = a.game_id
INNER JOIN users u ON u.user_id = a.author_id
LEFT JOIN (
    SELECT article_id, COUNT(*) AS like_count
    FROM article_likes
    GROUP BY article_id
) lk ON lk.article_id = a.article_id
LEFT JOIN (
    SELECT article_id, COUNT(*) AS comment_count
    FROM article_comments
    GROUP BY article_id
) cm ON cm.article_id = a.article_id";

        public async Task<WikiArticle?> GetByIdAsync(int id)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + " WHERE a.article_id = @Id LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<WikiArticle>(sql, new { Id = id });
        }

        public async Task<WikiArticle?> GetBySlugAsync(string slug)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + " WHERE a.slug = @Slug LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<WikiArticle>(sql, new { Slug = slug });
        }

        public async Task<IEnumerable<WikiArticle>> GetByGameIdAsync(int gameId, bool onlyPublished = true)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + @"
WHERE a.game_id = @GameId
  AND (@OnlyPublished = 0 OR a.is_published = 1)
ORDER BY a.updated_at DESC, a.views_count DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { GameId = gameId, OnlyPublished = onlyPublished ? 1 : 0 });
        }

        public async Task<IEnumerable<WikiArticle>> SearchAsync(string query, bool onlyPublished = true)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + @"
WHERE (a.title LIKE @Query OR a.summary LIKE @Query OR a.content LIKE @Query)
  AND (@OnlyPublished = 0 OR a.is_published = 1)
ORDER BY a.updated_at DESC, a.views_count DESC";
            return await conn.QueryAsync<WikiArticle>(sql, new { Query = "%" + query.Trim() + "%", OnlyPublished = onlyPublished ? 1 : 0 });
        }

        public async Task<IEnumerable<WikiArticle>> GetPopularAsync(int limit = 10)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + @"
WHERE a.is_published = 1
ORDER BY a.views_count DESC, a.updated_at DESC
LIMIT @Limit";
            return await conn.QueryAsync<WikiArticle>(sql, new { Limit = limit });
        }

        public async Task<IEnumerable<WikiArticle>> GetRecentAsync(int limit = 10)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + @"
WHERE a.is_published = 1
ORDER BY a.updated_at DESC
LIMIT @Limit";
            return await conn.QueryAsync<WikiArticle>(sql, new { Limit = limit });
        }

        public async Task<IEnumerable<WikiArticle>> GetRelatedAsync(int articleId, int gameId, int limit = 6)
        {
            using var conn = DbConnection.GetOpen();
            var sql = BaseSelect + @"
WHERE a.game_id = @GameId
  AND a.article_id <> @ArticleId
  AND a.is_published = 1
ORDER BY a.views_count DESC, a.updated_at DESC
LIMIT @Limit";
            return await conn.QueryAsync<WikiArticle>(sql, new { ArticleId = articleId, GameId = gameId, Limit = limit });
        }

        public async Task<IEnumerable<ArticleLink>> GetLinkedArticlesAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT al.link_id AS LinkId,
       al.from_article_id AS FromArticleId,
       al.to_article_id AS ToArticleId,
       al.link_text AS LinkText,
       wa.title AS TargetTitle,
       wa.slug AS TargetSlug,
       g.title AS TargetGameTitle
FROM article_links al
INNER JOIN wiki_articles wa ON wa.article_id = al.to_article_id
INNER JOIN games g ON g.game_id = wa.game_id
WHERE al.from_article_id = @ArticleId
UNION ALL
SELECT al.link_id AS LinkId,
       al.to_article_id AS FromArticleId,
       al.from_article_id AS ToArticleId,
       al.link_text AS LinkText,
       wa.title AS TargetTitle,
       wa.slug AS TargetSlug,
       g.title AS TargetGameTitle
FROM article_links al
INNER JOIN wiki_articles wa ON wa.article_id = al.from_article_id
INNER JOIN games g ON g.game_id = wa.game_id
WHERE al.to_article_id = @ArticleId
ORDER BY TargetTitle";
            return await conn.QueryAsync<ArticleLink>(sql, new { ArticleId = articleId });
        }

        public async Task<int> CreateAsync(WikiArticle article)
        {
            using var conn = DbConnection.GetOpen();
            if (string.IsNullOrWhiteSpace(article.Slug))
            {
                article.Slug = SlugGenerator.Generate(article.Title);
            }

            var sql = @"
INSERT INTO wiki_articles (game_id, author_id, title, slug, summary, content, cover_image, is_published)
VALUES (@GameId, @AuthorId, @Title, @Slug, @Summary, @Content, @CoverImage, @IsPublished);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, article);
        }

        public async Task<bool> UpdateAsync(WikiArticle article)
        {
            using var conn = DbConnection.GetOpen();
            if (string.IsNullOrWhiteSpace(article.Slug))
            {
                article.Slug = SlugGenerator.Generate(article.Title);
            }

            var sql = @"
UPDATE wiki_articles
SET game_id = @GameId,
    title = @Title,
    slug = @Slug,
    summary = @Summary,
    content = @Content,
    cover_image = @CoverImage,
    is_published = @IsPublished
WHERE article_id = @ArticleId";
            var rows = await conn.ExecuteAsync(sql, article);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync("DELETE FROM wiki_articles WHERE article_id = @Id", new { Id = articleId });
            return rows > 0;
        }

        public async Task<bool> IncrementViewsAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync("UPDATE wiki_articles SET views_count = views_count + 1 WHERE article_id = @Id", new { Id = articleId });
            return rows > 0;
        }

        public async Task<bool> TrackViewOnceAsync(int articleId, int userId)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                var inserted = await conn.ExecuteAsync(
                    @"INSERT IGNORE INTO page_views (page_type, page_id, user_id)
                      VALUES ('article', @ArticleId, @UserId)",
                    new { ArticleId = articleId, UserId = userId },
                    tran) > 0;

                if (inserted)
                {
                    await conn.ExecuteAsync(
                        "UPDATE wiki_articles SET views_count = views_count + 1 WHERE article_id = @ArticleId",
                        new { ArticleId = articleId },
                        tran);
                }

                tran.Commit();
                return inserted;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<Dictionary<string, int>> ResolveTitlesToIdsAsync(int gameId, IEnumerable<string> titles)
        {
            using var conn = DbConnection.GetOpen();
            var titleList = titles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (titleList.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            var placeholders = string.Join(",", titleList.Select((_, i) => $"@t{i}"));
            var parameters = new DynamicParameters();
            for (var i = 0; i < titleList.Count; i++)
            {
                parameters.Add($"@t{i}", titleList[i]);
            }

            parameters.Add("@GameId", gameId);

            var sql = $@"
SELECT title, article_id
FROM wiki_articles
WHERE game_id = @GameId
  AND title IN ({placeholders})
  AND is_published = 1";

            var rows = (await conn.QueryAsync<(string Title, int ArticleId)>(sql, parameters)).ToList();

            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                result[row.Title] = row.ArticleId;
            }

            return result;
        }

        public async Task<bool> ReplaceLinksAsync(int fromArticleId, IEnumerable<(int ToArticleId, string LinkText)> links)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(
                    "DELETE FROM article_links WHERE from_article_id = @FromArticleId",
                    new { FromArticleId = fromArticleId },
                    tran);

                foreach (var link in links)
                {
                    if (string.IsNullOrWhiteSpace(link.LinkText))
                    {
                        continue;
                    }

                    await conn.ExecuteAsync(
                        @"INSERT INTO article_links (from_article_id, to_article_id, link_text)
                          VALUES (@FromArticleId, @ToArticleId, @LinkText)",
                        new { FromArticleId = fromArticleId, ToArticleId = link.ToArticleId, LinkText = link.LinkText.Trim() },
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

        public async Task<IEnumerable<int>> GetCategoryIdsByArticleIdAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = "SELECT category_id FROM article_categories WHERE article_id = @ArticleId";
            return await conn.QueryAsync<int>(sql, new { ArticleId = articleId });
        }

        public async Task<bool> SetArticleCategoriesAsync(int articleId, IEnumerable<int> categoryIds)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM article_categories WHERE article_id = @ArticleId", new { ArticleId = articleId }, tran);
                foreach (var categoryId in categoryIds)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO article_categories (article_id, category_id) VALUES (@ArticleId, @CategoryId)",
                        new { ArticleId = articleId, CategoryId = categoryId },
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
