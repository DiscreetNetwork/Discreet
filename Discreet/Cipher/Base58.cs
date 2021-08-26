using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;

namespace Discreet.Cipher
{
	public class ArrayHelpers
	{
		public static T[] ConcatArrays<T>(params T[][] arrays)
		{
			Contract.Requires(arrays != null);
			Contract.Requires(Contract.ForAll(arrays, (arr) => arr != null));
			Contract.Ensures(Contract.Result<T[]>() != null);
			Contract.Ensures(Contract.Result<T[]>().Length == arrays.Sum(arr => arr.Length));

			var result = new T[arrays.Sum(arr => arr.Length)];
			int offset = 0;
			for (int i = 0; i < arrays.Length; i++)
			{
				var arr = arrays[i];
				Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
				offset += arr.Length;
			}
			return result;
		}

		public static T[] ConcatArrays<T>(T[] arr1, T[] arr2)
		{
			Contract.Requires(arr1 != null);
			Contract.Requires(arr2 != null);
			Contract.Ensures(Contract.Result<T[]>() != null);
			Contract.Ensures(Contract.Result<T[]>().Length == arr1.Length + arr2.Length);

			var result = new T[arr1.Length + arr2.Length];
			Buffer.BlockCopy(arr1, 0, result, 0, arr1.Length);
			Buffer.BlockCopy(arr2, 0, result, arr1.Length, arr2.Length);
			return result;
		}

		public static T[] SubArray<T>(T[] arr, int start, int length)
		{
			Contract.Requires(arr != null);
			Contract.Requires(start >= 0);
			Contract.Requires(length >= 0);
			Contract.Requires(start + length <= arr.Length);
			Contract.Ensures(Contract.Result<T[]>() != null);
			Contract.Ensures(Contract.Result<T[]>().Length == length);

			var result = new T[length];
			Buffer.BlockCopy(arr, start, result, 0, length);
			return result;
		}

		public static T[] SubArray<T>(T[] arr, int start)
		{
			Contract.Requires(arr != null);
			Contract.Requires(start >= 0);
			Contract.Requires(start <= arr.Length);
			Contract.Ensures(Contract.Result<T[]>() != null);
			Contract.Ensures(Contract.Result<T[]>().Length == arr.Length - start);

			return SubArray(arr, start, arr.Length - start);
		}
	}

	public static class Base58
	{
		public const int CheckSumSizeInBytes = 4;

		public static byte[] AddCheckSum(byte[] data)
		{
			Contract.Requires<ArgumentNullException>(data != null);
			Contract.Ensures(Contract.Result<byte[]>().Length == data.Length + CheckSumSizeInBytes);
			byte[] checkSum = GetCheckSum(data);
			byte[] dataWithCheckSum = ArrayHelpers.ConcatArrays(data, checkSum);
			return dataWithCheckSum;
		}

		//Returns null if the checksum is invalid
		public static byte[] VerifyAndRemoveCheckSum(byte[] data)
		{
			Contract.Requires<ArgumentNullException>(data != null);
			Contract.Ensures(Contract.Result<byte[]>() == null || Contract.Result<byte[]>().Length + CheckSumSizeInBytes == data.Length);
			byte[] result = ArrayHelpers.SubArray(data, 0, data.Length - CheckSumSizeInBytes);
			byte[] givenCheckSum = ArrayHelpers.SubArray(data, data.Length - CheckSumSizeInBytes);
			byte[] correctCheckSum = GetCheckSum(result);
			if (givenCheckSum.SequenceEqual(correctCheckSum))
				return result;
			else
				return null;
		}

		private const string Digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

		public static string Encode(byte[] data)
		{
			Contract.Requires<ArgumentNullException>(data != null);
			Contract.Ensures(Contract.Result<string>() != null);

			// Decode byte[] to BigInteger
			BigInteger intData = 0;
			for (int i = 0; i < data.Length; i++)
			{
				intData = intData * 256 + data[i];
			}

			// Encode BigInteger to Base58 string
			string result = "";
			while (intData > 0)
			{
				int remainder = (int)(intData % 58);
				intData /= 58;
				result = Digits[remainder] + result;
			}

			// Append `1` for each leading 0 byte
			for (int i = 0; i < data.Length && data[i] == 0; i++)
			{
				result = '1' + result;
			}
			return result;
		}

		public static string EncodeWithCheckSum(byte[] data)
		{
			Contract.Requires<ArgumentNullException>(data != null);
			Contract.Ensures(Contract.Result<string>() != null);
			return Encode(AddCheckSum(data));
		}

		public static byte[] Decode(string s)
		{
			Contract.Requires<ArgumentNullException>(s != null);
			Contract.Ensures(Contract.Result<byte[]>() != null);

			// Decode Base58 string to BigInteger 
			BigInteger intData = 0;
			for (int i = 0; i < s.Length; i++)
			{
				int digit = Digits.IndexOf(s[i]); //Slow
				if (digit < 0)
					throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", s[i], i));
				intData = intData * 58 + digit;
			}

			// Encode BigInteger to byte[]
			// Leading zero bytes get encoded as leading `1` characters
			int leadingZeroCount = s.TakeWhile(c => c == '1').Count();
			var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
			var bytesWithoutLeadingZeros =
				intData.ToByteArray()
				.Reverse()// to big endian
				.SkipWhile(b => b == 0);//strip sign byte
			var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
			return result;
		}

		// Throws `FormatException` if s is not a valid Base58 string, or the checksum is invalid
		public static byte[] DecodeWithCheckSum(string s)
		{
			Contract.Requires<ArgumentNullException>(s != null);
			Contract.Ensures(Contract.Result<byte[]>() != null);
			var dataWithCheckSum = Decode(s);
			var dataWithoutCheckSum = VerifyAndRemoveCheckSum(dataWithCheckSum);
			if (dataWithoutCheckSum == null)
				throw new FormatException("Base58 checksum is invalid");
			return dataWithoutCheckSum;
		}

		public static byte[] GetCheckSum(byte[] data)
		{
			Contract.Requires<ArgumentNullException>(data != null);
			Contract.Ensures(Contract.Result<byte[]>() != null);

			byte[] hash1 = SHA256.HashData(data).Bytes;
			byte[] hash2 = SHA256.HashData(hash1).Bytes;

			var result = new byte[CheckSumSizeInBytes];
			Buffer.BlockCopy(hash2, 0, result, 0, result.Length);

			return result;
		}
	}
}
