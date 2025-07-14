using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArendaPro
{
    public static class CryptoHelper
    {
        public static byte[] GetKey() =>
            StringToByteArray(Properties.Settings.Default.EncryptionKey, 32);

        public static byte[] GetIV() =>
            StringToByteArray(Properties.Settings.Default.EncryptionIV, 16);

        private static byte[] StringToByteArray(string hex, int expectedLength)
        {
            if (hex.Length != expectedLength * 2)
                throw new ArgumentException($"Недопустимая длина ключа: {hex.Length}");

            byte[] bytes = new byte[expectedLength];
            for (int i = 0; i < expectedLength; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        public static void EncryptFile(string inputPath, string outputPath)
        {
            using var inputFile = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var outputFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var aes = Aes.Create();
            aes.Key = GetKey();
            aes.IV = GetIV();
            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(outputFile, encryptor, CryptoStreamMode.Write);
            inputFile.CopyTo(cryptoStream);
        }

        public static void DecryptFile(string inputPath, string outputPath)
        {
            using var inputFile = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var outputFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var aes = Aes.Create();
            aes.Key = GetKey();
            aes.IV = GetIV();
            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(inputFile, decryptor, CryptoStreamMode.Read);
            cryptoStream.CopyTo(outputFile);
        }

        public static string DecryptToTempFile(string encryptedPath)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(encryptedPath) + ".docx");
            DecryptFile(encryptedPath, tempPath);
            return tempPath;
        }
    }
}
