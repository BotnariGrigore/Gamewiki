using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ArticleImageRepository
    {
        public async Task<IEnumerable<ArticleImage>> GetByArticleIdAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
SELECT image_id AS ImageId,
       article_id AS ArticleId,
       uploaded_by AS UploadedBy,
       image_url AS ImageUrl,
       alt_text AS AltText
FROM article_images
WHERE article_id = @ArticleId
ORDER BY image_id DESC";
            return await conn.QueryAsync<ArticleImage>(sql, new { ArticleId = articleId });
        }

        public async Task<int> CreateAsync(ArticleImage image)
        {
            using var conn = DbConnection.GetOpen();
            var sql = @"
INSERT INTO article_images (article_id, uploaded_by, image_url, alt_text)
VALUES (@ArticleId, @UploadedBy, @ImageUrl, @AltText);
SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, image);
        }

        public async Task<bool> DeleteByArticleIdAsync(int articleId)
        {
            using var conn = DbConnection.GetOpen();
            var rows = await conn.ExecuteAsync("DELETE FROM article_images WHERE article_id = @ArticleId", new { ArticleId = articleId });
            return rows > 0;
        }

        public async Task<bool> ReplaceAsync(int articleId, int uploadedBy, IEnumerable<string> imageUrls)
        {
            using var conn = DbConnection.GetOpen();
            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM article_images WHERE article_id = @ArticleId", new { ArticleId = articleId }, tran);

                foreach (var url in imageUrls)
                {
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        continue;
                    }

                    await conn.ExecuteAsync(
                        "INSERT INTO article_images (article_id, uploaded_by, image_url, alt_text) VALUES (@ArticleId, @UploadedBy, @ImageUrl, @AltText)",
                        new { ArticleId = articleId, UploadedBy = uploadedBy, ImageUrl = url.Trim(), AltText = (string?)null },
                        tran);
                }

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}
