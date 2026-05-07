using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class GameRepository
    {
        public int Create(Game game)
        {
            const string sql = @"INSERT INTO games (created_by, title, slug, short_description, full_description, cover_image, banner_image)
                                 VALUES (@created_by, @title, @slug, @short_description, @full_description, @cover_image, @banner_image);
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@created_by", game.CreatedBy);
            cmd.Parameters.AddWithValue("@title", game.Title);
            cmd.Parameters.AddWithValue("@slug", game.Slug);
            cmd.Parameters.AddWithValue("@short_description", (object)game.ShortDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@full_description", (object)game.FullDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cover_image", (object)game.CoverImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_image", (object)game.BannerImage ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Game GetById(int id)
        {
            const string sql = @"SELECT game_id, created_by, title, slug, short_description, full_description, cover_image, banner_image, created_at
                                 FROM games WHERE game_id = @id";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new Game
            {
                GameId = rdr.GetInt32("game_id"),
                CreatedBy = rdr.GetInt32("created_by"),
                Title = rdr.GetString("title"),
                Slug = rdr.GetString("slug"),
                ShortDescription = rdr.IsDBNull(rdr.GetOrdinal("short_description")) ? null : rdr.GetString("short_description"),
                FullDescription = rdr.IsDBNull(rdr.GetOrdinal("full_description")) ? null : rdr.GetString("full_description"),
                CoverImage = rdr.IsDBNull(rdr.GetOrdinal("cover_image")) ? null : rdr.GetString("cover_image"),
                BannerImage = rdr.IsDBNull(rdr.GetOrdinal("banner_image")) ? null : rdr.GetString("banner_image"),
                CreatedAt = rdr.GetDateTime("created_at")
            };
        }

        public IEnumerable<Game> GetAll()
        {
            var list = new List<Game>();
            const string sql = @"SELECT game_id, created_by, title, slug, short_description, created_at FROM games ORDER BY created_at DESC";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Game
                {
                    GameId = rdr.GetInt32("game_id"),
                    CreatedBy = rdr.GetInt32("created_by"),
                    Title = rdr.GetString("title"),
                    Slug = rdr.GetString("slug"),
                    ShortDescription = rdr.IsDBNull(rdr.GetOrdinal("short_description")) ? null : rdr.GetString("short_description"),
                    CreatedAt = rdr.GetDateTime("created_at")
                });
            }
            return list;
        }

        public void AddTagRelation(int gameId, int tagId)
        {
            const string sql = @"INSERT IGNORE INTO game_tag_relations (game_id, tag_id) VALUES (@game_id, @tag_id)";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@game_id", gameId);
            cmd.Parameters.AddWithValue("@tag_id", tagId);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<Game> SearchByTitle(string q)
        {
            var list = new List<Game>();
            const string sql = @"SELECT game_id, title, slug, short_description FROM games WHERE title LIKE @q LIMIT 50";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", $"%{q}%");
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Game
                {
                    GameId = rdr.GetInt32("game_id"),
                    Title = rdr.GetString("title"),
                    Slug = rdr.GetString("slug"),
                    ShortDescription = rdr.IsDBNull(rdr.GetOrdinal("short_description")) ? null : rdr.GetString("short_description")
                });
            }
            return list;
        }
    }
}