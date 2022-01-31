using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Discreet.Cipher.Mnemonics
{
    /// <summary>
    /// Represents a BIP39-style mnemonic.
    /// </summary>
    public class Mnemonic
    {
        /// <summary>
        /// Represents the language a mnemonic is in.
        /// </summary>
        public enum Language { English, Japanese, Spanish, ChineseSimplified, ChineseTraditional, French, Unknown };

        /// <summary>
        /// The minimum number of bits a mnemonic should be.
        /// </summary>
        public const int EntropyMin = 128;

        /// <summary>
        /// The maximum number of bits a mnemonic should be.
        /// </summary>
        public const int EntropyMax = 256;

        /// <summary>
        /// The seed from which the mnemonic is derived from.
        /// </summary>
        private byte[] entropy;

        private Language language;
        
        private string[] words;

        private Wordlist.Wordlist wordlist;

        /// <summary>
        /// The language the mnemonic is in.
        /// </summary>
        public Language Lang
        {
            get
            {
                return language;
            }
        }

        /// <summary>
        /// The words of the mnemonic, derived from the entropy.
        /// </summary>
        public string[] Words
        {
            get
            {
                return words;
            }
        }

        /// <summary>
        /// Checks if the number of bits in the mnemonic is valid. Currently, mnemonic bits must be:
        /// <br />
        /// <list type="bullet"><item>divisible by 32</item><item>less than the EntropyMin and EntropyMax, which are 128 and 256</item><item>either 128 or 256</item></list>
        /// Throws an Exception if the bits are invalid.
        /// </summary>
        /// <param name="bits">The parameter to check</param>
        private void CheckEntropyBits(int bits)
        {
            if (bits % 32 != 0)
            {
                throw new Exception("Discreet.Cipher.Mnemonics.Mnemonic: Cannot create mnemonic with bit width not divisible by 32!");
            }

            if (bits < EntropyMin || bits > EntropyMax)
            {
                throw new Exception($"Discreet.Cipher.Mnemonics.Mnemonic: Cannot create mnemonic with bit width {bits}; width must be between {EntropyMin} and {EntropyMax}");
            }

            /* for now we only allow 128 and 256 bits */
            if (bits != 128 && bits != 256)
            {
                throw new Exception($"Discreet.Cipher.Mnemonics.Mnemonic does not support any bit width besides 128 and 256 bits right now.");
            }
        }

        /// <summary>
        /// Gets the Wordlist associated with the language.
        /// </summary>
        /// <param name="_language">The language to get the Wordlist for.</param>
        /// <returns>The Wordlist for the language.</returns>
        private Wordlist.Wordlist GetWordlist(Language _language)
        {
            return _language switch
            {
                Language.English => new Wordlist.English(),
                Language.Japanese => new Wordlist.Japanese(),
                Language.Spanish => new Wordlist.Spanish(),
                Language.ChineseSimplified => new Wordlist.ChineseSimplified(),
                Language.ChineseTraditional => new Wordlist.ChineseTraditional(),
                Language.French => new Wordlist.French(),
                _ => new Wordlist.English(),
            };
        }

        /// <summary>
        /// Creates a new mnemonic with the specified language and number of bits.
        /// </summary>
        /// <param name="bits">The length of the mnemonic to generate, in bits.</param>
        /// <param name="_language">The language the mnemonic should be in.</param>
        public Mnemonic(int bits, Language _language)
        {
            CheckEntropyBits(bits);

            entropy = Randomness.Random((uint)bits / 8);
            language = _language;

            wordlist = GetWordlist(language);

            /* ensure words field is set */
            GetMnemonic();
        }

        /// <summary>
        /// Generates a mnemonic from a string of words. Throws an Exception if the argument is in an unsupported language or is invalid.
        /// </summary>
        /// <param name="mnemonic">The words in the mnemonic.</param>
        public Mnemonic(string mnemonic)
        {
            words = mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            language = AutoDetectLanguageOfWords(words);

            if (language == Language.Unknown)
            {
                throw new Exception($"Discreet.Cipher.Mnemonics.Mnemonic: unknown language for mnemonic \"{mnemonic}\"");
            }

            wordlist = GetWordlist(language);

            GetEntropy();
        }

        /// <summary>
        /// Generates a mnemonic from the words. Throws an Exception if the argument is in an unsupported language or is invalid.
        /// </summary>
        /// <param name="_words">The words in the mnemonic.</param>
        public Mnemonic(string[] _words) : this(string.Join(' ', _words)) { }

        /// <summary>
        /// Generates a mnemonic with the specified entropy and language.
        /// </summary>
        /// <param name="_entropy">The entropy of the mnemonic.</param>
        /// <param name="_language">The language the mnemonic should be in.</param>
        public Mnemonic(byte[] _entropy, Language _language)
        {
            CheckEntropyBits(_entropy.Length * 8);

            entropy = _entropy;
            language = _language;

            wordlist = GetWordlist(language);

            /* ensure words field is set */
            GetMnemonic();
        }

        /// <summary>
        /// Generates a mnemonic from the specified entropy, in English.
        /// </summary>
        /// <param name="_entropy">The entropy of the mnemonic.</param>
        public Mnemonic(byte[] _entropy) : this(_entropy, Language.English) { }

        /// <summary>
        /// Tries to get the language the array of words is in.
        /// </summary>
        /// <param name="words">The words to check.</param>
        /// <returns>The Language the words are in, or Language.Unknown if multiple languages or no language is detected.</returns>
        public static Language AutoDetectLanguageOfWords(string[] words)
        {
            Wordlist.English eng = new Wordlist.English();
            Wordlist.Japanese jp = new Wordlist.Japanese();
            Wordlist.Spanish es = new Wordlist.Spanish();
            Wordlist.French fr = new Wordlist.French();
            Wordlist.ChineseSimplified cnS = new Wordlist.ChineseSimplified();
            Wordlist.ChineseTraditional cnT = new Wordlist.ChineseTraditional();

            List<int> languageCount = new List<int>(new int[] { 0, 0, 0, 0, 0, 0 });
            int index;

            foreach (string s in words)
            {
                if (eng.WordExists(s, out index))
                {
                    //english is at 0
                    languageCount[0]++;
                }

                if (jp.WordExists(s, out index))
                {
                    //japanese is at 1
                    languageCount[1]++;
                }

                if (es.WordExists(s, out index))
                {
                    //spanish is at 2
                    languageCount[2]++;
                }

                if (cnS.WordExists(s, out index))
                {
                    //chinese simplified is at 3
                    languageCount[3]++;
                }

                if (cnT.WordExists(s, out index) && !cnS.WordExists(s, out index))
                {
                    //chinese traditional is at 4
                    languageCount[4]++;
                }

                if (fr.WordExists(s, out index))
                {
                    //french is at 5
                    languageCount[5]++;
                }
            }

            //no hits found for any language unknown
            if (languageCount.Max() == 0)
            {
                return Language.Unknown;
            }

            if (languageCount.IndexOf(languageCount.Max()) == 0)
            {
                return Language.English;
            }
            else if (languageCount.IndexOf(languageCount.Max()) == 1)
            {
                return Language.Japanese;
            }
            else if (languageCount.IndexOf(languageCount.Max()) == 2)
            {
                return Language.Spanish;
            }
            else if (languageCount.IndexOf(languageCount.Max()) == 3)
            {
                if (languageCount[4] > 0)
                {
                    //has traditional characters so not simplified but instead traditional
                    return Language.ChineseTraditional;
                }

                return Language.ChineseSimplified;
            }
            else if (languageCount.IndexOf(languageCount.Max()) == 4)
            {
                return Language.ChineseTraditional;
            }
            else if (languageCount.IndexOf(languageCount.Max()) == 5)
            {
                return Language.French;
            }

            return Language.Unknown;
        }

        /// <summary>
        /// Tries to get the string of words the mnemonic represents. <br />If Words is null, it generates the mnemonic string.
        /// </summary>
        /// <returns>The string of words the mnemonic represents.</returns>
        public string GetMnemonic()
        {
            if (words != null)
            {
                return string.Join(' ', words);
            }

            int entropyBits = entropy.Length * 8;
            int checksumBits = entropyBits / 32;
            int sentenceLength = (entropyBits + checksumBits) / 11;

            byte[] checksum = SHA256.HashData(entropy).Bytes;

            byte[] csharpsucks = (byte[])entropy.Clone();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(csharpsucks);
            }

            byte[] trueEntropy = new byte[entropy.Length + 1];
            Array.Copy(csharpsucks, trueEntropy, entropy.Length);

            BigInteger bigData = new BigInteger(trueEntropy);

            for (int i = 0; i < checksumBits; i++)
            {
                bigData *= 2;

                if ((checksum[i / 8] & (1 << (7 - (i % 8)))) > 0) {
                    bigData |= 1;
                }
            }

            BigInteger word;

            words = new string[sentenceLength];

            for (int i = sentenceLength - 1; i >= 0; i--)
            {
                word = bigData & 2047;
                bigData /= 2048;

                words[i] = wordlist.GetWord((int)word);
            }

            return string.Join(' ', words);
        }

        /// <summary>
        /// Pads or shortens the specified byte array. Used internally for generating the entropy from a mnemonic string.
        /// </summary>
        /// <param name="data">The array to pad or shorten.</param>
        /// <param name="len">The specified length the resulting array should be.</param>
        /// <returns>The resulting byte array, prepended with zeros to be the specified length.</returns>
        private static byte[] PadOrShortenByteArray(byte[] data, int len)
        {
            if (data.Length > len)
            {
                return data[0..len];
            }

            byte[] rv = new byte[len];
            Array.Copy(data, 0, rv, len - data.Length, data.Length);
            return rv;
        }

        /// <summary>
        /// Tries to get the entropy of the mnemonic. <br />If it isn't generated yet, it generates the entropy from the mnemonic string.<br />Throws an Exception if the mnemonic string is invalid, or if the checksum is incorrect.
        /// </summary>
        /// <returns>The entropy of the mnemonic.</returns>
        /// <remarks>WARNING: This returns a reference to the actual entropy of the mnemonic, so avoid mutating the return value.</remarks>
        public byte[] GetEntropy()
        {
            if (entropy != null)
            {
                return entropy;
            }

            int index;
            BigInteger bigData = 0;

            for (int i = 0; i < words.Length; i++)
            {
                if (!wordlist.WordExists(words[i], out index))
                {
                    throw new Exception($"Discreet.Cipher.Mnemonics.Mnemonic: GetEntropy does not recognize word {words[i]} in the {language} language wordlist");
                }

                bigData *= 2048;
                bigData |= index;
            }

            int checksumBits = ((words.Length - 12) / 3) + 4;
            BigInteger checksum = bigData & ((1 << checksumBits) - 1);
            bigData /= (1 << checksumBits);

            entropy = bigData.ToByteArray();

            /* c# bigintegers are the dumbest things ever. Why make them signed!?? */
            if (entropy.Length > (words.Length / 3 * 4))
            {
                entropy = PadOrShortenByteArray(entropy, words.Length / 3 * 4);
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(entropy);
            }

            /* call it twice because, as I said previously, c# sucks */
            entropy = PadOrShortenByteArray(entropy, words.Length / 3 * 4);

            byte[] computedChecksumBytes = SHA256.HashData(entropy).Bytes;
            int computedChecksum = computedChecksumBytes[0];
            
            if (words.Length != 24)
            {
                computedChecksum /= (1 << ((24 - words.Length) / 3));
            }

            if ((int)checksum != computedChecksum)
            {
                //throw new Exception($"Discreet.Cipher.Mnemonics.Mnemonic: GetEntropy could not recover checksum {checksum}; instead got {(computedChecksumBytes[0] & ((1 << checksumBits) - 1))}");
                throw new Exception($"Discreet.Cipher.Mnemonics.Mnemonic: GetEntropy could not recover checksum {checksum}; instead got {computedChecksum}");
            }

            return entropy;
        }
    }
}
