﻿using System;
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

        public bool Deterministic;

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

        public IAddress GetAddress()
        {
            return (Type == 0) ? new StealthAddress(PubViewKey, PubSpendKey) : new TAddress(PubKey);
        }

        public WalletAddress(byte type, bool random)
        {
            Type = type; 

            if (random)
            {
                if (type == 0)
                {
                    SecSpendKey = KeyOps.GenerateSeckey();
                    SecViewKey = KeyOps.GenerateSeckey();

                    PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
                    PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

                    Address = new StealthAddress(PubViewKey, PubSpendKey).ToString();
                }
                else if (type == 1)
                {
                    SecKey = Cipher.KeyOps.GenerateSeckey();
                    PubKey = Cipher.KeyOps.ScalarmultBase(ref SecKey);
                    Address = new TAddress(PubKey).ToString();
                }
                else
                {
                    throw new Exception("Discreet.Wallets.WalletAddress: unknown transaction type " + Type);
                }

                UTXOs = new List<UTXO>();
            }

            Deterministic = !random;
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
         * tsk1 = ScalarReduce(SHA256(SHA256(Entropy||hash||index||"transparent")))
         * hash = SHA256(SHA256(tsk1))
         * ...
         */
        public WalletAddress(byte type, byte[] entropy, byte[] hash, int index)
        {
            Type = type;

            if (type == 0)
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

                Array.Clear(tmp, 0, tmp.Length);

                PubSpendKey = Cipher.KeyOps.ScalarmultBase(ref SecSpendKey);
                PubViewKey = Cipher.KeyOps.ScalarmultBase(ref SecViewKey);

                Address = new StealthAddress(PubViewKey, PubSpendKey).ToString();
            }
            else if (type == 1)
            {
                byte[] tmpsec = new byte[entropy.Length + hash.Length + 15];
                byte[] indexBytes = BitConverter.GetBytes(index);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(indexBytes);
                }

                Array.Copy(entropy, tmpsec, entropy.Length);
                Array.Copy(hash, 0, tmpsec, entropy.Length, hash.Length);
                Array.Copy(indexBytes, 0, tmpsec, entropy.Length + hash.Length, 4);

                Array.Copy(new byte[] { 0x74, 0x72, 0x61, 0x6E, 0x73, 0x70, 0x61, 0x72, 0x65, 0x6E, 0x74 }, 0, tmpsec, entropy.Length + hash.Length + 4, 11);

                Cipher.HashOps.HashToScalar(ref SecKey, Cipher.SHA256.HashData(tmpsec).Bytes, 32);

                byte[] newhash = Cipher.SHA256.HashData(Cipher.SHA256.HashData(SecKey.bytes).Bytes).Bytes;

                Array.Copy(newhash, hash, 32);

                PubKey = Cipher.KeyOps.ScalarmultBase(ref SecKey);

                Address = new TAddress(PubKey).ToString();
            }
            else
            {
                throw new Exception("Discreet.Wallets.WalletAddress: unknown transaction type " + Type);
            }

            UTXOs = new List<UTXO>();

            Deterministic = true;
        }

        public void MustEncrypt(byte[] key)
        {
            if (Encrypted) throw new Exception("Discreet.Wallet.WalletAddress.Encrypt: wallet is already encrypted!");

            Encrypt(key);
        }

        public void Encrypt(byte[] key)
        {
            if (Encrypted) return;

            if (Type == 0)
            {
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
            }
            else if (Type == 1)
            {
                CipherObject cipherObjSec = AESCBC.GenerateCipherObject(key);
                byte[] encryptedSecKeyBytes = AESCBC.Encrypt(SecKey.bytes, cipherObjSec);
                EncryptedSecKey = cipherObjSec.PrependIV(encryptedSecKeyBytes);

                Array.Clear(SecKey.bytes, 0, 32);
            }
            else
            {
                throw new Exception("Discreet.Wallets.WalletAddress: unknown transaction type " + Type);
            }

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

            if (Type == 0)
            {
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
            }
            else if (Type == 1)
            {
                (CipherObject cipherObjSec, byte[] encryptedSecKeyBytes) = CipherObject.GetFromPrependedArray(key, EncryptedSecKey);
                byte[] unencryptedSecKey = AESCBC.Decrypt(encryptedSecKeyBytes, cipherObjSec);
                Array.Copy(unencryptedSecKey, SecKey.bytes, 32);

                Array.Clear(unencryptedSecKey, 0, unencryptedSecKey.Length);
            }
            else
            {
                throw new Exception("Discreet.Wallets.WalletAddress: unknown transaction type " + Type);
            }

            Encrypted = false;
        }

        public void EncryptDropKeys()
        {
            if (Type == 0)
            {
                Array.Clear(SecSpendKey.bytes, 0, 32);
                Array.Clear(SecViewKey.bytes, 0, 32);
            }
            else if (Type == 1)
            {
                Array.Clear(SecKey.bytes, 0, 32);
            }
            else
            {
                throw new Exception("Discreet.Wallets.WalletAddress: unknown transaction type " + Type);
            }

            Encrypted = true;
        }

        public byte GetTransactionType(IAddress[] to)
        {
            Config.TransactionVersions ver = (Type == 0) ? Config.TransactionVersions.BP_PLUS : Config.TransactionVersions.TRANSPARENT;

            foreach (IAddress addr in to)
            {
                if (addr.Type() == (byte)AddressType.STEALTH)
                {
                    if (ver == Config.TransactionVersions.TRANSPARENT)
                    {
                        ver = Config.TransactionVersions.MIXED;
                        break;
                    }

                    ver = Config.TransactionVersions.BP_PLUS;
                }
                else if (addr.Type() == (byte)AddressType.TRANSPARENT)
                {
                    if (ver != Config.TransactionVersions.TRANSPARENT)
                    {
                        ver = Config.TransactionVersions.MIXED;
                        break;
                    }

                    ver = Config.TransactionVersions.TRANSPARENT;
                }
            }

            return (byte)ver;
        }

        public StealthAddress[] GetStealthAddresses(IAddress[] to)
        {
            StealthAddress[] addresses = new StealthAddress[to.Length];

            for (int i = 0; i < to.Length; i++)
            {
                addresses[i] = new StealthAddress(to[i].Bytes());
            }

            return addresses;
        }

        public TAddress[] GetTAddresses(IAddress[] to)
        {
            TAddress[] addresses = new TAddress[to.Length];

            for (int i = 0; i < to.Length; i++)
            {
                addresses[i] = new TAddress(to[i].Bytes());
            }

            return addresses;
        }

        public MixedTransaction CreateTransaction(IAddress[] to, ulong[] amount)
        {
            switch (GetTransactionType(to))
            {
                case 1:
                case 2:
                    return new MixedTransaction(CreateTransaction(GetStealthAddresses(to), amount));
                case 3:
                    return new MixedTransaction(CreateTransaction(GetTAddresses(to), amount));
                case 4:
                    break;
                default:
                    throw new Exception("Discreet.Wallets.WalletAddress.CreateTransaction: unsupported transaction type");
            }


            UnsignedTX utx = CreateUnsignedTransaction(to, amount);
            return SignTransaction(utx);
        }

        public MixedTransaction SignTransaction(UnsignedTX utx)
        {
            MixedTransaction tx = utx.ToMixed();

            Key[] blindingFactors = new Key[tx.NumInputs];
            Key tmp = new Key(new byte[32]);
            for (int i = 0; i < tx.NumInputs - 1; i++)
            {
                blindingFactors[i] = KeyOps.GenerateSeckey();
                tx.PseudoOutputs[i] = new Key(new byte[32]);
                KeyOps.GenCommitment(ref tx.PseudoOutputs[i], ref blindingFactors[i], utx.inputAmounts[i]);
            }

            Key dif = new Key(new byte[32]);
            for (int k = 0; k < blindingFactors.Length; k++)
            {
                KeyOps.ScalarAdd(ref tmp, ref dif, ref blindingFactors[k]);
                Array.Copy(tmp.bytes, dif.bytes, 32);
            }

            Key xm = new Key(new byte[32]);
            KeyOps.ScalarSub(ref xm, ref utx.sumGammas, ref dif);
            tx.PseudoOutputs[tx.NumInputs - 1] = new Key(new byte[32]);
            KeyOps.GenCommitment(ref tx.PseudoOutputs[tx.NumInputs - 1], ref xm, utx.inputAmounts[tx.NumInputs - 1]);
            blindingFactors[tx.NumInputs - 1] = xm;

            if (Type == (byte)WalletType.PRIVATE)
            {
                for (int i = 0; i < tx.NumInputs; i++)
                {
                    Key C_offset = tx.PseudoOutputs[i];
                    Key sign_r = new Key(new byte[32]);
                    KeyOps.DKSAPRecover(ref sign_r, ref utx.TransactionKeys[i], ref SecViewKey, ref SecSpendKey, utx.DecodeIndices[i]);
                    Key sign_x = KeyOps.GenCommitmentMaskRecover(ref utx.TransactionKeys[i], ref SecViewKey, utx.DecodeIndices[i]);

                    if (utx.IsCoinbase[i])
                    {
                        sign_x = Key.I;
                    }
                    
                    Key sign_s = new(new byte[32]);
                    /* s = zt = xt - x't */
                    KeyOps.ScalarSub(ref sign_s, ref sign_x, ref blindingFactors[i]);

                    Cipher.Triptych ringsig = Cipher.Triptych.Prove(utx.PInputs[i].M, utx.PInputs[i].P, C_offset, (uint)utx.PInputs[i].l, sign_r, sign_s, tx.SigningHash.ToKey());
                    tx.PSignatures[i] = new Coin.Triptych(ringsig);
                }
            }
            else if (Type == (byte)WalletType.TRANSPARENT)
            {
                for (int i = 0; i < tx.NumInputs; i++)
                {
                    tx.TSignatures[i] = new Signature(SecKey, PubKey, tx.SigningHash);
                }
            }

            return tx;
        }

        public UnsignedTX CreateUnsignedTransaction(IAddress[] to, ulong[] amount)
        {
            DB.DB db = DB.DB.GetDB();

            UnsignedTX utx = new();
            utx.Version = 4;
            utx.Fee = 0;

            ulong totalAmount = 0;
            for (int i = 0; i < amount.Length; i++) totalAmount += amount[i];
            if (totalAmount > Balance)
            {
                throw new Exception($"Discreet.Wallets.WalletAddress.CreateTransaction: sending amount is greater than wallet balance ({totalAmount} > {Balance})");
            }
            List<UTXO> inputs = new List<UTXO>();
            ulong neededAmount = 0;
            int j = 0;

            if (Type == (byte)WalletType.PRIVATE)
            {
                while (neededAmount < totalAmount)
                {
                    neededAmount += UTXOs[j].DecodedAmount;
                    inputs.Add(UTXOs[j]);
                    j++;
                }

                utx.NumInputs = (byte)j;
                utx.NumOutputs = (byte)((neededAmount == totalAmount) ? to.Length : to.Length + 1);
                utx.NumSigs = utx.NumInputs;
                utx.NumPInputs = utx.NumInputs;
                utx.NumPOutputs = 0;
                utx.NumTInputs = 0;
                utx.NumTOutputs = 0;

                /* construct inputs */
                utx.PInputs = new PTXInput[utx.NumInputs];
                utx.TransactionKeys = new Key[utx.NumInputs];
                utx.DecodeIndices = new int[utx.NumInputs];
                utx.inputAmounts = new ulong[utx.NumInputs];
                utx.IsCoinbase = new bool[utx.NumInputs];
                for (int i = 0; i < utx.NumInputs; i++)
                {
                    (TXOutput[] anonymitySet, int l) = db.GetMixins(inputs[i].Index);

                    utx.PInputs[i] = new PTXInput();
                    utx.PInputs[i].l = l;
                    /* get ringsig params */
                    utx.PInputs[i].M = new Key[64];
                    utx.PInputs[i].P = new Key[64];

                    for (int k = 0; k < 64; k++)
                    {
                        utx.PInputs[i].M[k] = anonymitySet[k].UXKey;
                        utx.PInputs[i].P[k] = anonymitySet[k].Commitment;
                    }

                    /* generate inputs */
                    utx.PInputs[i].Input = new TXInput();
                    utx.PInputs[i].Input.Offsets = new uint[64];
                    for (int k = 0; k < 64; k++)
                    {
                        utx.PInputs[i].Input.Offsets[k] = anonymitySet[k].Index;
                    }

                    Key sign_r = new Key(new byte[32]);
                    KeyOps.DKSAPRecover(ref sign_r, ref inputs[i].TransactionKey, ref SecViewKey, ref SecSpendKey, inputs[i].DecodeIndex);

                    utx.PInputs[i].Input.KeyImage = new Key(new byte[32]);
                    KeyOps.GenerateLinkingTag(ref utx.PInputs[i].Input.KeyImage, ref sign_r);

                    utx.inputAmounts[i] = inputs[i].DecodedAmount;
                    utx.TransactionKeys[i] = inputs[i].TransactionKey;
                    utx.DecodeIndices[i] = inputs[i].DecodeIndex;
                    utx.IsCoinbase[i] = inputs[i].IsCoinbase;
                }
            }
            else if (Type == (byte)WalletType.TRANSPARENT)
            {
                while (neededAmount < totalAmount)
                {
                    neededAmount += UTXOs[j].Amount;
                    inputs.Add(UTXOs[j]);
                    j++;
                }

                utx.NumInputs = (byte)j;
                utx.NumOutputs = (byte)((neededAmount == totalAmount) ? to.Length : to.Length + 1);
                utx.NumSigs = utx.NumInputs;
                utx.NumTInputs = utx.NumInputs;
                utx.NumTOutputs = 0;
                utx.NumPInputs = 0;
                utx.NumPOutputs = 0;

                /* Construct inputs */
                utx.TInputs = new Coin.Transparent.TXOutput[utx.NumInputs];
                utx.inputAmounts = new ulong[utx.NumInputs];
                for (int i = 0; i < utx.NumInputs; i++)
                {
                    utx.TInputs[i] = new Coin.Transparent.TXOutput(inputs[i].TransactionSrc, new TAddress(Address), inputs[i].Amount);
                    utx.inputAmounts[i] = inputs[i].Amount;
                }
            }

            /* construct outputs */
            List<Coin.Transparent.TXOutput> tOutputs = new List<Coin.Transparent.TXOutput>();
            List<TXOutput> pOutputs = new List<TXOutput>();
            List<Key> gammas = new List<Key>();
            List<ulong> amounts = new List<ulong>();

            Key r = new Key(new byte[32]);
            Key R = new Key(new byte[32]);
            KeyOps.GenerateKeypair(ref r, ref R);
            utx.TransactionKey = R;

            Key sum = new Key(new byte[32]);
            Key tmp = new Key(new byte[32]);

            for (int i = 0; i < to.Length; i++)
            {
                if (to[i].Type() == (byte)AddressType.TRANSPARENT)
                {
                    tOutputs.Add(new Coin.Transparent.TXOutput(default, new TAddress(to[i].Bytes()), amount[i]));
                    utx.NumTOutputs++;
                }
                else
                {
                    TXOutput pOutput = new TXOutput();
                    StealthAddress addr = new StealthAddress(to[i].Bytes());
                    pOutput.UXKey = KeyOps.DKSAP(ref r, addr.view, addr.spend, pOutputs.Count);
                    pOutput.Commitment = new Key(new byte[32]);
                    Key mask = KeyOps.GenCommitmentMask(ref r, ref addr.view, pOutputs.Count);
                    KeyOps.GenCommitment(ref pOutput.Commitment, ref mask, amount[i]);
                    pOutput.Amount = KeyOps.GenAmountMask(ref r, ref addr.view, pOutputs.Count, amount[i]);
                    gammas.Add(mask);
                    amounts.Add(amount[i]);
                    pOutputs.Add(pOutput);
                    utx.NumPOutputs++;

                    KeyOps.ScalarAdd(ref tmp, ref sum, ref mask);
                    Array.Copy(tmp.bytes, sum.bytes, 32);
                }
            }

            if (Type == (byte)WalletType.PRIVATE)
            {
                if (neededAmount != totalAmount)
                {
                    TXOutput pOutput = new TXOutput();
                    pOutput.UXKey = KeyOps.DKSAP(ref r, PubViewKey, PubSpendKey, pOutputs.Count);
                    pOutput.Commitment = new Key(new byte[32]);
                    Key mask = KeyOps.GenCommitmentMask(ref r, ref PubViewKey, pOutputs.Count);
                    KeyOps.GenCommitment(ref pOutput.Commitment, ref mask, neededAmount - totalAmount);
                    pOutput.Amount = KeyOps.GenAmountMask(ref r, ref PubViewKey, pOutputs.Count, neededAmount - totalAmount);
                    gammas.Add(mask);
                    amounts.Add(neededAmount - totalAmount);
                    pOutputs.Add(pOutput);
                    utx.NumPOutputs++;

                    KeyOps.ScalarAdd(ref tmp, ref sum, ref mask);
                    Array.Copy(tmp.bytes, sum.bytes, 32);
                }
            }
            else if (Type == (byte)WalletType.TRANSPARENT)
            {
                if (neededAmount != totalAmount)
                {
                    tOutputs.Add(new Coin.Transparent.TXOutput(default, new TAddress(Address), neededAmount - totalAmount));
                    utx.NumTOutputs++;
                }
            }
            
            /* create range proof */
            Cipher.BulletproofPlus bp = Cipher.BulletproofPlus.Prove(amounts.ToArray(), gammas.ToArray());
            utx.RangeProof = new Coin.BulletproofPlus(bp);

            utx.TOutputs = tOutputs.ToArray();
            utx.POutputs = pOutputs.ToArray();
            utx.sumGammas = sum;

            return utx;
        }

        public Coin.Transparent.Transaction CreateTransaction(TAddress[] to, ulong[] amount)
        {
            if (Type != (byte)WalletType.TRANSPARENT) throw new Exception("Discreet.Wallets.WalletAddress.CreateTransaction: private wallet cannot create fully transparent transaction!");

            if (Encrypted) throw new Exception("Discreet.Wallets.WalletAddress.CreateTransaction: Wallet is still encrypted!");

            ulong totalAmount = 0;
            for (int i = 0; i < amount.Length; i++) totalAmount += amount[i];
            if (totalAmount > Balance)
            {
                throw new Exception($"Discreet.Wallets.WalletAddress.CreateTransaction: sending amount is greater than wallet balance ({totalAmount} > {Balance})");
            }

            Coin.Transparent.Transaction tx = new();

            List<UTXO> inputs = new List<UTXO>();
            ulong neededAmount = 0;
            int j = 0;
            while (neededAmount < totalAmount)
            {
                neededAmount += UTXOs[j].Amount;
                inputs.Add(UTXOs[j]);
                j++;
            }

            tx.Version = (byte)Config.TransactionVersions.TRANSPARENT;
            tx.NumInputs = (byte)j;
            tx.NumOutputs = (byte)((neededAmount == totalAmount) ? to.Length : to.Length + 1);
            tx.NumSigs = tx.NumInputs;

            /* fees are currently zero */
            tx.Fee = 0;

            /* create inputs */
            tx.Inputs = new Coin.Transparent.TXOutput[tx.NumInputs];
            for (int i = 0; i < tx.NumInputs; i++)
            {
                tx.Inputs[i] = new Coin.Transparent.TXOutput(inputs[i].TransactionSrc, new TAddress(Address), inputs[i].Amount);
            }

            /* create outputs */
            tx.Outputs = new Coin.Transparent.TXOutput[tx.NumOutputs];
            for (int i = 0; i < to.Length; i++)
            {
                tx.Outputs[i] = new Coin.Transparent.TXOutput(default, to[i], amount[i]);
            }
            if (neededAmount != totalAmount)
            {
                tx.Outputs[tx.NumOutputs - 1] = new Coin.Transparent.TXOutput(default, new TAddress(Address), neededAmount - totalAmount);
            }

            /* create signatures */
            tx.Signatures = new Signature[tx.NumInputs];
            tx.InnerHash = tx.SigningHash();
            for (int i = 0; i < tx.NumInputs; i++)
            {
                tx.Signatures[i] = new Signature(SecKey, PubKey, tx.InnerHash);
            }

            return tx;
        }

        public Transaction CreateTransaction(StealthAddress[] to, ulong[] amount)
        {
            return CreateTransaction(to, amount, (byte)Config.TransactionVersions.BP_PLUS);
        }

        public Transaction CreateTransaction(StealthAddress[] to, ulong[] amount, byte version)
        {
            if (version != 1 && version != 2)
            {
                throw new Exception($"Discreet.Wallets.WalletAddress.CreateTransaction: version cannot be {version}; currently supporting 1 and 2");
            }

            DB.DB db = DB.DB.GetDB();

            /* mahjick happens now. */
            if (Encrypted) throw new Exception("Discreet.Wallets.WalletAddress.CreateTransaction: Wallet is still encrypted!");
            int i = 0;

            ulong totalAmount = 0;
            for (i = 0; i < amount.Length; i++) totalAmount += amount[i];
            if (totalAmount > Balance)
            {
                throw new Exception($"Discreet.Wallets.WalletAddress.CreateTransaction: sending amount is greater than wallet balance ({totalAmount} > {Balance})");
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
            /* we need to check if the change output is zero */
            tx.NumOutputs = (neededAmount == totalAmount) ? (byte)to.Length : (byte)(to.Length + 1);
            tx.NumSigs = tx.NumInputs;

            /* for testnet, all tx fees are zero */
            tx.Fee = 0;

            /* set TransactionKey */
            tx.TransactionKey = R;

            /* assemble outputs */
            tx.Outputs = new TXOutput[tx.NumOutputs];

            Key[] gammas = new Key[tx.NumOutputs];

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
            if (neededAmount != totalAmount)
            {
                tx.Outputs[i] = new TXOutput();
                tx.Outputs[i].UXKey = KeyOps.DKSAP(ref r, PubViewKey, PubSpendKey, i);
                tx.Outputs[i].Commitment = new Key(new byte[32]);
                Key rmask = KeyOps.GenCommitmentMask(ref r, ref PubViewKey, i);
                KeyOps.GenCommitment(ref tx.Outputs[i].Commitment, ref rmask, neededAmount - totalAmount);
                tx.Outputs[i].Amount = KeyOps.GenAmountMask(ref r, ref PubViewKey, i, neededAmount - totalAmount);
                gammas[i] = rmask;
            }

            ulong[] amounts = new ulong[tx.NumOutputs];

            for (i = 0; i < to.Length; i++)
            {
                amounts[i] = amount[i];
            }

            if (neededAmount != totalAmount)
            {
                amounts[i] = neededAmount - totalAmount;
            }

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