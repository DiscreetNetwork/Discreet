using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public static class Printable
    {
        public static uint NUMSPACE = 4;
        
        public static string Prettify(string s)
        {
            int nBrace = 0;
            string rv = "";

            string spaces = "";

            for (int i = 0; i < NUMSPACE; i++)
            {
                spaces += " ";
            }

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{' || s[i] == '[')
                {
                    if (i > 0 && s[i-1] == ':')
                    {
                        rv += "\n";
                        for (int j = 0; j < nBrace; j++)
                        {
                            rv += spaces;
                        }
                    }
                    rv += s[i];
                    rv += "\n";
                    nBrace++;
                    for (int j = 0; j < nBrace; j++)
                    {
                        rv += spaces;
                    }
                }
                else if (s[i] == '}' || s[i] == ']')
                {
                    rv += "\n";
                    nBrace--;
                    for (int j = 0; j < nBrace; j++)
                    {
                        rv += spaces;
                    }
                    rv += s[i];
                }
                else if (s[i] == ',')
                {
                    rv += s[i];
                    rv += "\n";
                    for (int j = 0; j < nBrace; j++)
                    {
                        rv += spaces;
                    }
                }
                else
                {
                    rv += s[i];
                }
            }

            return rv;
        }

        public static string Hexify(byte[] bytes)
        {
            if (bytes == null) return "";

            string rv = "";

            for(int i = 0; i < bytes.Length; i++)
            {
                rv += "0123456789abcdef"[bytes[i] >> 4];
                rv += "0123456789abcdef"[bytes[i] & 0xf];
            }

            return rv;
        }

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

        public static byte[] Byteify(string hex)
        {
            if (hex == null) return new byte[0];

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
