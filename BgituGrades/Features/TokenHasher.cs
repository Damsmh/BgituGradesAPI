using System.Security.Cryptography;
using System.Text;

namespace BgituGrades.Features
{
    public interface ITokenHasher
    {
        string Hash(string token);
        bool Verify(string token, string storedHash);
        string ComputeLookupHash(string token);
    }
    public class TokenHasher : ITokenHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

        public string ComputeLookupHash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLower();
        }

        public string Hash(string token)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(token, salt, Iterations, Algorithm, HashSize);

            var result = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

            return Convert.ToBase64String(result);
        }

        public bool Verify(string token, string storedHash)
        {
            var bytes = Convert.FromBase64String(storedHash);

            var salt = bytes[..SaltSize];
            var storedTokenHash = bytes[SaltSize..];

            var hash = Rfc2898DeriveBytes.Pbkdf2(token, salt, Iterations, Algorithm, HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, storedTokenHash);
        }
    }
}
