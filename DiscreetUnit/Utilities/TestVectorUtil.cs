using Discreet.Cipher;
using Discreet.Cipher.Extensions;
using Discreet.Coin;
using Discreet.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscreetUnit.Utilities
{
    /// <summary>
    /// Provides a set of methods to generate test vectors. Assumes types implement ISerializable and have an associated JsonConverter where needed.
    /// </summary>
    internal static class TestVectorUtil
    {
        private static readonly Random rng;

        static TestVectorUtil()
        {
            rng = new Random(1024);
        }

        public static string GenerateConstructor(Type t, object? instance = null, bool random = true)
        {
            if (instance != null && t != instance?.GetType()) throw new Exception();
            var sb = new StringBuilder();
            GenerateConstructor(t, instance, sb, random);
            return sb.ToString();
        }

        private static void GenerateConstructor(Type t, object? instance, StringBuilder str, bool random = true)
        {
            if (GenerateBaseConstructor(t, str, random, instance))
            {
                return;
            }

            str.Append($"new {t.Name}");

            if (t.IsGenericType)
            {
                str.Append('<');

                var genericArguments = t.GetGenericArguments();
                str.Append(string.Join(", ", genericArguments.Select(x => x.Name)));

                str.Append('>');
            }

            if (t.IsArray || t.GetInterfaces().Any(x => x.Name.StartsWith("IEnumerable")))
            {
                var length = new Random().Next(8);
                List<object> elems = new();
                if (instance != null)
                {
                    length = 0;
                    foreach (var obj in (instance as IEnumerable) ?? Array.Empty<object>())
                    {
                        length++;
                        elems.Add(obj);
                    }
                }

                str.Append(" {");

                for (int i = 0; i < length; i++)
                {
                    GenerateConstructor(t.GetElementType() ?? (t.GetGenericArguments()?[0] ?? typeof(object)), elems[i], str, random);
                    if (i == length - 1) continue;
                    str.Append(", ");
                }

                str.Append('}');
            }
            else if (t.IsClass || (t.IsValueType && !t.IsPrimitive))
            {
                var props = t.GetProperties().Where(x => x.CanWrite && x.CanRead).ToArray();
                var fields = t.GetFields();

                str.Append('{');

                for (int i = 0; i < props.Length; i++)
                {
                    str.Append($"{props[i].Name} = ");
                    GenerateConstructor(props[i].PropertyType, instance == null ? null : props[i].GetValue(instance), str, random);
                    if (i == props.Length - 1) continue;
                    str.Append(", ");
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    str.Append($"{fields[i].Name} = ");
                    GenerateConstructor(fields[i].FieldType, instance == null ? null : fields[i].GetValue(instance), str, random);
                    if (i == fields.Length - 1) continue;
                    str.Append(", ");
                }

                str.Append('}');
            }
            
        }

        private static bool GenerateBaseConstructor(Type type, StringBuilder sb, bool random = true, object? obj = null)
        {
            if (!random)
            {
                if (obj == null) sb.Append("null");
                else if (type == typeof(sbyte)) sb.Append($"{(sbyte)obj}");
                else if (type == typeof(byte)) sb.Append($"{(byte)obj}");
                else if (type == typeof(bool)) sb.Append($"{(bool)obj}");
                else if (type == typeof(ushort)) sb.Append($"{(ushort)obj}");
                else if (type == typeof(short)) sb.Append($"{(short)obj}");
                else if (type == typeof(int)) sb.Append($"{(int)obj}");
                else if (type == typeof(uint)) sb.Append($"{(uint)obj}");
                else if (type == typeof(long)) sb.Append($"{(long)obj}");
                else if (type == typeof(ulong)) sb.Append($"{(ulong)obj}");
                else if (type == typeof(float)) sb.Append($"{(float)obj}");
                else if (type == typeof(double)) sb.Append($"{(double)obj}");
                else if (type == typeof(decimal)) sb.Append($"{(decimal)obj}");
                else if (type == typeof(string)) sb.Append($"\"{(string)obj}\"");
                else if (type == typeof(SHA256)) sb.Append($"SHA256.FromHex(\"{((SHA256)obj).ToHex()}\")");
                else if (type == typeof(Key)) sb.Append($"Key.FromHex(\"{((Key)obj).ToHex()}\")");
                else if (type == typeof(TAddress)) sb.Append($"new TAddress(\"{((TAddress)obj).ToString()}\")");
                else if (type == typeof(StealthAddress)) sb.Append($"new StealthAddress(\"{((StealthAddress)obj).ToString()}\")");
                else if (type == typeof(Signature)) sb.Append($"new Signature(Printable.Byteify(\"{((Signature)obj).ToHex()}\"))");
                else return false;

                return true;
            }

            if (type == typeof(sbyte)) sb.Append($"{rng.Next(sbyte.MinValue, sbyte.MaxValue)}");
            else if (type == typeof(byte)) sb.Append($"{rng.Next(byte.MinValue, byte.MaxValue)}");
            else if (type == typeof(bool)) sb.Append(rng.Next(2) == 1 ? "true" : "false");
            else if (type == typeof(ushort)) sb.Append($"{rng.Next(ushort.MinValue, ushort.MaxValue)}");
            else if (type == typeof(short)) sb.Append($"{rng.Next(short.MinValue, short.MaxValue)}");
            else if (type == typeof(int)) sb.Append($"{rng.Next(int.MinValue, int.MaxValue)}");
            else if (type == typeof(uint)) sb.Append($"{rng.Next((int)uint.MinValue, int.MaxValue)}");
            else if (type == typeof(long)) sb.Append($"{rng.NextInt64()}");
            else if (type == typeof(ulong)) sb.Append($"{rng.NextInt64()}");
            else if (type == typeof(float)) sb.Append($"{rng.NextSingle()}");
            else if (type == typeof(double)) sb.Append($"{rng.NextDouble()}");
            else if (type == typeof(decimal)) sb.Append($"{rng.NextDouble()}");
            else if (type == typeof(string)) sb.Append($"\"this is a random string\"");
            else if (type == typeof(SHA256)) sb.Append($"SHA256.FromHex(\"{SHA256.HashData(Randomness.Random(64)).ToHex()}\")");
            else if (type == typeof(Key)) sb.Append($"Key.FromHex(\"{KeyOps.GeneratePubkey().ToHex()}\")");
            else if (type == typeof(TAddress)) sb.Append($"new TAddress(\"{new TAddress(KeyOps.GeneratePubkey()).ToString()}\")");
            else if (type == typeof(StealthAddress)) sb.Append($"new StealthAddress(\"{new StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey()).ToString()}\")");
            else if (type == typeof(Signature)) sb.Append($"new Signature(\"{((Func<(Key, Key), Signature>)(((Key x, Key y) a) => new Signature(a.x, a.y, SHA256.HashData(Printable.Byteify("Discreet")))))(KeyOps.GenerateKeypair()).ToHex()}\")");
            else return false;

            return true;
        }
    }
}
