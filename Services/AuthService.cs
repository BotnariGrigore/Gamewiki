using System;
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

                // Minimal migration: ensure theme_preference column exists
                try
                {
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SHOW COLUMNS FROM users LIKE 'theme_preference'";
                    var col = checkCmd.ExecuteScalar();
                    if (col == null)
                    {
                        using var alterCmd = conn.CreateCommand();
                        alterCmd.CommandText = "ALTER TABLE users ADD COLUMN IF NOT EXISTS theme_preference VARCHAR(20) DEFAULT 'light'";
                        alterCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex2)
                {
                    try { File.AppendAllText("auth_errors.log", DateTime.UtcNow + " MIGRATION ERROR: " + ex2 + Environment.NewLine); } catch { }
                }

                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("auth_errors.log", DateTime.UtcNow + " DB CHECK ERROR: " + ex + Environment.NewLine); } catch {}
                return Task.FromResult<string?>(ex.Message);
            }
        }
    }
}
