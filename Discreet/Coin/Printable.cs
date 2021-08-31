using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    public static class Printable
    {
        public static string Prettify(string s)
        {
            int nBrace = 0;
            string rv = "";

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{' || s[i] == '[')
                {
                    if (i > 0 && s[i-1] == ':')
                    {
                        rv += "\n";
                        for (int j = 0; j < nBrace; j++)
                        {
                            rv += "  ";
                        }
                    }
                    rv += s[i];
                    rv += "\n";
                    nBrace++;
                    for (int j = 0; j < nBrace; j++)
                    {
                        rv += "  ";
                    }
                }
                else if (s[i] == '}' || s[i] == ']')
                {
                    rv += "\n";
                    nBrace--;
                    for (int j = 0; j < nBrace; j++)
                    {
                        rv += "  ";
                    }
                    rv += s[i];
                }
                else if (s[i] == ',')
                {
                    rv += s[i];
                    rv += "\n";
                    for (int j = 0; j < nBrace; j++)
                    {
                        rv += "  ";
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
            string rv = "";

            for(int i = 0; i < bytes.Length; i++)
            {
                rv += "0123456789ancdef"[bytes[i] >> 4];
                rv += "0123456789ancdef"[bytes[i] & 0xf];
            }

            return rv;
        }
    }
}
