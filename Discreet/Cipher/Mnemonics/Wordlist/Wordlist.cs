using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discreet.Cipher.Mnemonics.Wordlist
{
    /// <summary>
    /// Stores the list of words for a language a mnemonic can be represented in. 
    /// </summary>
    public abstract class Wordlist
    {
        private string[] words;

        public Wordlist(string[] _words)
        {
            words = _words;
        }

        /// <summary>
        /// Checks whether or not the word exists in the Wordlist. Outputs the index it can be found at.
        /// </summary>
        /// <param name="word">the word to check.</param>
        /// <param name="index">the index the word can be found at.</param>
        /// <returns>true if the word exists in the Wordlist; false otherwise.</returns>
        public bool WordExists(string word, out int index)
        {
            index = Array.IndexOf(words, word);

            return words.Contains(word);
        }

        /// <summary>
        /// Gets the word in the Wordlist at the specified index.
        /// </summary>
        /// <param name="index">the index to find the word at.</param>
        /// <returns>The word at the specified index.</returns>
        public string GetWord(int index)
        {
            return words[index];
        }

        /// <summary>
        /// The length of the list of words.
        /// </summary>
        public int Length
        {
            get
            {
                return words.Length;
            }
        }
    }
}
