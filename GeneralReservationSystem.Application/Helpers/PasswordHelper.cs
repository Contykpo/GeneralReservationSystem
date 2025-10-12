using System.Security.Cryptography;

namespace GeneralReservationSystem.Application.Helpers
{
    public static class PasswordHelper
    {
        private const int SaltSize = 32; // 256 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        public static (byte[] hash, byte[] salt) HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);
            return (hash, salt);
        }

        public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            if (hash == null || salt == null || salt.Length != SaltSize || hash.Length != KeySize)
                return false;
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);
            return CryptographicOperations.FixedTimeEquals(hash, hashToCompare);
        }
    }
}
