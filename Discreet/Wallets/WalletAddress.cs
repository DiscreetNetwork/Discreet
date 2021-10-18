using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Coin;

namespace Discreet.Wallets
{
    public enum WalletType : byte
    {
        PRIVATE = 0,
        TRANSPARENT = 1,
    }

    /**
     * <summary>
     * Wallet addresses are generated either deterministically or randomly.
     * If deterministic, wallets will take in as a parameter the index, previous hash, and entropy.
     * </summary>
     */
    public class WalletAddress
    {
        public bool Encrypted;

        public byte Type;

        /* privacy */
        public Key PubSpendKey;
        public Key PubViewKey;

        public Key SecSpendKey;
        public Key SecViewKey;

        public byte[] EncryptedSecSpendKey;
        public byte[] EncryptedSecViewKey;

        /* transparent */
        public Key SecKey;
        public Key PubKey;

        public byte[] EncryptedSecKey;

        /* both */
        public string Address;

        public ulong Balance;

        /* UTXO data for the wallet. Stored in a local JSON database. */
        public List<UTXO> UTXOs;

        public StealthAddress GetAddress()
        {
            return new StealthAddress(PubViewKey, PubSpendKey);
        }

        public WalletAddress(bool random)
        {
            if (random)
            {
                SecSpendKey = KeyOps.GenerateSeckey();
                SecViewKey = KeyOps.GenerateSeckey();

                PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
                PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

                Address = new StealthAddress(PubViewKey, PubSpendKey).ToString();

                UTXOs = new List<UTXO>();
            }
        }

        /* default constructor, used for falling back on JSON deserialize */
        public WalletAddress() { }

        /**
         * hash is set to zero first.
         * spendsk1 = ScalarReduce(SHA256(SHA256(Entropy||hash||index||"spend")))
         * viewsk1 = ScalarReduce(SHA256(SHA256(Entropy||hash||index||"view")))
         * hash = SHA256(SHA256(spendsk1||viewsk1))
         * 
         * for transparent:
         * tsk1 = ScalarReduce(SHA256(SHA256(Entropy||hash||"transparent")))
         * hash = SHA256(SHA256(tsk1))
         * ...
         */
        public WalletAddress(byte[] entropy, byte[] hash, int index)
        {
            byte[] tmpspend = new byte[entropy.Length + hash.Length + 9];
            byte[] tmpview = new byte[entropy.Length + hash.Length + 8];
            byte[] indexBytes = BitConverter.GetBytes(index);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            Array.Copy(entropy, tmpspend, entropy.Length);
            Array.Copy(hash, 0, tmpspend, entropy.Length, hash.Length);
            Array.Copy(indexBytes, 0, tmpspend, entropy.Length + hash.Length, 4);

            Array.Copy(tmpspend, tmpview, entropy.Length + hash.Length + 4);

            Array.Copy(new byte[] { 0x73, 0x70, 0x65, 0x6E, 0x64 }, 0, tmpspend, entropy.Length + hash.Length + 4, 5);
            Array.Copy(new byte[] { 0x76, 0x69, 0x65, 0x77 }, 0, tmpview, entropy.Length + hash.Length + 4, 4);

            Cipher.HashOps.HashToScalar(ref SecSpendKey, Cipher.SHA256.HashData(tmpspend).Bytes, 32);
            Cipher.HashOps.HashToScalar(ref SecViewKey, Cipher.SHA256.HashData(tmpview).Bytes, 32);

            byte[] tmp = new byte[64];

            Array.Copy(SecSpendKey.bytes, tmp, 32);
            Array.Copy(SecViewKey.bytes, 0, tmp, 32, 32);

            byte[] newhash = Cipher.SHA256.HashData(Cipher.SHA256.HashData(tmp).Bytes).Bytes;

            Array.Copy(newhash, hash, 32);

            Array.Clear(tmpspend, 0, tmpspend.Length);
            Array.Clear(tmpview, 0, tmpview.Length);

            PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
            PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

            Address = new StealthAddress(PubViewKey, PubSpendKey).ToString();

            UTXOs = new List<UTXO>();
        }

