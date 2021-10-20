﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Wallets
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

        public Coin.Transparent.TXOutput[] TInputs;
        public Coin.Transparent.TXOutput[] TOutputs;

        public Cipher.Key TransactionKey;
        public PTXInput[] PInputs;
        public Coin.TXOutput[] POutputs;
        public Coin.BulletproofPlus RangeProof;
        public Cipher.Key sumGammas;
        public ulong[] inputAmounts;
        public Cipher.Key[] TransactionKeys;
        public int[] DecodeIndices;

        public Coin.MixedTransaction Sign(WalletAddress addr)
        {
            return addr.SignTransaction(this);
        }

        public Coin.MixedTransaction ToMixed()
        {
            Coin.MixedTransaction tx = new Coin.MixedTransaction();
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
            tx.TSignatures = new Cipher.Signature[tx.NumTInputs];

            tx.TransactionKey = TransactionKey;
            tx.PInputs = new Coin.TXInput[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                tx.PInputs[i] = PInputs[i].Input;
            }
            tx.POutputs = POutputs;
            tx.RangeProof = RangeProof;
            tx.PseudoOutputs = new Cipher.Key[NumInputs];
            tx.PSignatures = new Coin.Triptych[NumPInputs];

            tx.SigningHash = tx.TXSigningHash();

            return tx;
        }
    }
}
