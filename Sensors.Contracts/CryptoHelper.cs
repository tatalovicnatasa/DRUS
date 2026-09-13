using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Sensors.Contracts
{
    // Zahtev 4. AES = poverljivost 
    // HMAC = potpis/integritet
    public static class CryptoHelper
    {
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"); // 32B
        private static readonly byte[] HmacKey = Encoding.UTF8.GetBytes("SuperSecretHmacKeyForSensorsAB!");

        // Vraca JEDAN niz bajtova: prvih 16 bajtova je IV, ostatak je sifrat.
        // kroz ceo WCF ugovor, "zalepimo" ga na sifrovani sadrzaj.
        public static byte[] Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.GenerateIV();
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length); // prvih 16 bajtova = IV
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = data.Take(16).ToArray();          // prvih 16 bajtova nazad u IV
                var cipherText = data.Skip(16).ToArray();  // ostatak je stvarni sifrat

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        //HMACSHA256, kljuc + poruka -> potpis.
        public static byte[] Sign(byte[] encryptedPayload, long messageId, DateTime timestamp)
        {
            var buffer = encryptedPayload
                .Concat(BitConverter.GetBytes(messageId))
                .Concat(BitConverter.GetBytes(timestamp.Ticks))
                .ToArray();

            using (var hmac = new HMACSHA256(HmacKey))
                return hmac.ComputeHash(buffer);
        }

        public static bool Verify(byte[] encryptedPayload, long messageId, DateTime timestamp, byte[] signature)
        {
            var expected = Sign(encryptedPayload, messageId, timestamp);
            return Xor(expected, signature); // BAC poredjenje, str. 12 kursa
        }

        // 
        private static bool Xor(byte[] a, byte[] b)
        {
            int x = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; ++i)
                x |= a[i] ^ b[i];
            return x == 0;
        }
    }
}