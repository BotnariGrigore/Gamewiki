using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Helpers;

namespace GameWikiApp.Services
{
    public class AuthService
    {
        private readonly UserRepository _users = new();

        public async Task<(bool success, string? error)> RegisterAsync(string username, string email, string password)
        {
            try
            {
                if (!Helpers.Validator.IsUsername(username)) return (false, "Invalid username. Use 3-50 letters, numbers, dots, underscores or hyphens.");
                if (!Helpers.Validator.IsEmail(email)) return (false, "Invalid email address.");
                if (!Helpers.Validator.IsStrongPassword(password)) return (false, "Password is not strong enough.");

                var byUser = await _users.GetByUsernameAsync(username);
                if (byUser != null) return (false, "Username already taken.");

                var byEmail = await _users.GetByEmailAsync(email);
                if (byEmail != null) return (false, "Email already registered.");

                var hash = PasswordHasher.Hash(password);
                var user = new Models.User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = hash,
                    ThemePreference = "light",
                    RoleId = 2
                };
                var id = await _users.CreateAsync(user);
                return id > 0 ? (true, null) : (false, "Database error while creating user.");
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("auth_errors.log", DateTime.UtcNow + " REGISTER ERROR: " + ex + Environment.NewLine); } catch {}
                return (false, "An unexpected error occurred while registering. Check logs.");
            }
        }

        public async Task<(Models.User? user, string? error)> AuthenticateAsync(string identifier, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                    return (null, "Username and password are required.");

                var user = await _users.GetByUsernameAsync(identifier.Trim());
                if (user == null)
                {
                    user = await _users.GetByEmailAsync(identifier.Trim());
                }

                if (user == null)
                {
                    try { File.AppendAllText("auth_debug.log", DateTime.UtcNow + $" AUTH: User not found - identifier={identifier}" + Environment.NewLine); } catch {}
                    return (null, "Invalid username/email or password.");
                }

                var passwordValid = PasswordHasher.Verify(password, user.PasswordHash);
                try { File.AppendAllText("auth_debug.log", DateTime.UtcNow + $" AUTH: User={user.Username}, PasswordValid={passwordValid}" + Environment.NewLine); } catch {}

                if (!passwordValid) return (null, "Invalid username/email or password.");

                try
                {
                    user.IsOnline = true;
                    user.LastSeen = DateTime.UtcNow;
                    await _users.UpdateAsync(user);
                    await EnsureBootstrapAdminAsync(user);
                }
                catch { }

                return (user, null);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("auth_errors.log", DateTime.UtcNow + " AUTH ERROR: " + ex + Environment.NewLine); } catch {}
                return (null, "An unexpected error occurred. Please try again.");
            }
        }

        private async Task EnsureBootstrapAdminAsync(Models.User user)
        {
            var adminCount = await _users.GetAdminCountAsync();
            if (adminCount > 0)
            {
                return;
            }

            if (await _users.UpdateRoleAsync(user.UserId, 1))
            {
                user.RoleId = 1;
                user.RoleName = "admin";
            }
        }

        public Task<string?> CheckDatabaseAsync()
        {
            try
            {
                using var conn = DbConnection.GetOpen();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                _ = cmd.ExecuteScalar();

                EnsureColumn(conn, "users", "theme_preference", "ALTER TABLE users ADD COLUMN theme_preference VARCHAR(20) DEFAULT 'light'");
                EnsureColumn(conn, "notifications", "source_user_id", "ALTER TABLE notifications ADD COLUMN source_user_id INT NULL AFTER user_id");
                EnsureColumn(conn, "notifications", "notification_type", "ALTER TABLE notifications ADD COLUMN notification_type VARCHAR(50) NULL DEFAULT 'general' AFTER source_user_id");
                EnsureColumn(conn, "notifications", "title", "ALTER TABLE notifications ADD COLUMN title VARCHAR(150) NULL AFTER notification_type");
                EnsureColumn(conn, "notifications", "message", "ALTER TABLE notifications ADD COLUMN message TEXT NULL AFTER title");
                EnsureColumn(conn, "notifications", "target_type", "ALTER TABLE notifications ADD COLUMN target_type VARCHAR(50) NULL AFTER message");
                EnsureColumn(conn, "notifications", "target_id", "ALTER TABLE notifications ADD COLUMN target_id INT NULL AFTER target_type");
                EnsureColumn(conn, "notifications", "action_route", "ALTER TABLE notifications ADD COLUMN action_route VARCHAR(255) NULL AFTER target_id");
                EnsureColumn(conn, "notifications", "is_read", "ALTER TABLE notifications ADD COLUMN is_read BOOLEAN NOT NULL DEFAULT FALSE AFTER action_route");
                EnsureColumn(conn, "notifications", "created_at", "ALTER TABLE notifications ADD COLUMN created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP AFTER is_read");
                EnsureColumn(conn, "page_views", "viewed_at", "ALTER TABLE page_views ADD COLUMN viewed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP AFTER user_id");

                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("auth_errors.log", DateTime.UtcNow + " DB CHECK ERROR: " + ex + Environment.NewLine); } catch {}
                return Task.FromResult<string?>(ex.Message);
            }
        }

        private static void EnsureColumn(IDbConnection conn, string tableName, string columnName, string alterSql)
        {
            try
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = $"SHOW COLUMNS FROM `{tableName}` LIKE '{columnName}'";
                var exists = checkCmd.ExecuteScalar() != null;
                if (exists)
                {
                    return;
                }

                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = alterSql;
                alterCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("auth_errors.log", DateTime.UtcNow + $" MIGRATION ERROR ({tableName}.{columnName}): " + ex + Environment.NewLine); } catch { }
            }
        }
    }
}
