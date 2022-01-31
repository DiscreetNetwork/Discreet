using Discreet.Coin;
using Discreet.Common;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Discreet.Cipher
{
    /// <summary>
    /// Represents AES-256 CBC information, such as the mode of padding, stream key, and IV.
    /// </summary>
    public class CipherObject
    {
        /// <summary>
        /// The 256-bit stream key to use for the cipher.
        /// </summary>
        public byte[] Key { get; set; } // 32 bytes

        /// <summary>
        /// The initialization vector (IV) for the cipher.
        /// </summary>
        public byte[] IV { get; set; } // 16 bytes


        /// <summary>
        /// The padding mode for the cipher. Defaults to PKCS7.
        /// </summary>
        public PaddingMode Mode { get; set; }

        /// <summary>
        /// Default constructor. 
        /// </summary>
        public CipherObject()
        {
            Mode = PaddingMode.PKCS7;
        }

        /// <summary>
        /// Generates a CipherObject with the specified key and IV.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        public CipherObject(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
            Mode = PaddingMode.PKCS7;
        }

        /// <summary>
        /// Generates a CipherObject from the byte array. The IV is the first 16 bytes; the next 32 bytes is the key.
        /// </summary>
        /// <param name="bytes"></param>
        public CipherObject(byte[] bytes)
        {
            IV = bytes[0..16];
            Key = bytes[16..48];
            Mode = PaddingMode.PKCS7;
        }

        /// <summary>
        /// Returns a byte array storing the IV and key information.
        /// </summary>
        /// <returns>A byte array, where the first 16 bytes are the IV and the last 32 bytes are the key.</returns>
        public byte[] ToBytes()
        {
            byte[] rv = new byte[48];
            Array.Copy(IV, rv, 16);
            Array.Copy(Key, 0, rv, 16, 32);

            return rv;
        }

        /// <summary>
        /// Prepends the IV to the byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public byte[] PrependIV(byte[] bytes)
        {
            byte[] rv = new byte[16 + bytes.Length];
            Array.Copy(IV, rv, 16);
            Array.Copy(bytes, 0, rv, 16, bytes.Length);

            return rv;
        }

        /// <summary>
        /// Generates a CipherObject from the key and data of an ecrypted object.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="bytes">The encrypted object, prepended with the initialization vector used to generate it.</param>
        /// <returns>A tuple storin the CipherObject and the encrypted object data, without the initialization vector prepended to it.</returns>
        public static (CipherObject, byte[]) GetFromPrependedArray(byte[] key, byte[] bytes)
        {
            CipherObject cipherObject = new()
            {
                Key = key,
                IV = new byte[16]
            };

            Array.Copy(bytes, cipherObject.IV, 16);
            byte[] ct = new byte[bytes.Length - 16];
            Array.Copy(bytes, 16, ct, 0, bytes.Length - 16);

            return (cipherObject, ct);
        }

        /// <summary>
        /// Generates a string representing the CipherObject.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{{\"Key:\"{Printable.Hexify(Key)}\",\"IV\": \"{Printable.Hexify(IV)}\"}}";
        }
    }

    /// <summary>
    /// Contains the methods for using the AES-256 CBC scheme for Discreet.
    /// </summary>
    public static class AESCBC
    {
        /// <summary>
        /// Generates a CipherObject using PBKDF2 on the specified passphrase, as specified by Discreet.
        /// </summary>
        /// <param name="passphrase">The PBKDF2 passphrase.</param>
        /// <returns>The resulting CipherObject, with a random IV.</returns>
        public static CipherObject GenerateCipherObject(string passphrase)
        {
            using RNGCryptoServiceProvider rng = new();
            {

                byte[] tmpKey = new byte[32];
                byte[] passhash = Cipher.SHA512.HashData(Cipher.SHA512.HashData(Encoding.UTF8.GetBytes(passphrase)).Bytes).Bytes;
                Cipher.KeyDerivation.PBKDF2(tmpKey, passhash, 64, new byte[] { 0x44, 0x69, 0x73, 0x63, 0x72, 0x65, 0x65, 0x74 }, 8, 4096, 32);


                byte[] tmpIV = new byte[16];
                rng.GetBytes(tmpIV);

                return new CipherObject(tmpKey, tmpIV);
            };
        }

        /// <summary>
        /// Generates a CipherObject using the specified key information.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The resulting CipherObject, with a random IV.</returns>
        public static CipherObject GenerateCipherObject(byte[] key)
        {
            using RNGCryptoServiceProvider rng = new();
            {
                byte[] tmpIV = new byte[16];
                rng.GetBytes(tmpIV);

                return new CipherObject(key, tmpIV);
            };
        }

        /// <summary>
        /// Performs AES-256 CBC on the specified byte data using the specified CipherObject.
        /// </summary>
        /// <param name="unencryptedCipher">The byte data to encrypt.</param>
        /// <param name="encryptionParams">The CipherObject specifying the encryption parameters.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(byte[] unencryptedCipher, CipherObject encryptionParams)
        {
            Aes cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;  
            cipher.Padding = encryptionParams.Mode;
            cipher.Key = encryptionParams.Key;
            //cipher.KeySize = encryptionParams.Key.Length * 8;
            cipher.IV = encryptionParams.IV;


            ICryptoTransform cryptTransform = cipher.CreateEncryptor();
            byte[] cipherBytes = cryptTransform.TransformFinalBlock(unencryptedCipher, 0, unencryptedCipher.Length);

            return cipherBytes;
        }

        /// <summary>
        /// Decrypts an array of bytes generated by using AESCBC.Encrypt() with the specified CipherObject.
        /// </summary>
        /// <param name="encryptedCipher">The encrypted byte data to decrypt.</param>
        /// <param name="encryptionParams">The CipherObject specifying the decryption parameters.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] Decrypt(byte[] encryptedCipher, CipherObject encryptionParams)
        {

            Aes cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC; 
            cipher.Padding = encryptionParams.Mode;
            cipher.Key = encryptionParams.Key;
            //cipher.KeySize = encryptionParams.Key.Length * 8;
            cipher.IV = encryptionParams.IV;

            ICryptoTransform cryptTransform = cipher.CreateDecryptor();
            byte[] plainBytes = cryptTransform.TransformFinalBlock(encryptedCipher, 0, encryptedCipher.Length);

            return plainBytes;
        }
    }
}