        public void MustEncrypt(byte[] key)
        {
            if (Encrypted) throw new Exception("Discreet.Wallet.WalletAddress.Encrypt: wallet is already encrypted!");

            Encrypt(key);
        }

        public void Encrypt(byte[] key)
        {
            if (Encrypted) return;

            CipherObject cipherObjSpend = AESCBC.GenerateCipherObject(key);
            byte[] encryptedSecSpendKeyBytes = AESCBC.Encrypt(SecSpendKey.bytes, cipherObjSpend);
            EncryptedSecSpendKey = cipherObjSpend.PrependIV(encryptedSecSpendKeyBytes);

            CipherObject cipherObjView = AESCBC.GenerateCipherObject(key);
            byte[] encryptedSecViewKeyBytes = AESCBC.Encrypt(SecViewKey.bytes, cipherObjView);
            EncryptedSecViewKey = cipherObjView.PrependIV(encryptedSecViewKeyBytes);

            Array.Clear(SecSpendKey.bytes, 0, 32);
            Array.Clear(SecViewKey.bytes, 0, 32);

            for (int i = 0; i < UTXOs.Count; i++)
            {
                UTXOs[i].Encrypt();
            }

            Balance = 0;

            Encrypted = true;
        }

        public void MustDecrypt(byte[] key)
        {
            if (!Encrypted) throw new Exception("Discreet.Wallet.WalletAddress.Decrypt: wallet is not encrypted!");

            Decrypt(key);
        }

        public void Decrypt(byte[] key)
        {
            if (!Encrypted) return;

            (CipherObject cipherObjSpend, byte[] encryptedSecSpendKeyBytes) = CipherObject.GetFromPrependedArray(key, EncryptedSecSpendKey);
            byte[] unencryptedSpendKey = AESCBC.Decrypt(encryptedSecSpendKeyBytes, cipherObjSpend);
            Array.Copy(unencryptedSpendKey, SecSpendKey.bytes, 32);

            (CipherObject cipherObjView, byte[] encryptedSecViewKeyBytes) = CipherObject.GetFromPrependedArray(key, EncryptedSecViewKey);
            byte[] unencryptedViewKey = AESCBC.Decrypt(encryptedSecViewKeyBytes, cipherObjView);
            Array.Copy(unencryptedViewKey, SecViewKey.bytes, 32);

            for (int i = 0; i < UTXOs.Count; i++)
            {
                UTXOs[i].Decrypt(this);

                Balance += UTXOs[i].DecodedAmount;
            }

            Array.Clear(unencryptedSpendKey, 0, unencryptedSpendKey.Length);
            Array.Clear(unencryptedViewKey, 0, unencryptedViewKey.Length);

            Encrypted = false;
        }

        public void EncryptDropKeys()
        {
            Array.Clear(SecSpendKey.bytes, 0, 32);
            Array.Clear(SecViewKey.bytes, 0, 32);

            Encrypted = true;
        }

        public Transaction CreateTransaction(StealthAddress[] to, ulong[] amount)
        {
            return CreateTransaction(to, amount, (byte)Config.TransactionVersions.BP_PLUS);
        }

