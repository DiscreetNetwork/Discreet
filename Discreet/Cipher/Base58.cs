using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace Discreet.Cipher
{
	public static class Base58
	{
		static string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

		static int[] blockSizes = new int[] { 0, 2, 3, 5, 6, 7, 9, 10, 11 };

		public static byte[] TruncateFrom8(byte[] bytes)
        {
			int i;
			for (i = 7; i >= 0; i--)
            {
				if (bytes[i] > 0)
                {
					break;
                }
            }

			byte[] rv = new byte[i + 1];
			Array.Copy(bytes, 0, rv, 0, rv.Length);
			return rv;
		}

		public static byte[] PadTo8(byte[] bytes)
        {
			if (bytes.Length == 8)
            {
				return bytes;
            }

			byte[] rv = new byte[8];
			Array.Copy(bytes, 0, rv, (8 - bytes.Length), bytes.Length);
			return rv;
        }

		public static string EncodeChunk(byte[] raw, int padding)
        {
			raw = PadTo8(raw);

			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(raw);
			}

			ulong remainder = BitConverter.ToUInt64(raw);
			ulong bigZero = 0;
			ulong bigBase = 58;

			string rv = "";

			while (remainder.CompareTo(bigZero) > 0)
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

		public static byte[] DecodeChunk(string encoded)
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

			byte[] rv = TruncateFrom8(BitConverter.GetBytes(bigResult));
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(rv);
			}
			
			return rv;
		}

		public static string EncodeWhole(byte[] raw)
        {
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(raw);
			}

			BigInteger remainder = new BigInteger(raw);
			BigInteger bigZero = 0;
			BigInteger bigBase = 58;

			string rv = "";

			while (remainder.CompareTo(bigZero) > 0)
			{
				BigInteger current = remainder % bigBase;
				remainder /= bigBase;
				rv = Alphabet[((int)current)] + rv;
			}

			return rv;
		}

		public static byte[] DecodeWhole(string encoded)
		{
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

			byte[] rv = bigResult.ToByteArray();

			if (BitConverter.IsLittleEndian)
            {
				Array.Reverse(rv);
            }

			return rv;
		}

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

		public static byte[] Decode(string data)
		{
			int rounds = data.Length / 11;
			List<byte> res = new List<byte>();

			for (int i = 0; i < rounds; i++)
			{
				res.AddRange(DecodeChunk(data.Substring(i * 11, 11)));
			}

			if (data.Length % 11 > 0)
			{
				res.AddRange(DecodeChunk(data.Substring(rounds * 11)));
			}

			return res.ToArray();
		}

		public const int CheckSumSizeInBytes = 4;

		public static byte[] GetCheckSum(byte[] data)
		{
			byte[] hash = Keccak.HashData(data).Bytes;

			var result = new byte[CheckSumSizeInBytes];
			Array.Copy(hash, 0, result, 0, result.Length);

			return result;
		}
	}
}
