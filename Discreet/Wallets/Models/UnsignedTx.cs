using Discreet.Coin;
using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Models
{
    public class UnsignedTx
    {
        public byte Version;
        public byte NumInputs;
        public byte NumOutputs;
        public byte NumSigs;

        public byte NumTInputs;
        public byte NumPInputs;
        public byte NumTOutputs;
        public byte NumPOutputs;

        public SHA256 SigningHash;
        public ulong Fee;

        public Discreet.Coin.Transparent.TXInput[] TInputs;
        public Discreet.Coin.Transparent.TXOutput[] TOutputs;

        public Key TransactionKey;
        public PrivateTxInput[] PInputs;
        public TXOutput[] POutputs;
        public Discreet.Coin.BulletproofPlus RangeProof;
        public Key sumGammas;
        public ulong[] inputAmounts;
        public Key[] TransactionKeys;
        public int[] DecodeIndices;
        public bool[] IsCoinbase;

        public MixedTransaction ToMixed()
        {
            MixedTransaction tx = new();
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
            tx.TSignatures = (NumTInputs > 0) ? new Signature[tx.NumTInputs] : null;

            tx.TransactionKey = TransactionKey;
            tx.PInputs = new TXInput[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                tx.PInputs[i] = PInputs[i].Input;
            }
            tx.POutputs = POutputs;
            tx.RangeProofPlus = RangeProof;
            tx.PseudoOutputs = new Key[NumPInputs];
            tx.PSignatures = (NumPInputs > 0) ? new Discreet.Coin.Triptych[NumPInputs] : null;

            tx.SigningHash = tx.TXSigningHash();

            return tx;
        }
    }
}
