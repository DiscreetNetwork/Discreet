using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace Discreet.Cipher
{
	/// <summary>
	/// Implements a Monero-style Base58 block encoding, and Bitcoin-style Base58 encoding.<br /> <br />
	/// Monero's Base58 encoding first splits the byte array into 8-byte chunks and a remainder, <br />
	/// and encodes each chunk into an 11-character base-58 string. The remainder is encoded <br />
	/// into a fixed-length string as well, depending on the size. The blocks are then added <br />
	/// together using string appending to produce the resulting Base58-encoded string.
	/// </summary>
	public static class Base58
	{
		public static int IndexOf(this int[] array, int value)
        {
			if (array == null) return -1;

			for (int i = 0; i < array.Length; i++)
            {
				if (array[i] == value) return i;
            }

			return -1;
        }

		/// <summary>
		/// The Base58 alphabet.
		/// </summary>
		static readonly string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

		/// <summary>
		/// The fixed block sizes the remainder is encoded to.
		/// </summary>
		static readonly int[] blockSizes = new int[] { 0, 2, 3, 5, 6, 7, 9, 10, 11 };

		/// <summary>
		/// Encodes an 8-byte chunk or remainder into a base-58 block of characters.
		/// </summary>
		/// <param name="raw">The raw chunk or remainder of byte data.</param>
		/// <param name="padding">The size to pad the block to.</param>
		/// <returns>A string containing the base-58 block of characters.</returns>
		public static string EncodeChunk(byte[] raw, int padding)
        {
			if (raw.Length < 8)
            {
				byte[] rawtmp = new byte[8];
				Array.Copy(raw, 0, rawtmp, (8 - raw.Length), raw.Length);
				raw = rawtmp;
            }

			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(raw);
			}

			ulong remainder = BitConverter.ToUInt64(raw);
			ulong bigBase = 58;

			string rv = "";

			while (remainder > 0)
            {
				ulong current = remainder % bigBase;
				remainder /= bigBase;
				rv = Alphabet[((int)current)] + rv;
			}

			while (rv.Length < padding)
            {
				rv = "1" + rv;
            }

			return rv;
        }

		/// <summary>
		/// Decodes a base-58 block of characters into a byte array chunk or remainder.
		/// </summary>
		/// <param name="encoded">The base-58 block of characters to decode.</param>
		/// <returns>The chunk or remainder as a byte array.</returns>
		public static byte[] DecodeChunk(string encoded, int padding)
		{
			ulong bigResult = 0;
			ulong currentMultiplier = 1;
			ulong bigBase = 58;
			ulong tmp;

			for (int i = encoded.Length - 1; i >= 0; i--)
            {
				tmp = (ulong)Alphabet.IndexOf(encoded[i]);
				tmp *= currentMultiplier;
				bigResult += tmp;
				currentMultiplier *= bigBase;
            }

			byte[] brBytes = BitConverter.GetBytes(bigResult);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(brBytes);
			}

			/* perform truncating here */
			if (padding < 8)
            {
				byte[] rv = new byte[padding];
				Array.Copy(brBytes, (8 - padding), rv, 0, padding);
				return rv;
            }
			else
            {
				return brBytes;
            }
		}

		/// <summary>
		/// Encodes an entire byte array into a Bitcoin-style Base58 string.
		/// </summary>
		/// <param name="raw">The byte data to encode.</param>
		/// <returns>A Base58 string containing the encoded data.</returns>
		public static string EncodeWhole(byte[] raw)
        {
			// first convert leading zeros to rv string
			string rv = "";

			int i = 0;
			while (raw[i] == 0 && i < raw.Length)
            {
				rv += "1";
            }

			if (i == raw.Length) return rv;

			// this also prepends a zero to ensure the BigInteger is positive
			byte[] _rem = new byte[raw.Length - i + 1];
			Array.Copy(raw, i, _rem, i + 1, _rem.Length);

			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(raw);
			}

			BigInteger remainder = new BigInteger(raw);
			BigInteger bigZero = 0;
			BigInteger bigBase = 58;

			while (remainder.CompareTo(bigZero) > 0)
			{
				BigInteger current = remainder % bigBase;
				remainder /= bigBase;
				rv = Alphabet[((int)current)] + rv;
			}

			return rv;
		}

		/// <summary>
		/// Decodes a Bitcoin-style Base58 string into a byte array.
		/// </summary>
		/// <param name="raw">The Base58 string to decode.</param>
		/// <returns>A byte array containing the decoded data.</returns>
		public static byte[] DecodeWhole(string encoded)
		{
			int j = 0;
			while (encoded[j] == '1' && j < encoded.Length)
            {
				j++;
            }

			if (j == encoded.Length) return new byte[j];

			encoded = encoded.Substring(j);

			BigInteger bigResult = 0;
			BigInteger currentMultiplier = 1;
			BigInteger bigBase = 58;
			BigInteger tmp;

			for (int i = encoded.Length - 1; i >= 0; i--)
			{
				tmp = new BigInteger(Alphabet.IndexOf(encoded[i]));
				tmp *= currentMultiplier;
				bigResult += tmp;
				currentMultiplier *= bigBase;
			}

			byte[] brBytes = bigResult.ToByteArray();

			if (BitConverter.IsLittleEndian)
            {
				Array.Reverse(brBytes);
            }

			// discard leading zero; appended when msbyte > 0x7F for the biginteger (due to positivity)
			if (brBytes[0] == 0)
            {
				byte[] brtmp = new byte[brBytes.Length - 1];
				Array.Copy(brBytes, 1, brtmp, 0, brtmp.Length);
				brBytes = brtmp;
            }

			byte[] rv = new byte[brBytes.Length + j];
			Array.Copy(brBytes, 0, rv, j, brBytes.Length);

			return rv;
		}

		/// <summary>
		/// Performs Monero-style Base58 block encoding on a byte array.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string Encode(byte[] data)
        {
			int rounds = data.Length / 8;
			string res = "";
			byte[] dataChunk = new byte[8];
			byte[] dataRem = new byte[data.Length % 8];

			for (int i = 0; i < rounds; i++)
            {
				Array.Copy(data, i * 8, dataChunk, 0, 8);
				res += EncodeChunk(dataChunk, 11);
            }

			if (data.Length % 8 > 0)
            {
				Array.Copy(data, rounds * 8, dataRem, 0, data.Length % 8);
				res += EncodeChunk(dataRem, blockSizes[data.Length % 8]);
            }

			return res;
        }

		/// <summary>
		/// Performs Monero-style Base58 block decoding on a string.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static byte[] Decode(string data)
		{
			int rounds = data.Length / 11;
			List<byte> res = new List<byte>();

			for (int i = 0; i < rounds; i++)
			{
				res.AddRange(DecodeChunk(data.Substring(i * 11, 11), 8));
			}

			if (data.Length % 11 > 0)
			{
				res.AddRange(DecodeChunk(data.Substring(rounds * 11), blockSizes.IndexOf(data.Substring(rounds * 11).Length)));
			}

			return res.ToArray();
		}

		/// <summary>
		/// The size of an address checksum for both Monero-style Stealth Addresses and Bitcoin-style Transparent Addresses.
		/// </summary>
		public const int CheckSumSizeInBytes = 4;

		/// <summary>
		/// Gets a checksum from the first CheckSumSizeInBytes bytes of a keccak digest.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static byte[] GetCheckSum(byte[] data)
		{
			byte[] hash = Keccak.HashData(data).Bytes;

			var result = new byte[CheckSumSizeInBytes];
			Array.Copy(hash, 0, result, 0, result.Length);

			return result;
		}
	}
}
