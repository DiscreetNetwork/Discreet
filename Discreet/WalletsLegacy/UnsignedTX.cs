using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using Discreet.Coin.Models;

namespace Discreet.WalletsLegacy
{
    public class UnsignedTX
    {
        public byte Version;
        public byte NumInputs;
        public byte NumOutputs;
        public byte NumSigs;

        public byte NumTInputs;
        public byte NumPInputs;
        public byte NumTOutputs;
        public byte NumPOutputs;

        public Cipher.SHA256 SigningHash;
        public ulong Fee;

        public TTXInput[] TInputs;
        public ScriptTXOutput[] TOutputs;

        public Cipher.Key TransactionKey;
        public PTXInput[] PInputs;
        public TXOutput[] POutputs;
        public BulletproofPlus RangeProof;
        public Cipher.Key sumGammas;
        public ulong[] inputAmounts;
        public Cipher.Key[] TransactionKeys;
        public int[] DecodeIndices;
        public bool[] IsCoinbase;

        public MixedTransaction Sign(WalletAddress addr)
        {
            return addr.SignTransaction(this);
        }

        public MixedTransaction ToMixed()
        {
            MixedTransaction tx = new MixedTransaction();
            tx.Version = Version;
            tx.NumInputs = NumInputs;
            tx.NumOutputs = NumOutputs;
            tx.NumSigs = NumSigs;

            tx.NumTInputs = NumTInputs;
            tx.NumPInputs = NumPInputs;
            tx.NumTOutputs = NumTOutputs;
            tx.NumPOutputs = NumPOutputs;

            tx.Fee = Fee;

            tx.TInputs = TInputs;
            tx.TOutputs = TOutputs;
            tx.TSignatures = (NumTInputs > 0) ? Enumerable.Range(0, tx.NumTInputs).Select(x => (byte)x).Zip(new Cipher.Signature[tx.NumTInputs]).ToArray() : Array.Empty<(byte, Cipher.Signature)>();

            tx.TransactionKey = TransactionKey;
            tx.PInputs = new TXInput[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                tx.PInputs[i] = PInputs[i].Input;
            }
            tx.POutputs = POutputs;
            tx.RangeProofPlus = RangeProof;
            tx.PseudoOutputs = new Cipher.Key[NumPInputs];
            tx.PSignatures = (NumPInputs > 0) ? new Triptych[NumPInputs] : null;

            tx.SigningHash = tx.TXSigningHash();

            return tx;
        }
    }
}
