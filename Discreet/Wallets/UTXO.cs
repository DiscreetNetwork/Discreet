using System;
using System.Collections.Generic;
using System.Text;
using Discreet.Cipher;

namespace Discreet.Wallets
{
    public enum UTXOType: byte
    {
        //TRANSPARENT, currently unsupported in testnet. Private transactions only.
        STEALTH,
    }

    /**
     * <summary>This class stores UTXO data for wallets.</summary>
     */
    public class UTXO
    {
        /* fields are used as needed, depending on type. */
        public UTXOType Type;

        /* for all types */
        public SHA256 TransactionSrc;
        public int DecodeIndex;
        public Key TransactionKey;
        public ulong DecodedAmount;
        public ulong Amount;

        public bool Encrypted;

        /* stealth transactions */
        public uint Index;
        public Key UXKey;
        public Key UXSecKey;
        public Key Commitment;

        public int OwnedIndex;

        public byte[] Marshal()
        {
            byte[] bytes = new byte[145];

            bytes[0] = (byte)Type;
            Array.Copy(TransactionSrc.Bytes, 0, bytes, 1, 32);
            Coin.Serialization.CopyData(bytes, 33, DecodeIndex);
            Array.Copy(TransactionKey.bytes, 0, bytes, 37, 32);
            Coin.Serialization.CopyData(bytes, 69, Amount);

            Coin.Serialization.CopyData(bytes, 77, Index);
            Array.Copy(UXKey.bytes, 0, bytes, 81, 32);
            Array.Copy(Commitment.bytes, 0, bytes, 113, 32);

            return bytes;
        }

        public void Unmarshal(byte[] bytes)
        {
            TransactionSrc = new SHA256(bytes[1..33], false);
            TransactionKey = new Key(bytes[37..69]);
            UXKey = new Key(bytes[81..113]);
            Commitment = new Key(bytes[113..145]);

            Type = (UTXOType)bytes[0];

            DecodeIndex = Coin.Serialization.GetInt32(bytes, 33);
            DecodedAmount = 0;
            Encrypted = true;
            Amount = Coin.Serialization.GetUInt64(bytes, 69);
            Index = Coin.Serialization.GetUInt32(bytes, 77);
        }

        /* default constructor always sets UTXOType to STEALTH */
        public UTXO()
        {
            Type = UTXOType.STEALTH;
        }

        public UTXO(uint index, Coin.TXOutput output)
        {
            Type = UTXOType.STEALTH;
            Index = index;
            TransactionSrc = output.TransactionSrc;
            UXKey = output.UXKey;
            Commitment = output.Commitment;
            Amount = output.Amount;
            DecodedAmount = 0;
            UXSecKey = new Key(new byte[32]);

            Encrypted = true;
        }

        public UTXO(uint index, Coin.TXOutput output, Coin.Transaction tx, int i) : this(index, output)
        {
            TransactionKey = new Key(tx.Extra[2..34]);
            DecodeIndex = i;
        }

        public void Encrypt()
        {
            DecodedAmount = 0;
            Array.Clear(UXSecKey.bytes, 0, 32);

            Encrypted = true;
        }

        public void Decrypt(WalletAddress addr)
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
}
