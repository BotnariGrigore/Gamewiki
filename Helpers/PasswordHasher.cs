using System;
using System.Linq;
using System.Security.Cryptography;

namespace GameWikiApp.Helpers
{
    public static class PasswordHasher
    {
     
        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var derived = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(derived);
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
             

                int iterations = 100_000;
                string saltB64 = null!;
                string hashB64 = null!;

                if (stored.Contains('$'))
                {
                    var tok = stored.Split('$');
                
                    if (tok.Length >= 4 && tok[1].All(char.IsDigit))
                    {
                        iterations = int.Parse(tok[1]);
                        saltB64 = tok[2];
                        hashB64 = tok[3];
                    }
                }

                if (saltB64 == null)
                {
                    var parts = stored.Split(':');
                    if (parts.Length == 2)
                    {
                        saltB64 = parts[0];
                        hashB64 = parts[1];
                    }
                    else if (parts.Length == 3 && parts[0].All(char.IsDigit))
                    {
                        iterations = int.Parse(parts[0]);
                        saltB64 = parts[1];
                        hashB64 = parts[2];
                    }
                    else
                    {
                        return false;
                    }
                }

                var salt = Convert.FromBase64String(saltB64);
                var expected = Convert.FromBase64String(hashB64);
                var derived = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(derived, expected);
            }
            catch
            {
                return false;
            }
        }
    }
}
