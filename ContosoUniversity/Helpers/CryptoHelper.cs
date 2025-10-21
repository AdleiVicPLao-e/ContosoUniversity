using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class CryptoHelper
{
    private static byte[] GetKey()
    {
        var keyString = Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
                        ?? "w7bXg4rM1kL2mN3oP4qR5sT6uV7wXyZa"; // base64 fallback
        return Convert.FromBase64String(keyString);
    }

    private static byte[] GetIV()
    {
        var ivString = Environment.GetEnvironmentVariable("ENCRYPTION_IV")
                       ?? "1234567890123456"; // base64 fallback
        if (string.IsNullOrEmpty(ivString))
            throw new InvalidOperationException("ENCRYPTION_IV is not set.");

        // Use UTF8 bytes directly (16 chars = 16 bytes)
        var ivBytes = Encoding.UTF8.GetBytes(ivString);
        if (ivBytes.Length != 16)
            throw new InvalidOperationException("ENCRYPTION_IV must be 16 bytes.");

        return ivBytes;
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;

        using (Aes aes = Aes.Create())
        {
            aes.Key = GetKey();
            aes.IV = GetIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (MemoryStream ms = new MemoryStream())
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;

        using (Aes aes = Aes.Create())
        {
            aes.Key = GetKey();
            aes.IV = GetIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (MemoryStream ms = new MemoryStream())
            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.FlushFinalBlock();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
