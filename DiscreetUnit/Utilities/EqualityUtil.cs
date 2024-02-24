using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscreetUnit.Utilities.EqualityUtil;
using Discreet.Cipher.Extensions;
using Discreet.Cipher;
using Discreet.Coin;
using System.Collections;

namespace DiscreetUnit.Utilities
{
    internal static class EqualityUtil
    {
        public delegate bool EqualityComparer<T>(T x, T y);

        public static bool CheckArrayEqual<T>(T[] item1, T[] item2, Func<T, T, bool>? itemEqualityComparer = null)
        {
            if (item1 == null && item2 == null) return true;
            if (item1 == null || item2 == null) return false;

            if (item1.Length != item2.Length) return false;

            if (itemEqualityComparer == null)
            {
                itemEqualityComparer = (T x, T y) => (x == null && y == null) || (x?.Equals(y) ?? false);
            }

            for (int i = 0; i < item1.Length; i++)
            {
                if (!itemEqualityComparer(item1[i], item2[i])) return false;
            }

            return true;
        }

        public static bool CheckBytesEqual(byte[] item1, byte[] item2)
        {
            if (item1 == null && item2 == null) return true;
            if (item1 == null || item2 == null) return false;

            if (item1.Length != item2.Length) return false;

            for (int i = 0; i < item1.Length; i++)
            {
                if (item1[i] != item2[i]) return false;
            }

            return true;
        }

        public static bool CheckStructureEqual<T>(T item1, T item2, IEnumerable<Func<T,T,bool>> fieldEqualityComparers = null)
        {
            if (item1 == null && item2 == null) return true;
            if (item1 == null || item2 == null) return false;

            if (item1.GetType() != item2.GetType()) return false;

            foreach (var fieldEqualityComparer in fieldEqualityComparers)
            {
                if (!fieldEqualityComparer(item1, item2)) return false;
            }

            return true;
        }

        public static bool CheckFieldEqual<T,T2>(T item1, T item2, Func<T, T2> selector, Func<T2, T2, bool> fieldEqualityComparer = null)
        {
            if (item1 == null && item2 == null) return true;
            if (item1 == null || item2 == null) return false;

            if (fieldEqualityComparer == null)
            {
                fieldEqualityComparer = (T2 x, T2 y) => (x == null && y == null) || (x?.Equals(y) ?? false);
            }

            return fieldEqualityComparer(selector(item1), selector(item2));
        }

        public static bool CheckEqual(object? x, object? y) => CheckStructureEqual(x, y);

        private static Func<object, object, Type, bool>? GetBaseEqualityComparer(Type type)
        {
            if (type == typeof(sbyte)) return (x, y, t) => ((sbyte)x).Equals((sbyte)y);
            if (type == typeof(byte)) return (x, y, t) => ((byte)x).Equals((byte)y);
            if (type == typeof(bool)) return (x, y, t) => ((bool)x).Equals((bool)y);
            if (type == typeof(ushort)) return (x, y, t) => ((ushort)x).Equals((ushort)y);
            if (type == typeof(short)) return (x, y, t) => ((short)x).Equals((short)y);
            if (type == typeof(int)) return (x, y, t) => ((int)x).Equals((int)y);
            if (type == typeof(uint)) return (x, y, t) => ((uint)x).Equals((uint)y);
            if (type == typeof(long)) return (x, y, t) => ((long)x).Equals((long)y);
            if (type == typeof(ulong)) return (x, y, t) => ((ulong)x).Equals((ulong)y);
            if (type == typeof(float)) return (x, y, t) => ((float)x).Equals((float)y);
            if (type == typeof(double)) return (x, y, t) =>((double)x).Equals((double)y);
            if (type == typeof(decimal)) return (x, y, t) => ((decimal)x).Equals((decimal)y);
            if (type == typeof(string)) return (x, y, t) => ((string)x).Equals((string)y);
            if (type == typeof(byte[])) return (x, y, t) => ((byte[])x).Compare((byte[])y) == 0;
            if (type == typeof(Hash)) return (x, y, t) => ((Hash)x).GetBytes().Compare(((Hash)y).GetBytes()) == 0;
            if (type == typeof(Key)) return (x, y, t) => ((Key)x) == ((Key)y);
            if (type == typeof(IAddress)) return (x, y, t) => ((IAddress)x).Bytes().Compare(((IAddress)y).Bytes()) == 0;
            if (type == typeof(Signature)) return (x, y, t) => ((Signature)x).ToBytes().Compare(((Signature)y).ToBytes()) == 0;

            return null;
        }

        public static bool CheckEnumerableEqual(object enum1, object enum2)
        {
            var x = enum1 as IEnumerable;
            var y = enum2 as IEnumerable;

            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            List<object> xs = new();
            List<object> ys = new();

            foreach (var xe in x)
            {
                xs.Add(xe);
            }

            foreach(var ye in y)
            {
                ys.Add(ye);
            }


            if (xs.Count != ys.Count) return false;

            foreach ((var xe, var ye) in xs.Zip(ys))
            {
                if (!CheckStructureEqual(xe, ye)) return false;
            }

            return true;
        }

        public static bool CheckClassEqual(object x, object y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            var type = x.GetType();

            /* check (readable & writable) properties and fields */
            var props = type.GetProperties().Where(x => x.CanWrite && x.CanRead).ToArray();
            var fields = type.GetFields();

            foreach (var prop in props)
            {
                var propx = prop.GetValue(x);
                var propy = prop.GetValue(y);

                if (!CheckStructureEqual(propx, propy)) return false;
            }

            foreach (var field in fields)
            {
                var fieldx = field.GetValue(x);
                var fieldy = field.GetValue(y);

                if (!CheckStructureEqual(fieldx, fieldy)) return false;
            }

            return true;
        }

        private static bool CheckStructureEqual(object item1, object item2)
        {
            // uses reflection to get underlying data, then checks the data
            if (item1 == null && item2 == null) return true;
            if (item1 == null || item2 == null) return false;
            
            if (item1.GetType() != item2.GetType()) return false;

            var baseType = item1.GetType();
            var baseComparer = GetBaseEqualityComparer(baseType);

            if (baseComparer == null)
            {
                // consider it as a compound type
                if (baseType.IsArray || baseType.GetInterfaces().Any(x => x.Name.StartsWith("IEnumerable")))
                {
                    return CheckEnumerableEqual(item1, item2);
                }

                if (baseType.IsClass || (baseType.IsValueType && !baseType.IsPrimitive))
                {
                    return CheckClassEqual(item1, item2);
                }
                else
                {
                    throw new Exception($"Could not compare structured data of type {baseType.Name}");
                }
            }
            else
            {
                return baseComparer(item1, item2, baseType);
            }
        }
    }
}
