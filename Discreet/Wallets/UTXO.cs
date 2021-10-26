using System;
using System.Collections.Generic;
using System.Text;
using Discreet.Cipher;

namespace Discreet.Wallets
{
    public enum UTXOType: byte
    {
        PRIVATE,
        TRANSPARENT, 
    }

    /**
     * <summary>This class stores UTXO data for wallets.</summary>
     */
    public class UTXO
    {
        /* fields are used as needed, depending on type. */
        public UTXOType Type;
        public bool IsCoinbase;

        /* for all types */
        public SHA256 TransactionSrc;
        public ulong Amount;

        public bool Encrypted;

        /* stealth transactions */
        public uint Index;
        public Key UXKey;
        public Key UXSecKey;
        public Key Commitment;
        public int DecodeIndex;
        public Key TransactionKey;
        public ulong DecodedAmount;

        public int OwnedIndex;

        public byte[] Marshal()
        {
            byte[] bytes = new byte[146];

            bytes[0] = (byte)Type;
            bytes[1] = (IsCoinbase) ? (byte)1 : (byte)0;
            Array.Copy(TransactionSrc.Bytes, 0, bytes, 2, 32);
            Coin.Serialization.CopyData(bytes, 34, DecodeIndex);
            Array.Copy(TransactionKey.bytes, 0, bytes, 38, 32);
            Coin.Serialization.CopyData(bytes, 70, Amount);

            Coin.Serialization.CopyData(bytes, 78, Index);
            Array.Copy(UXKey.bytes, 0, bytes, 82, 32);
            Array.Copy(Commitment.bytes, 0, bytes, 114, 32);

            return bytes;
        }

        public void Unmarshal(byte[] bytes)
        {
            TransactionSrc = new SHA256(bytes[2..34], false);
            TransactionKey = new Key(bytes[38..70]);
            UXKey = new Key(bytes[82..114]);
            Commitment = new Key(bytes[114..146]);

            Type = (UTXOType)bytes[0];
            IsCoinbase = bytes[1] == 1;

            DecodeIndex = Coin.Serialization.GetInt32(bytes, 34);
            DecodedAmount = 0;
            Encrypted = (Type == UTXOType.PRIVATE) ? true : false;
            Amount = Coin.Serialization.GetUInt64(bytes, 70);
            Index = Coin.Serialization.GetUInt32(bytes, 78);
        }

        /* default constructor always sets UTXOType to STEALTH */
        public UTXO()
        {
            Type = UTXOType.PRIVATE;
        }

        public UTXO(uint index, Coin.TXOutput output)
        {
            Type = UTXOType.PRIVATE;
            Index = index;
            TransactionSrc = output.TransactionSrc;
            UXKey = output.UXKey;
            Commitment = output.Commitment;
            Amount = output.Amount;
            DecodedAmount = 0;
            UXSecKey = new Key(new byte[32]);

            Encrypted = true;
        }

        public UTXO(Coin.Transparent.TXOutput output)
        {
            Type = UTXOType.TRANSPARENT;
            TransactionSrc = output.TransactionSrc;
            Amount = output.Amount;
            IsCoinbase = false;

            Encrypted = false;
        }

        public UTXO(uint index, Coin.TXOutput output, Coin.Transaction tx, int i) : this(index, output)
        {
            TransactionKey = tx.TransactionKey;
            DecodeIndex = i;
            IsCoinbase = false;
        }

        public UTXO(uint index, Coin.TXOutput output, Coin.MixedTransaction tx, int i) : this(index, output)
        {
            TransactionKey = tx.TransactionKey;
            DecodeIndex = i;
            IsCoinbase = false;
        }

        public UTXO(uint index, Coin.TXOutput output, Key txKey, int i) : this(index, output)
        {
            TransactionKey = txKey;
            DecodeIndex = i;
            IsCoinbase = false;
        }

        public UTXO(uint index, Coin.TXOutput output, Coin.Transaction tx, int i, bool isCoinbase) : this(index, output)
        {
            TransactionKey = tx.TransactionKey;
            DecodeIndex = i;
            IsCoinbase = isCoinbase;
        }

        public UTXO(uint index, Coin.TXOutput output, Coin.MixedTransaction tx, int i, bool isCoinbase) : this(index, output)
        {
            TransactionKey = tx.TransactionKey;
            DecodeIndex = i;
            IsCoinbase = isCoinbase;
        }

        public UTXO(uint index, Coin.TXOutput output, Key txKey, int i, bool isCoinbase) : this(index, output)
        {
            TransactionKey = txKey;
            DecodeIndex = i;
            IsCoinbase = isCoinbase;
        }


        public void Encrypt()
        {
            if (Type == UTXOType.PRIVATE)
            {
                DecodedAmount = 0;
                Array.Clear(UXSecKey.bytes, 0, 32);
            }

            Encrypted = true;
        }

        public void Decrypt(WalletAddress addr)
        {
            if (Type == UTXOType.PRIVATE)
            {
                if (IsCoinbase)
                {
                    DecodedAmount = Amount;
                }
                else
                {
                    byte[] cdata = new byte[36];
                    Array.Copy(KeyOps.ScalarmultKey(ref TransactionKey, ref addr.SecViewKey).bytes, cdata, 32);
                    Coin.Serialization.CopyData(cdata, 32, DecodeIndex);

                    Key c = new Key(new byte[32]);
                    HashOps.HashToScalar(ref c, cdata, 36);

                    byte[] gdata = new byte[38];
                    gdata[0] = (byte)'a';
                    gdata[1] = (byte)'m';
                    gdata[2] = (byte)'o';
                    gdata[3] = (byte)'u';
                    gdata[4] = (byte)'n';
                    gdata[5] = (byte)'t';
                    Array.Copy(c.bytes, 0, gdata, 6, 32);

                    Key g = new Key(new byte[32]);
                    HashOps.HashData(ref g, gdata, 38);

                    /* DecodedAmount = g ^ Amount */
                    /* Amount stores the encrypted amount. */
                    DecodedAmount = KeyOps.XOR8(ref g, Amount);

                    KeyOps.DKSAPRecover(ref UXSecKey, ref TransactionKey, ref addr.SecViewKey, ref addr.SecSpendKey, DecodeIndex);
                }
            }

            Encrypted = false;
        }
    }
}