        public Transaction CreateTransaction(StealthAddress[] to, ulong[] amount, byte version)
        {
            if (version != 1 && version != 2)
            {
                throw new Exception($"Discreet.Wallets.Wallet.CreateTransaction: version cannot be {version}; currently supporting 1 and 2");
            }

            DB.DB db = DB.DB.GetDB();

            /* mahjick happens now. */
            if (Encrypted) throw new Exception("Discreet.Wallets.WalletAddress.CreateTransaction: Wallet is still encrypted!");
            int i = 0;

            ulong totalAmount = 0;
            for (i = 0; i < amount.Length; i++) totalAmount += amount[i];
            if (totalAmount > Balance)
            {
                throw new Exception($"Discreet.Wallets.Wallet.CreateTransaction: sending amount is greater than wallet balance ({totalAmount} > {Balance})");
            }

            /* Assemble pv and ps */
            Key[] pv = new Key[to.Length];
            Key[] ps = new Key[to.Length];

            for (i = 0; i < pv.Length; i++)
            {
                pv[i] = to[i].view;
                ps[i] = to[i].spend;
            }

            Key R = new Key(new byte[32]);
            Key r = new Key(new byte[32]);

            Key[] T = KeyOps.DKSAP(ref r, ref R, pv, ps);

            Transaction tx = new Transaction();

            /* get inputs */

            List<UTXO> inputs = new List<UTXO>();
            ulong neededAmount = 0;
            i = 0;
            while (neededAmount < totalAmount)
            {
                neededAmount += UTXOs[i].DecodedAmount;
                inputs.Add(UTXOs[i]);
                i++;
            }

            tx.Version = version;
            tx.NumInputs = (byte)inputs.Count;
            tx.NumOutputs = (byte)(to.Length + 1);
            tx.NumSigs = tx.NumInputs;

            /* for testnet, all tx fees are zero */
            tx.Fee = 0;

            /* assemble Extra */
            tx.ExtraLen = 34;
            tx.Extra = new byte[34];
            tx.Extra[0] = 1; // first byte is always 1.
            tx.Extra[1] = 0; // next byte indicates that no extra data besides TXKey is used in Extra.
            Array.Copy(R.bytes, 0, tx.Extra, 2, 32);

            /* assemble outputs */
            tx.Outputs = new TXOutput[to.Length + 1];

            Key[] gammas = new Key[to.Length + 1];

            for (i = 0; i < to.Length; i++)
            {
                tx.Outputs[i] = new TXOutput();
                tx.Outputs[i].UXKey = T[i];
                tx.Outputs[i].Commitment = new Key(new byte[32]);
                Key mask = KeyOps.GenCommitmentMask(ref r, ref to[i].view, i);
                KeyOps.GenCommitment(ref tx.Outputs[i].Commitment, ref mask, amount[i]);
                tx.Outputs[i].Amount = KeyOps.GenAmountMask(ref r, ref to[i].view, i, amount[i]);
                gammas[i] = mask;
            }

            /* assemble change output */
            tx.Outputs[i] = new TXOutput();
            tx.Outputs[i].UXKey = KeyOps.DKSAP(ref r, PubViewKey, PubSpendKey, i);
            tx.Outputs[i].Commitment = new Key(new byte[32]);
            Key rmask = KeyOps.GenCommitmentMask(ref r, ref PubViewKey, i);
            KeyOps.GenCommitment(ref tx.Outputs[i].Commitment, ref rmask, neededAmount - totalAmount);
            tx.Outputs[i].Amount = KeyOps.GenAmountMask(ref r, ref PubViewKey, i, neededAmount - totalAmount);
            gammas[i] = rmask;

            ulong[] amounts = new ulong[to.Length + 1];

            for (i = 0; i < to.Length; i++)
            {
                amounts[i] = amount[i];
            }

            amounts[i] = neededAmount - totalAmount;

            /* assemble range proof */
            if (tx.Version == 2)
            {
                Cipher.BulletproofPlus bp = Cipher.BulletproofPlus.Prove(amounts, gammas);
                tx.RangeProofPlus = new Coin.BulletproofPlus(bp);
            }
            else
            {
                Cipher.Bulletproof bp = Cipher.Bulletproof.Prove(amounts, gammas);
                tx.RangeProof = new Coin.Bulletproof(bp);
            }

            /* generate inputs and signatures */
            tx.Inputs = new TXInput[inputs.Count];
            tx.Signatures = new Coin.Triptych[inputs.Count];

            tx.PseudoOutputs = new Key[inputs.Count];

            Key[] blindingFactors = new Key[inputs.Count];

            Key sum = new Key(new byte[32]);
            Key tmp = new Key(new byte[32]);

            for (i = 0; i < inputs.Count - 1; i++)
            {
                blindingFactors[i] = KeyOps.GenerateSeckey();
                tx.PseudoOutputs[i] = new Key(new byte[32]);
                KeyOps.GenCommitment(ref tx.PseudoOutputs[i], ref blindingFactors[i], inputs[i].DecodedAmount);
            }

            /* tmp = sum + gammas[k]; sum = tmp; */
            for (int k = 0; k < tx.Outputs.Length; k++)
            {
                KeyOps.ScalarAdd(ref tmp, ref sum, ref gammas[k]);
                Array.Copy(tmp.bytes, sum.bytes, 32);
            }

            Key dif = new Key(new byte[32]);

            for (int k = 0; k < blindingFactors.Length; k++)
            {
                KeyOps.ScalarAdd(ref tmp, ref dif, ref blindingFactors[k]);
                Array.Copy(tmp.bytes, dif.bytes, 32);
            }

            Key xm = new Key(new byte[32]);
            KeyOps.ScalarSub(ref xm, ref sum, ref dif);
            tx.PseudoOutputs[i] = new Key(new byte[32]);
            KeyOps.GenCommitment(ref tx.PseudoOutputs[i], ref xm, inputs[i].DecodedAmount);

            blindingFactors[i] = xm;

            List<Key[]> Ms = new List<Key[]>();
            List<Key[]> Ps = new List<Key[]>();
            List<int> Ls = new List<int>();

            for (i = 0; i < inputs.Count; i++)
            {
                (TXOutput[] anonymitySet, int l) = db.GetMixins(inputs[i].Index);

                /* get ringsig params */
                Key[] M = new Key[64];
                Key[] P = new Key[64];

                for (int k = 0; k < 64; k++)
                {
                    M[k] = anonymitySet[k].UXKey;
                    P[k] = anonymitySet[k].Commitment;
                }

                /* generate inputs */
                tx.Inputs[i] = new TXInput();
                tx.Inputs[i].Offsets = new uint[64];
                for (int k = 0; k < 64; k++)
                {
                    tx.Inputs[i].Offsets[k] = anonymitySet[k].Index;
                }

                Key sign_r = new Key(new byte[32]);
                KeyOps.DKSAPRecover(ref sign_r, ref inputs[i].TransactionKey, ref SecViewKey, ref SecSpendKey, inputs[i].DecodeIndex);

                tx.Inputs[i].KeyImage = new Key(new byte[32]);
                KeyOps.GenerateLinkingTag(ref tx.Inputs[i].KeyImage, ref sign_r);

                Ms.Add(M);
                Ps.Add(P);
                Ls.Add(l);
            }

            for (i = 0; i < inputs.Count; i++)
            {
                /* get mixins */
                Key[] M = Ms[i];
                Key[] P = Ps[i];
                int l = Ls[i];

                Key C_offset = tx.PseudoOutputs[i];
                Key sign_r = new Key(new byte[32]);
                KeyOps.DKSAPRecover(ref sign_r, ref inputs[i].TransactionKey, ref SecViewKey, ref SecSpendKey, inputs[i].DecodeIndex);

                Key sign_x = KeyOps.GenCommitmentMaskRecover(ref inputs[i].TransactionKey, ref SecViewKey, inputs[i].DecodeIndex);
                Key sign_s = new(new byte[32]);
                /* s = zt = xt - x't */
                KeyOps.ScalarSub(ref sign_s, ref sign_x, ref blindingFactors[i]);

                Cipher.Triptych ringsig = Cipher.Triptych.Prove(M, P, C_offset, (uint)l, sign_r, sign_s, tx.SigningHash().ToKey());
                tx.Signatures[i] = new Coin.Triptych(ringsig);
            }

            return tx;
        }
    }
}
