using Discreet.Coin;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Discreet.Cipher
{
    // Store CipherObject as JSON file on disk. We need initialization vectors (IV) to successfully decrypt encrypted data.
    public class CipherObject
    {
        public byte[] Key { get; set; }
        public byte[] IV { get; set; }
        public PaddingMode Padding
        {
            get
            {
                return PaddingMode.ISO10126;
            }
            set
            {
                Padding = value;
            }
        }

        public override string ToString()
        {
            return $"{{\"Key:\"{Printable.Hexify(Key)}\",\"IV\": \"{Printable.Hexify(IV)}\"}}";
        }

    }

    public static class AESCBC
    {
        public static byte[] Encrypt(byte[] unencryptedCipher, CipherObject encryptionParams)
        {
            Aes cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;  
            cipher.Padding = encryptionParams.Padding;
            cipher.Key = encryptionParams.Key;
            cipher.IV = encryptionParams.IV;


            ICryptoTransform cryptTransform = cipher.CreateEncryptor();
            byte[] cipherBytes = cryptTransform.TransformFinalBlock(unencryptedCipher, 0, unencryptedCipher.Length);

            return cipherBytes;
        }


        public static byte[] Decrypt(byte[] encryptedCipher, CipherObject encryptionParams)
        {

            Aes cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC; 
            cipher.Padding = encryptionParams.Padding;
            cipher.Key = encryptionParams.Key;
            cipher.IV = encryptionParams.IV;

            ICryptoTransform cryptTransform = cipher.CreateDecryptor();
            byte[] plainBytes = cryptTransform.TransformFinalBlock(encryptedCipher, 0, encryptedCipher.Length);

            return plainBytes;
        }



    }
}
