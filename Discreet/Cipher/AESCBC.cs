using Discreet.Coin;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Discreet.Cipher
{
    public class CipherObject
    {
        public byte[] Key { get; set; } // 32 bytes
        public byte[] IV { get; set; } // 16 bytes

        public CipherObject()
        {

        }
        public CipherObject(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
        }

        public CipherObject(byte[] bytes)
        {
            Key = bytes[0..16];
            IV = bytes[16..48];
        }

        public byte[] ToBytes()
        {
            byte[] rv = new byte[48];
            Array.Copy(IV, rv, 16);
            Array.Copy(Key, 0, rv, 16, 32);

            return rv;
        }
        public override string ToString()
        {
            return $"{{\"Key:\"{Printable.Hexify(Key)}\",\"IV\": \"{Printable.Hexify(IV)}\"}}";
        }

    }

    public static class AESCBC
    {
        public static CipherObject GenerateCipherObject(string passphrase)
        {
            using RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            {

                byte[] tmpKey = new byte[32];
                byte[] passhash = Cipher.SHA512.HashData(Cipher.SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
                Cipher.KeyDerivation.PBKDF2(tmpKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);


                byte[] tmpIV = new byte[16];
                rng.GetBytes(tmpIV);

                return new CipherObject(tmpKey, tmpIV);
            };
        }



        public static byte[] Encrypt(byte[] unencryptedCipher, CipherObject encryptionParams)
        {
            Aes cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;  
            cipher.Padding = PaddingMode.PKCS7;
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
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = encryptionParams.Key;
            cipher.IV = encryptionParams.IV;

            ICryptoTransform cryptTransform = cipher.CreateDecryptor();
            byte[] plainBytes = cryptTransform.TransformFinalBlock(encryptedCipher, 0, encryptedCipher.Length);

            return plainBytes;
        }



    }
}
