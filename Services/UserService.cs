using System;
using System.Threading.Tasks;
using GameWikiApp.Data;
using GameWikiApp.Helpers;
using GameWikiApp.Models;

namespace GameWikiApp.Services
{
    public class UserService
    {
        private readonly UserRepository _repo = new();

        public Task<User?> GetByIdAsync(int userId) => _repo.GetByIdAsync(userId);

        public async Task<(bool success, string message, User? user)> UpdateProfileAsync(
            int userId,
            string username,
            string email,
            string? bio,
            string? profileImage,
            string? themePreference = null)
        {
            username = username?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;
            bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
            profileImage = string.IsNullOrWhiteSpace(profileImage) ? null : profileImage.Trim();

            if (!Validator.IsUsername(username))
            {
                return (false, "Username must be 3-50 characters and may contain letters, numbers, dots, underscores or hyphens.", null);
            }

            if (!Validator.IsEmail(email))
            {
                return (false, "Please enter a valid email address.", null);
            }

            var current = await _repo.GetByIdAsync(userId);
            if (current == null)
            {
                return (false, "The user account could not be found.", null);
            }

            var usernameOwner = await _repo.GetByUsernameAsync(username);
            if (usernameOwner != null && usernameOwner.UserId != userId)
            {
                return (false, "That username is already taken.", null);
            }

            var emailOwner = await _repo.GetByEmailAsync(email);
            if (emailOwner != null && emailOwner.UserId != userId)
            {
                return (false, "That email address is already in use.", null);
            }

            current.Username = username;
            current.Email = email;
            current.Bio = bio;
            current.ProfileImage = profileImage;
            if (!string.IsNullOrWhiteSpace(themePreference))
            {
                current.ThemePreference = themePreference.Trim();
            }

            var updated = await _repo.UpdateAsync(current);
            return updated
                ? (true, "Profile saved.", current)
                : (false, "The profile could not be saved right now.", null);
        }

        public async Task<(bool success, string message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            currentPassword = currentPassword ?? string.Empty;
            newPassword = newPassword ?? string.Empty;

            if (!Validator.IsStrongPassword(newPassword))
            {
                return (false, "The new password must be at least 8 characters and include upper, lower, number and symbol.");
            }

            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "The user account could not be found.");
            }

            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
            {
                return (false, "The current password is incorrect.");
            }

            var hash = PasswordHasher.Hash(newPassword);
            var updated = await _repo.UpdatePasswordHashAsync(userId, hash);
            return updated
                ? (true, "Password updated.")
                : (false, "The password could not be updated.");
        }

        public Task<bool> UpdateThemePreferenceAsync(int userId, string themePreference)
            => _repo.UpdateThemePreferenceAsync(userId, themePreference);

        public Task<bool> UpdatePresenceAsync(int userId, bool isOnline, DateTime? lastSeen = null)
            => _repo.UpdatePresenceAsync(userId, isOnline, lastSeen);
    }
}
