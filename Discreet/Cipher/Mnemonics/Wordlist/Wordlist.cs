using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discreet.Cipher.Mnemonics.Wordlist
{
    public abstract class Wordlist
    {
        private string[] words;

        public Wordlist(string[] _words)
        {
            words = _words;
        }

        public bool WordExists(string word, out int index)
        {
            index = Array.IndexOf(words, word);

            return words.Contains(word);
        }

        public string GetWord(int index)
        {
            return words[index];
        }

        public int Length
        {
            get
            {
                return words.Length;
            }
        }
    }
}
