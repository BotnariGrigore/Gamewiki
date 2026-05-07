using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class UserRepository
    {
        public User GetById(int id)
        {
            const string sql = @"SELECT user_id, role_id, username, email, password_hash, profile_image, bio, is_online, last_seen, created_at
                                 FROM users WHERE user_id = @id";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new User
            {
                UserId = rdr.GetInt32("user_id"),
                RoleId = rdr.GetInt32("role_id"),
                Username = rdr.GetString("username"),
                Email = rdr.GetString("email"),
                PasswordHash = rdr.GetString("password_hash"),
                ProfileImage = rdr.IsDBNull(rdr.GetOrdinal("profile_image")) ? null : rdr.GetString("profile_image"),
                Bio = rdr.IsDBNull(rdr.GetOrdinal("bio")) ? null : rdr.GetString("bio"),
                IsOnline = rdr.GetBoolean("is_online"),
                LastSeen = rdr.IsDBNull(rdr.GetOrdinal("last_seen")) ? (DateTime?)null : rdr.GetDateTime("last_seen"),
                CreatedAt = rdr.GetDateTime("created_at")
            };
        }

        public User GetByUsername(string username)
        {
            const string sql = @"SELECT user_id, role_id, username, email, password_hash, profile_image, bio, is_online, last_seen, created_at
                                 FROM users WHERE username = @username";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            return new User
            {
                UserId = rdr.GetInt32("user_id"),
                RoleId = rdr.GetInt32("role_id"),
                Username = rdr.GetString("username"),
                Email = rdr.GetString("email"),
                PasswordHash = rdr.GetString("password_hash"),
                ProfileImage = rdr.IsDBNull(rdr.GetOrdinal("profile_image")) ? null : rdr.GetString("profile_image"),
                Bio = rdr.IsDBNull(rdr.GetOrdinal("bio")) ? null : rdr.GetString("bio"),
                IsOnline = rdr.GetBoolean("is_online"),
                LastSeen = rdr.IsDBNull(rdr.GetOrdinal("last_seen")) ? (DateTime?)null : rdr.GetDateTime("last_seen"),
                CreatedAt = rdr.GetDateTime("created_at")
            };
        }

        public IEnumerable<User> GetAll()
        {
            var list = new List<User>();
            const string sql = @"SELECT user_id, role_id, username, email, profile_image, bio, is_online, last_seen, created_at FROM users";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new User
                {
                    UserId = rdr.GetInt32("user_id"),
                    RoleId = rdr.GetInt32("role_id"),
                    Username = rdr.GetString("username"),
                    Email = rdr.GetString("email"),
                    ProfileImage = rdr.IsDBNull(rdr.GetOrdinal("profile_image")) ? null : rdr.GetString("profile_image"),
                    Bio = rdr.IsDBNull(rdr.GetOrdinal("bio")) ? null : rdr.GetString("bio"),
                    IsOnline = rdr.GetBoolean("is_online"),
                    LastSeen = rdr.IsDBNull(rdr.GetOrdinal("last_seen")) ? (DateTime?)null : rdr.GetDateTime("last_seen"),
                    CreatedAt = rdr.GetDateTime("created_at")
                });
            }
            return list;
        }

        public int Create(User user)
        {
            const string sql = @"INSERT INTO users (role_id, username, email, password_hash, profile_image, bio, is_online, last_seen)
                                 VALUES (@role_id, @username, @email, @password_hash, @profile_image, @bio, @is_online, @last_seen);
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@role_id", user.RoleId);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@profile_image", (object)user.ProfileImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bio", (object)user.Bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_online", user.IsOnline);
            cmd.Parameters.AddWithValue("@last_seen", (object)user.LastSeen ?? DBNull.Value);
            var id = Convert.ToInt32(cmd.ExecuteScalar());
            return id;
        }

        public bool Update(User user)
        {
            const string sql = @"UPDATE users SET role_id=@role_id, username=@username, email=@email, profile_image=@profile_image, bio=@bio, is_online=@is_online, last_seen=@last_seen
                                 WHERE user_id=@user_id";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@role_id", user.RoleId);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@profile_image", (object)user.ProfileImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bio", (object)user.Bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_online", user.IsOnline);
            cmd.Parameters.AddWithValue("@last_seen", (object)user.LastSeen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@user_id", user.UserId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM users WHERE user_id = @id";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}