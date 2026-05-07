using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using GameWikiApp.Models;

namespace GameWikiApp.Data
{
    public class FriendRepository
    {
        public int SendRequest(int userId, int friendId)
        {
            const string sql = @"INSERT INTO friends (user_id, friend_id, status) VALUES (@user_id, @friend_id, 'pending');
                                 SELECT LAST_INSERT_ID();";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@friend_id", friendId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool UpdateStatus(int friendshipId, string status)
        {
            const string sql = @"UPDATE friends SET status = @status WHERE friendship_id = @fid";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@fid", friendshipId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public IEnumerable<Friend> GetUserFriends(int userId)
        {
            var list = new List<Friend>();
            const string sql = @"SELECT friendship_id, user_id, friend_id, status, created_at FROM friends
                                 WHERE (user_id = @user_id OR friend_id = @user_id) AND status = 'accepted'
                                 ORDER BY created_at DESC";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Friend
                {
                    FriendshipId = rdr.GetInt32("friendship_id"),
                    UserId = rdr.GetInt32("user_id"),
                    FriendId = rdr.GetInt32("friend_id"),
                    Status = rdr.GetString("status"),
                    CreatedAt = rdr.GetDateTime("created_at")
                });
            }
            return list;
        }

        public IEnumerable<Friend> GetPendingRequests(int userId)
        {
            var list = new List<Friend>();
            const string sql = @"SELECT friendship_id, user_id, friend_id, status, created_at FROM friends
                                 WHERE friend_id = @user_id AND status = 'pending'
                                 ORDER BY created_at DESC";
            using var conn = DbConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Friend
                {
                    FriendshipId = rdr.GetInt32("friendship_id"),
                    UserId = rdr.GetInt32("user_id"),
                    FriendId = rdr.GetInt32("friend_id"),
                    Status = rdr.GetString("status"),
                    CreatedAt = rdr.GetDateTime("created_at")
                });
            }
            return list;
        }
    }
}