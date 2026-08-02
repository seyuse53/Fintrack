using System.Security.Cryptography;
using System.Text;

namespace FinTrack.Core.Services
{
    public static class CryptoProvider
    {
        // 256-bit (32 bytes) key length for AES-256
        private const int KeySize = 32;
        // 128-bit (16 bytes) IV for AES
        private const int IvSize = 16;

        public static string GenerateRandomDataEncryptionKey()
        {
            byte[] key = new byte[KeySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }

        public static string GenerateRecoveryCode()
        {
            // Generates a random recovery code like: ABCD-EFGH-IJKL-MNOP
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new char[16];
            for (int i = 0; i < 16; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }

            return $"{new string(code, 0, 4)}-{new string(code, 4, 4)}-{new string(code, 8, 4)}-{new string(code, 12, 4)}";
        }

        public static string Encrypt(string plainText, string base64Key)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            if (string.IsNullOrEmpty(base64Key))
                return plainText; // Might happen during Entity Framework Design-Time Migrations
            
            base64Key = base64Key.Replace("\0", "").Trim();
            if (string.IsNullOrEmpty(base64Key))
                return plainText;

            try
            {
                byte[] key = Convert.FromBase64String(base64Key);

                // AES requires exactly 16, 24, or 32 byte keys
                if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                    return plainText;

                byte[] iv = new byte[IvSize];
                byte[] encrypted;

                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = iv;

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                            encrypted = msEncrypt.ToArray();
                        }
                    }
                }

                // Combine IV and Encrypted data to be able to decrypt it later
                byte[] result = new byte[iv.Length + encrypted.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

                return Convert.ToBase64String(result);
            }
            catch
            {
                // Return plaintext if encryption fails for any reason
                // The database itself is already encrypted by SQLCipher.
                return plainText;
            }
        }

        public static string Decrypt(string cipherText, string base64Key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            if (string.IsNullOrEmpty(base64Key))
                return cipherText;

            base64Key = base64Key.Replace("\0", "").Trim();
            if (string.IsNullOrEmpty(base64Key))
                return cipherText;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                byte[] key = Convert.FromBase64String(base64Key);

                // AES requires exactly 16, 24, or 32 byte keys
                if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                    return cipherText;

                byte[] iv = new byte[IvSize];
                byte[] cipher = new byte[fullCipher.Length - IvSize];

                // Extract IV and the actual cipher text
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                string plaintext;

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = iv;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (var msDecrypt = new System.IO.MemoryStream(cipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }

                return plaintext;
            }
            catch
            {
                // Return original text if decryption fails (e.g. it wasn't encrypted to begin with, or wrong key)
                // This is crucial for backward compatibility with unencrypted data.
                return cipherText;
            }
        }
        
        // Legacy helper: derives a 256-bit key from a user password without a salt (for backward compatibility)
        public static string DeriveKeyFromPasswordLegacy(string password)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        // New helper: derives a stable 256-bit key using PBKDF2 with a salt
        public static string DeriveKeyFromPassword(string password, string saltBase64)
        {
            byte[] saltBytes = Convert.FromBase64String(saltBase64);
            byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100000, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(keyBytes);
        }

        // Generate a random salt for password hashing
        public static byte[] CreateSalt(int size = 16)
        {
            byte[] salt = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        // Hashes a password using PBKDF2 (Rfc2898DeriveBytes)
        // Format: version:iterations:salt(base64):hash(base64)
        public static string HashPasswordPBKDF2(string password, int iterations = 100000)
        {
            byte[] salt = CreateSalt();
            
            // Generate a 256-bit (32 byte) hash using HMACSHA256
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                32);
            
            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);
            
            // Format: V1:Iterations:Salt:Hash
            return $"V1:{iterations}:{saltBase64}:{hashBase64}";
        }

        // Verifies a generic PBKDF2 hashed password
        public static bool VerifyPasswordPBKDF2(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith("V1:"))
                return false;

            string[] parts = storedHash.Split(':');
            if (parts.Length != 4)
                return false;

            if (!int.TryParse(parts[1], out int iterations))
                return false;

            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                32);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
