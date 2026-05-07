using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class ArticleRepository
    {
        public int Create(WikiArticle article)
        {
            const string sql = @"INSERT INTO wiki_articles (game_id, author_id, title, slug, summary, content, cover_image, is_published)
                                 VALUES (@game_id, @author_id, @title, @slug, @summary, @content, @cover_image, @is_published);
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@game_id", article.GameId);
            cmd.Parameters.AddWithValue("@author_id", article.AuthorId);
            cmd.Parameters.AddWithValue("@title", article.Title);
            cmd.Parameters.AddWithValue("@slug", article.Slug);
            cmd.Parameters.AddWithValue("@summary", (object)article.Summary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@content", article.Content);
            cmd.Parameters.AddWithValue("@cover_image", (object)article.CoverImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_published", article.IsPublished);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public WikiArticle GetById(int id)
        {
            const string sql = @"SELECT article_id, game_id, author_id, title, slug, summary, content, cover_image, views_count, is_published, created_at, updated_at
                                 FROM wiki_articles WHERE article_id = @id";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new WikiArticle
            {
                ArticleId = rdr.GetInt32("article_id"),
                GameId = rdr.GetInt32("game_id"),
                AuthorId = rdr.GetInt32("author_id"),
                Title = rdr.GetString("title"),
                Slug = rdr.GetString("slug"),
                Summary = rdr.IsDBNull(rdr.GetOrdinal("summary")) ? null : rdr.GetString("summary"),
                Content = rdr.GetString("content"),
                CoverImage = rdr.IsDBNull(rdr.GetOrdinal("cover_image")) ? null : rdr.GetString("cover_image"),
                ViewsCount = rdr.GetInt32("views_count"),
                IsPublished = rdr.GetBoolean("is_published"),
                CreatedAt = rdr.GetDateTime("created_at"),
                UpdatedAt = rdr.IsDBNull(rdr.GetOrdinal("updated_at")) ? (DateTime?)null : rdr.GetDateTime("updated_at")
            };
        }

        public WikiArticle GetBySlug(string slug)
        {
            const string sql = @"SELECT article_id, game_id, author_id, title, slug, summary, content, cover_image, views_count, is_published, created_at, updated_at
                                 FROM wiki_articles WHERE slug = @slug";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@slug", slug);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new WikiArticle
            {
                ArticleId = rdr.GetInt32("article_id"),
                GameId = rdr.GetInt32("game_id"),
                AuthorId = rdr.GetInt32("author_id"),
                Title = rdr.GetString("title"),
                Slug = rdr.GetString("slug"),
                Summary = rdr.IsDBNull(rdr.GetOrdinal("summary")) ? null : rdr.GetString("summary"),
                Content = rdr.GetString("content"),
                CoverImage = rdr.IsDBNull(rdr.GetOrdinal("cover_image")) ? null : rdr.GetString("cover_image"),
                ViewsCount = rdr.GetInt32("views_count"),
                IsPublished = rdr.GetBoolean("is_published"),
                CreatedAt = rdr.GetDateTime("created_at"),
                UpdatedAt = rdr.IsDBNull(rdr.GetOrdinal("updated_at")) ? (DateTime?)null : rdr.GetDateTime("updated_at")
            };
        }

        public bool Update(WikiArticle article)
        {
            const string sql = @"UPDATE wiki_articles SET title=@title, slug=@slug, summary=@summary, content=@content, cover_image=@cover_image, is_published=@is_published
                                 WHERE article_id=@article_id";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", article.Title);
            cmd.Parameters.AddWithValue("@slug", article.Slug);
            cmd.Parameters.AddWithValue("@summary", (object)article.Summary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@content", article.Content);
            cmd.Parameters.AddWithValue("@cover_image", (object)article.CoverImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_published", article.IsPublished);
            cmd.Parameters.AddWithValue("@article_id", article.ArticleId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public void AddRevision(int articleId, int editedBy, string oldContent)
        {
            const string sql = @"INSERT INTO article_revisions (article_id, edited_by, old_content) VALUES (@article_id, @edited_by, @old_content)";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@article_id", articleId);
            cmd.Parameters.AddWithValue("@edited_by", editedBy);
            cmd.Parameters.AddWithValue("@old_content", oldContent);
            cmd.ExecuteNonQuery();
        }

        public int AddComment(int articleId, int userId, string commentText)
        {
            const string sql = @"INSERT INTO article_comments (article_id, user_id, comment_text) VALUES (@article_id, @user_id, @comment_text);
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@article_id", articleId);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@comment_text", commentText);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public IEnumerable<Comment> GetComments(int articleId)
        {
            var list = new List<Comment>();
            const string sql = @"SELECT comment_id, user_id, comment_text, created_at FROM article_comments WHERE article_id = @article_id ORDER BY created_at ASC";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@article_id", articleId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Comment
                {
                    CommentId = rdr.GetInt32("comment_id"),
                    UserId = rdr.GetInt32("user_id"),
                    CommentText = rdr.GetString("comment_text"),
                    CreatedAt = rdr.GetDateTime("created_at")
                });
            }
            return list;
        }
    }
}