namespace ContosoUniversity.Helpers
{
    using System;
    using System.Security.Cryptography;

    public class KeyGenerator
    {
        public static void Main()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                Console.WriteLine("Key: " + Convert.ToBase64String(aes.Key));
                Console.WriteLine("IV: " + Convert.ToBase64String(aes.IV));
            }
        }
    }
}

