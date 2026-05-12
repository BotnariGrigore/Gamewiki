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
                PasswordHash = hash
            };
            var id = await _users.CreateAsync(user);
            return id > 0 ? (true, null) : (false, "Database error while creating user.");
        }

        public async Task<Models.User?> AuthenticateAsync(string username, string password)
        {
            var user = await _users.GetByUsernameAsync(username);
            if (user == null) return null;
            if (PasswordHasher.Verify(password, user.PasswordHash)) return user;
            return null;
        }
    }
}
