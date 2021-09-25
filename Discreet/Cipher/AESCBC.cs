using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Discreet.Cipher
{
    public static class AESCBC
    {
        public static byte[] Encrypt(byte[] plainText, byte[] Key, int keySize = 256, PaddingMode mode = PaddingMode.PKCS7)
        {
            byte[] encrypted;
            byte[] IV;

            using (Aes scheme = Aes.Create())
            {
                scheme.Key = Key;
                scheme.KeySize = keySize;
                scheme.BlockSize = 128;

                scheme.GenerateIV();
                IV = scheme.IV;

                scheme.Mode = CipherMode.CBC;

                scheme.Padding = mode;

                var encryptor = scheme.CreateEncryptor(scheme.Key, scheme.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainText, 0, plainText.Length);
                        cryptoStream.FlushFinalBlock();

                        encrypted = ms.ToArray();
                    }
                }
            }

            var combinedIvCt = new byte[IV.Length + encrypted.Length];
            Array.Copy(IV, 0, combinedIvCt, 0, IV.Length);
            Array.Copy(encrypted, 0, combinedIvCt, IV.Length, encrypted.Length);

            // Return the encrypted bytes from the memory stream. 
            return combinedIvCt;

        }

        public static byte[] Decrypt(byte[] cipherTextCombined, byte[] Key, int keySize = 256, PaddingMode mode = PaddingMode.PKCS7)
        {

            byte[] rv = null;

            // Create an Aes object 
            // with the specified key and IV. 
            using (Aes scheme = Aes.Create())
            {
                scheme.Key = Key;
                scheme.KeySize = keySize;
                scheme.BlockSize = 128;

                byte[] IV = new byte[scheme.BlockSize / 8];
                byte[] cipherText = new byte[cipherTextCombined.Length - IV.Length];

                Array.Copy(cipherTextCombined, IV, IV.Length);
                Array.Copy(cipherTextCombined, IV.Length, cipherText, 0, cipherText.Length);

                scheme.IV = IV;

                scheme.Mode = CipherMode.CBC;

                scheme.Padding = mode;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = scheme.CreateDecryptor(scheme.Key, scheme.IV);

                // Create the streams used for decryption. 
                using (var ms = new MemoryStream(cipherText))
                {
                    using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(cipherText, 0, cipherText.Length);
                        cryptoStream.FlushFinalBlock();

                        rv = ms.ToArray();
                    }
                }

            }

            return rv;

        }
    }
}
