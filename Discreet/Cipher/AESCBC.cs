using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Discreet.Cipher
{
    public static class AESCBC
    {
        public static byte[] Encrypt(byte[] plainText, byte[] Key, int keySize = 256)
        {
            byte[] encrypted;
            byte[] IV;

            using (Aes scheme = Aes.Create())
            {
                scheme.Key = Key;
                scheme.KeySize = keySize;

                scheme.GenerateIV();
                IV = scheme.IV;

                scheme.Mode = CipherMode.CBC;

                var encryptor = scheme.CreateEncryptor(scheme.Key, scheme.IV);

                // Create the streams used for encryption. 
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var bwEncrypt = new BinaryWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            bwEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            var combinedIvCt = new byte[IV.Length + encrypted.Length];
            Array.Copy(IV, 0, combinedIvCt, 0, IV.Length);
            Array.Copy(encrypted, 0, combinedIvCt, IV.Length, encrypted.Length);

            // Return the encrypted bytes from the memory stream. 
            return combinedIvCt;

        }

        public static byte[] Decrypt(byte[] cipherTextCombined, byte[] Key, int keySize = 256)
        {

            byte[] rv = null;

            // Create an Aes object 
            // with the specified key and IV. 
            using (Aes scheme = Aes.Create())
            {
                scheme.Key = Key;
                scheme.KeySize = keySize;

                byte[] IV = new byte[scheme.BlockSize / 8];
                byte[] cipherText = new byte[cipherTextCombined.Length - IV.Length];

                Array.Copy(cipherTextCombined, IV, IV.Length);
                Array.Copy(cipherTextCombined, IV.Length, cipherText, 0, cipherText.Length);

                scheme.IV = IV;

                scheme.Mode = CipherMode.CBC;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = scheme.CreateDecryptor(scheme.Key, scheme.IV);

                // Create the streams used for decryption. 
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var brDecrypt = new BinaryReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            rv = brDecrypt.ReadBytes((int)csDecrypt.Length);
                        }
                    }
                }

            }

            return rv;

        }
    }
}
