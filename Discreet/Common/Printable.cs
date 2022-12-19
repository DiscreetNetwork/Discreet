using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Discreet.Common defines utilities that are shared by multiple namespaces of Discreet.
/// </summary>
namespace Discreet.Common
{
    /// <summary>
    /// Printable provides static methods for handling different representations of string values.
    /// </summary>
    public static class Printable
    {
        /// <summary>
        /// ForEach calls an action on each of the elements in a collection.
        /// </summary>
        /// <param name="ie">A collection of elements.</param>
        /// <param name="action">The action to call on each of the elements.</param>
        private static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }

        /// <summary>
        /// Prettify adds indentation and newlines to a stringified JSON object.
        /// </summary>
        /// <param name="s">The string containing the JSON object.</param>
        /// <param name="useTabs">Should indentation use tabs instead of spaces?</param>
        /// <param name="numSpace">How many spaces to use when indenting, in case useTabs is false.</param>
        /// <returns>The prettified JSON object.</returns>
        public static string Prettify(string s, bool useTabs, int numSpace)
        {
            int nBrace = 0;
            StringBuilder rv = new();

            string spaces = "";

            bool quoted = false;

            if (useTabs)
            {
                spaces = "\t";
            }
            else
            {
                for (int i = 0; i < numSpace; i++)
                {
                    spaces += " ";
                }
            }

            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        rv.Append(ch);
                        if (!quoted)
                        {
                            if (ch == '[' && i < s.Length - 1 && s[i + 1] == ']')
                            {
                                rv.Append(']');
                                i++;
                            }
                            else if (ch == '{' && i < s.Length - 1 && s[i + 1] == '}')
                            {
                                rv.Append('}');
                                i++;
                            }
                            else
                            {
                                rv.AppendLine();
                                Enumerable.Range(0, ++nBrace).ForEach(item => rv.Append(spaces));
                            }
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            rv.AppendLine();
                            Enumerable.Range(0, --nBrace).ForEach(item => rv.Append(spaces));
                        }
                        rv.Append(ch);
                        break;
                    case '"':
                        rv.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && s[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        rv.Append(ch);
                        if (!quoted)
                        {
                            rv.AppendLine();
                            Enumerable.Range(0, nBrace).ForEach(item => rv.Append(spaces));
                        }
                        break;
                    case ':':
                        rv.Append(ch);
                        if (!quoted)
                        {
                            if (i < s.Length - 1 && (s[i + 1] == '[' || s[i + 1] == '{'))
                            {
                                if (i < s.Length - 2 && ((s[i + 1] == '[' && s[i + 2] == ']') || (s[i + 1] == '{' && s[i + 2] == '}')))
                                {
                                    rv.Append(' ');
                                    rv.Append(s[i + 1]);
                                    rv.Append(s[i + 2]);
                                    i += 2;
                                }
                                else
                                {
                                    rv.AppendLine();
                                    Enumerable.Range(0, nBrace).ForEach(item => rv.Append(spaces));
                                }
                            }
                            else
                            {
                                rv.Append(' ');
                            }
                        } 
                        break;
                    default:
                        rv.Append(ch);
                        break;
                }

            }

            return rv.ToString();
        }

        /// <summary>
        /// Prettify adds indentation and newlines to a stringified JSON object.
        /// Four spaces are used as a default indentation.
        /// </summary>
        /// <overloads>Prettify</overloads>
        /// <param name="s">The string containing the JSON object.</param>
        /// <returns>The prettified JSON object.</returns>
        public static string Prettify(string s)
        {
            return Prettify(s, false, 4);
        }

        /// <summary>
        /// Hexify transforms an array of bytes to a hexadecimal format.
        /// </summary>
        /// <param name="bytes">The bytes to be hexified.</param>
        /// <returns>The hexified string.</returns>
        public static string Hexify(byte[] bytes)
        {
            if (bytes == null) return "";

            StringBuilder rv = new(bytes.Length * 2);

            for(int i = 0; i < bytes.Length; i++)
            {
                rv.Append("0123456789abcdef"[bytes[i] >> 4]);
                rv.Append("0123456789abcdef"[bytes[i] & 0xf]);
            }

            return rv.ToString();
        }

        /// <summary>
        /// IsHex determines if a string represents a correctly formatted hexadecimal number or not.
        /// </summary>
        /// <param name="hex">The string to check.</param>
        /// <returns>True if the string represents a hexadecimal number, false otherwise.</returns>
        public static bool IsHex(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                return false;
            }

            for (int i = 0; i < hex.Length; i++)
            {
                if ("0123456789abcdef".IndexOf(hex[i]) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Byteify transforms a string representing a hexadecimal number into an array of bytes.
        /// </summary>
        /// <param name="hex">The stringified hexadecimal number.</param>
        /// <returns>An array of bytes equivalent to the hexadecimal number.</returns>
        public static byte[] Byteify(string hex)
        {
            if (hex == null) return Array.Empty<byte>();

            if (!IsHex(hex))
            {
                throw new Exception("Discreet.Coin.Printable: Byteify expects a hex string; this is not a hex string: " + hex);
            }

            byte[] rv = new byte[hex.Length / 2];

            for (int i = 0; i < rv.Length; i++)
            {
                rv[i] = (byte)(("0123456789abcdef".IndexOf(hex[2 * i]) << 4) | "0123456789abcdef".IndexOf(hex[2 * i + 1]));
            }

            return rv;
        }
    }
}
