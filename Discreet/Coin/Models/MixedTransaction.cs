using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;

namespace Discreet.Coin.Models
{
    /**
     * A mixed transaction contains both transparent and private inputs and outputs. 
     * The transaction cannot be partially signed like normal transparent transactions.
     * Additionally, there is no Extra field, and the transaction must use one uniform TXKey.
     */
    public class MixedTransaction : IHashable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public byte NumTInputs { get; set; }
        public byte NumPInputs { get; set; }
        public byte NumTOutputs { get; set; }
        public byte NumPOutputs { get; set; }

        public SHA256 SigningHash { get; set; }

        public ulong Fee { get; set; }

        /* Transparent part */
        public TTXInput[] TInputs { get; set; }
        public TTXOutput[] TOutputs { get; set; }
        public Signature[] TSignatures { get; set; }

        /* Private part */
        public Key TransactionKey { get; set; }
        public TXInput[] PInputs { get; set; }
        public TXOutput[] POutputs { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }
        public Triptych[] PSignatures { get; set; }
        public Key[] PseudoOutputs { get; set; }

        private SHA256 _txid;
        public SHA256 TxID { get { if (_txid == default || _txid.Bytes == null) _txid = this.Hash(); return _txid; } }

        public MixedTransaction() { }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }

        public MixedTransaction(byte[] bytes)
        {
            var reader = new MemoryReader(bytes.AsMemory());
            Deserialize(ref reader);
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);
            writer.Write(NumTInputs);
            writer.Write(NumPInputs);
            writer.Write(NumTOutputs);
            writer.Write(NumPOutputs);

            writer.Write(Fee);
            writer.WriteSHA256(SigningHash);

            writer.WriteSerializableArray(TInputs, false);
            writer.WriteSerializableArray(TOutputs, false, (x) => x.TXMarshal);
            writer.WriteSerializableArray(TSignatures, false);

            if (NumPOutputs > 0) writer.WriteKey(TransactionKey);

            writer.WriteSerializableArray(PInputs, false);
            writer.WriteSerializableArray(POutputs, false, (x) => x.TXMarshal);

            if (NumPOutputs > 0) RangeProofPlus.Serialize(writer);
            writer.WriteSerializableArray(PSignatures, false);
            if (NumPInputs > 0) writer.WriteKeyArray(PseudoOutputs);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt8();
            NumInputs = reader.ReadUInt8();
            NumOutputs = reader.ReadUInt8();
            NumSigs = reader.ReadUInt8();
            NumTInputs = reader.ReadUInt8();
            NumPInputs = reader.ReadUInt8();
            NumTOutputs = reader.ReadUInt8();
            NumPOutputs = reader.ReadUInt8();

            Fee = reader.ReadUInt64();
            SigningHash = reader.ReadSHA256();

            TInputs = reader.ReadSerializableArray<TTXInput>(NumTInputs);
            TOutputs = reader.ReadSerializableArray<TTXOutput>(NumTOutputs, (x) => x.TXUnmarshal);
            TSignatures = reader.ReadSerializableArray<Signature>(NumTInputs);

            if (NumPOutputs > 0) TransactionKey = reader.ReadKey();

            PInputs = reader.ReadSerializableArray<TXInput>(NumPInputs);
            POutputs = reader.ReadSerializableArray<TXOutput>(NumPOutputs, (x) => x.TXUnmarshal);

            if (NumPOutputs > 0) RangeProofPlus = reader.ReadSerializable<BulletproofPlus>();
            PSignatures = reader.ReadSerializableArray<Triptych>(NumPInputs);
            if (NumPInputs > 0) PseudoOutputs = reader.ReadKeyArray(NumPInputs);
        }

        public SHA256 TXSigningHash()
        {
            byte[] bytes = new byte[16 + TTXInput.GetSize() * NumTInputs + 33 * NumTOutputs + (NumPOutputs > 0 ? 32 : 0) + NumPInputs * TXInput.GetSize() + NumPOutputs * 40];
            var writer = new BEBinaryWriter(new MemoryStream(bytes));

            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);
            writer.Write(NumTInputs);
            writer.Write(NumPInputs);
            writer.Write(NumTOutputs);
            writer.Write(NumPOutputs);

            writer.Write(Fee);

            writer.WriteSerializableArray(TInputs, false);
            writer.WriteSerializableArray(TOutputs, false, (x) => x.TXMarshal);

            if (NumPOutputs > 0) writer.WriteKey(TransactionKey);

            writer.WriteSerializableArray(PInputs, false);
            writer.WriteSerializableArray(POutputs, false, (x) => (writer) => { writer.WriteKey(x.UXKey); writer.Write(x.Amount); });

            return SHA256.HashData(bytes);
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.TransactionConverter());

            return JsonSerializer.Serialize(ToFull(), typeof(FullTransaction), options);
        }

        public uint GetSize()
        {
            return (uint)(48 + TTXInput.GetSize() * (TInputs == null ? 0 : TInputs.Length)
                             + 33 * (TOutputs == null ? 0 : TOutputs.Length)
                             + 96 * (TSignatures == null ? 0 : TSignatures.Length)
                             + (NumPOutputs > 0 ? 32 : 0)
                             + (PInputs == null ? 0 : PInputs.Length) * TXInput.GetSize()
                             + (POutputs == null ? 0 : POutputs.Length) * 72
                             + (RangeProofPlus == null && NumPOutputs == 0 ? 0 : RangeProofPlus.Size)
                             + Triptych.GetSize() * (PSignatures == null ? 0 : PSignatures.Length)
                             + (NumPInputs == 0 ? 0 : 32 * PseudoOutputs.Length));
        }

        public int Size => (int)GetSize();

        public Key[] GetCommitments()
        {
            Key[] Comms = new Key[NumPOutputs];

            for (int i = 0; i < NumPOutputs; i++)
            {
                Comms[i] = POutputs[i].Commitment;
            }

            return Comms;
        }

        public Transaction ToPrivate()
        {
            Transaction tx = new Transaction();
            tx.Version = Version;
            tx.NumInputs = NumPInputs;
            tx.NumOutputs = NumPOutputs;
            tx.NumSigs = NumSigs;

            tx.Inputs = PInputs;
            tx.Outputs = POutputs;
            tx.Signatures = PSignatures;

            tx.RangeProofPlus = RangeProofPlus;

            tx.PseudoOutputs = PseudoOutputs;

            tx.Fee = Fee;

            tx.TransactionKey = TransactionKey;

            return tx;
        }

        public TTransaction ToTransparent()
        {
            TTransaction tx = new TTransaction();
            tx.Version = Version;
            tx.NumInputs = NumPInputs;
            tx.NumOutputs = NumPOutputs;
            tx.NumSigs = NumSigs;

            tx.Inputs = TInputs;
            tx.Outputs = TOutputs;
            tx.Signatures = TSignatures;

            tx.Fee = Fee;

            tx.InnerHash = SigningHash;

            return tx;
        }

        public VerifyException Verify()
        {
            return Verify(inBlock: false);
        }

        public VerifyException Verify(bool inBlock = false)
        {
            bool tInNull = TInputs == null;
            bool tOutNull = TOutputs == null;
            bool pInNull = PInputs == null;
            bool pOutNull = POutputs == null;
            bool tSigNull = TSignatures == null;
            bool pSigNull = PSignatures == null;

            int lenTInputs = tInNull ? 0 : TInputs.Length;
            int lenTOutputs = tOutNull ? 0 : TOutputs.Length;
            int lenPInputs = pInNull ? 0 : PInputs.Length;
            int lenPOutputs = pOutNull ? 0 : POutputs.Length;
            int lenTSigs = tSigNull ? 0 : TSignatures.Length;
            int lenPSigs = pSigNull ? 0 : PSignatures.Length;

            /* we can convert MixedTransactions to either Transparent.Transaction or Transaction */
            if (Version == 0 || Version == 1 || Version == 2)
            {
                return ToPrivate().Verify(inBlock);
            }

            if (Version == 3)
            {
                return ToTransparent().Verify(inBlock);
            }

            if (Version != (byte)Config.TransactionVersions.MIXED)
            {
                return new VerifyException("MixedTransaction", $"Invalid transaction version: expected {(byte)Config.TransactionVersions.MIXED}, but got {Version}");
            }

            if (lenTInputs + lenPInputs == 0)
            {
                return new VerifyException("MixedTransaction", "Zero inputs");
            }

            if (lenTOutputs + lenPOutputs == 0)
            {
                return new VerifyException("MixedTransaction", "Zero outputs");
            }

            if (NumInputs != lenTInputs + lenPInputs)
            {
                return new VerifyException("MixedTransaction", $"Input number mismatch: expected {NumInputs}, but got {lenTInputs + lenPOutputs}");
            }

            if (NumOutputs != lenTOutputs + lenPOutputs)
            {
                return new VerifyException("MixedTransaction", $"Output number mismatch: expected {NumOutputs}, but got {lenTOutputs + lenPOutputs}");
            }

            if (NumInputs > Config.TRANSPARENT_MAX_NUM_INPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of Inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_INPUTS} ({NumInputs} Inputs present)");
            }

            if (NumOutputs > Config.TRANSPARENT_MAX_NUM_OUTPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of Inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_OUTPUTS} ({NumOutputs} Inputs present)");
            }

            if (NumSigs > NumInputs)
            {
                return new VerifyException("MixedTransaction", $"Invalid number of signatures: {NumSigs} > {NumInputs}, exceeding the number of inputs");
            }

            if (lenTInputs != NumTInputs)
            {
                return new VerifyException("MixedTransaction", $"Transparent input number mismatch: expected {NumTInputs}, but got {lenTInputs}");
            }

            if (lenTOutputs != NumTOutputs)
            {
                return new VerifyException("MixedTransaction", $"Transparent output number mismatch: expected {NumTOutputs}, but got {lenTOutputs}");
            }

            if (lenPInputs != NumPInputs)
            {
                return new VerifyException("MixedTransaction", $"Private input number mismatch: expected {NumPInputs}, but got {lenPInputs}");
            }

            if (lenPOutputs != NumPOutputs)
            {
                return new VerifyException("MixedTransaction", $"Private output number mismatch: expected {NumPOutputs}, but got {lenPOutputs}");
            }

            if (NumTInputs > Config.TRANSPARENT_MAX_NUM_INPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of transparent inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_INPUTS} ({NumTInputs} Inputs present)");
            }

            if (NumTOutputs > Config.TRANSPARENT_MAX_NUM_OUTPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of transparent outputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_OUTPUTS} ({NumTOutputs} Inputs present)");
            }

            if (lenTSigs + lenPSigs != NumSigs)
            {
                return new VerifyException("MixedTransaction", $"Signature number mismatch: expected {NumSigs}, but got {lenTSigs + lenPSigs}");
            }

            if (lenTSigs != NumTInputs)
            {
                return new VerifyException("MixedTransaction", $"Transactions must have the same number of transparent signatures as transparent inputs ({NumTInputs} inputs, but {lenTSigs} sigs)");
            }

            if (lenPSigs != NumPInputs)
            {
                return new VerifyException("MixedTransaction", $"Transactions must have the same number of private signatures as private inputs ({NumPInputs} inputs, but {lenPSigs} sigs)");
            }

            if (lenPOutputs > 0 && RangeProofPlus == null)
            {
                return new VerifyException("MixedTransaction", $"Transaction contains at least one private output, but has no range proof!");
            }

            if (lenPOutputs > 0 && (TransactionKey == default || TransactionKey.bytes == null))
            {
                return new VerifyException("MixedTransaction", $"Transaction contains at least one private output, but has no transaction key!");
            }

            HashSet<TTXInput> _in = new HashSet<TTXInput>(new Comparers.TTXInputEqualityComparer());
            TTXOutput[] tinputValues = new TTXOutput[TInputs.Length];

            var dataView = DB.DataView.GetView();
            var pool = Daemon.TXPool.GetTXPool();

            for (int i = 0; i < NumTInputs; i++)
            {
                _in.Add(TInputs[i]);

                try
                {
                    tinputValues[i] = dataView.GetPubOutput(TInputs[i]);
                }
                catch (Exception e)
                {
                    Daemon.Logger.Error("MixedTransaction.Verify: " + e.Message, e);
                    return new VerifyException("MixedTransaction", $"Transparent input at index {i} for transaction not present in UTXO set!");
                }
            }

            if (_in.Count != NumTInputs)
            {
                return new VerifyException("MixedTransaction", $"Duplicate transparent inputs detected!");
            }

            for (int i = 0; i < NumTInputs; i++)
            {
                var aexc = tinputValues[i].Address.Verify();
                if (aexc != null) return aexc;

                if (!tinputValues[i].Address.CheckAddressBytes(TSignatures[i].y))
                {
                    return new VerifyException("MixedTransaction", $"Transparent input at index {i}'s address ({tinputValues[i].Address}) does not match public key in signature ({TSignatures[i].y.ToHexShort()})");
                }

                /* check if present in database */


                /* check if tx is in pool */
                if (!inBlock)
                {
                    if (pool.ContainsSpent(TInputs[i]))
                    {
                        return new VerifyException("MixedTransaction", $"Transparent input at index {i} was spent in a previous transaction currently in the mempool");
                    }
                }
            }

            foreach (TTXOutput output in TOutputs)
            {
                if (output.Amount == 0)
                {
                    return new VerifyException("MixedTransaction", "zero coins in output!");
                }
            }

            ulong _amount = 0;

            foreach (TTXOutput output in TOutputs)
            {
                try
                {
                    _amount = checked(_amount + output.Amount);
                }
                catch (OverflowException)
                {
                    return new VerifyException("MixedTransaction", $"transaction transparent outputs resulted in overflow!");
                }
            }

            SHA256 txhash = this.Hash();

            HashSet<SHA256> _out = new HashSet<SHA256>();

            for (int i = 0; i < NumTOutputs; i++)
            {
                _out.Add(new TTXOutput(txhash, TOutputs[i].Address, TOutputs[i].Amount).Hash());
            }

            if (_out.Count != NumTOutputs)
            {
                return new VerifyException("MixedTransaction", $"Duplicate transparent outputs detected!");
            }

            if (SigningHash != TXSigningHash())
            {
                return new VerifyException("MixedTransaction", $"Signing Hash {SigningHash.ToHexShort()} does not match computed signing hash {TXSigningHash().ToHexShort()}");
            }

            for (int i = 0; i < lenTSigs; i++)
            {
                if (TSignatures[i].IsNull())
                {
                    return new VerifyException("MixedTransaction", $"Unsigned transparent input present in transaction!");
                }

                byte[] data = new byte[64];
                Array.Copy(SigningHash.Bytes, data, 32);
                Array.Copy(TInputs[i].Hash(tinputValues[i]).Bytes, 0, data, 32, 32);

                SHA256 checkSig = SHA256.HashData(data);

                if (!TSignatures[i].Verify(checkSig))
                {
                    return new VerifyException("MixedTransaction", $"Signature failed verification!");
                }
            }

            if (NumPOutputs > 16)
            {
                return new VerifyException("MixedTransaction", $"Transactions cannot have more than 16 private outputs");
            }

            for (int i = 0; i < NumPOutputs; i++)
            {
                if (POutputs[i].Amount == 0)
                {
                    return new VerifyException("MixedTransaction", $"Amount field in private output at index {i} is not set!");
                }

                if (POutputs[i].Commitment.Equals(Key.Z))
                {
                    return new VerifyException("MixedTransaction", $"Commitment field in private output at index {i} is not set!");
                }
            }

            if (PseudoOutputs == null || PseudoOutputs.Length == 0)
            {
                return new VerifyException("MixedTransaction", $"Transaction is missing all PseudoOutputs");
            }

            if (NumInputs != PseudoOutputs.Length)
            {
                return new VerifyException("MixedTransaction", $"PseudoOutput length mismatch: expected {NumInputs}, but got {PseudoOutputs.Length}");
            }

            Key sumPseudos = new(new byte[32]);
            Key tmp = new(new byte[32]);

            for (int i = 0; i < PseudoOutputs.Length; i++)
            {
                KeyOps.AddKeys(ref tmp, ref sumPseudos, ref PseudoOutputs[i]);
                Array.Copy(tmp.bytes, sumPseudos.bytes, 32);
            }

            Key sumComms = new(new byte[32]);

            for (int i = 0; i < lenPOutputs; i++)
            {
                var comm = POutputs[i].Commitment;
                KeyOps.AddKeys(ref tmp, ref sumComms, ref comm);
                Array.Copy(tmp.bytes, sumComms.bytes, 32);
            }

            /* since we may have transparent inputs as well, we need to commit sumTOutputs; this is just a blinding factor of 0 */

            /* all mixed transactions must commit inputs to pseudo outputs, regardless of transparency or privacy. */

            ulong sumTOutputs = 0;
            for (int i = 0; i < lenTOutputs; i++)
            {
                sumTOutputs += TOutputs[i].Amount;
            }

            Key commTOutputs = new(new byte[32]);
            Key _Z = Key.Copy(Key.Z);
            KeyOps.GenCommitment(ref commTOutputs, ref _Z, sumTOutputs);
            KeyOps.AddKeys(ref tmp, ref sumComms, ref commTOutputs);
            Array.Copy(tmp.bytes, sumComms.bytes, 32);

            Key commFee = new(new byte[32]);
            KeyOps.GenCommitment(ref commFee, ref _Z, Fee);
            KeyOps.AddKeys(ref tmp, ref sumComms, ref commFee);
            Array.Copy(tmp.bytes, sumComms.bytes, 32);

            //Cipher.Key dif = new(new byte[32]);
            //Cipher.KeyOps.SubKeys(ref dif, ref sumPseudos, ref sumComms);

            if (!sumPseudos.Equals(sumComms))
            {
                return new VerifyException("MixedTransaction", $"Transaction does not balance! (sumC'a != sumCb)");
            }

            /* validate range sig */
            VerifyException bpexc = null;

            if (RangeProofPlus != null)
            {
                bpexc = RangeProofPlus.Verify(ToPrivate());
            }

            if (bpexc != null)
            {
                return bpexc;
            }

            /* validate inputs */
            var uncheckedTags = new List<Key>(PInputs.Select(x => x.KeyImage));
            for (int i = 0; i < lenPInputs; i++)
            {
                if (PInputs[i].Offsets.Length != 64)
                {
                    return new VerifyException("MixedTransaction", $"Private input at index {i} has an anonymity set of size {PInputs[i].Offsets.Length}; expected 64");
                }

                TXOutput[] mixins;
                try
                {
                    mixins = dataView.GetMixins(PInputs[i].Offsets);
                }
                catch (Exception e)
                {
                    return new VerifyException("MixedTransaction", $"Got error when getting mixin data at input at index {i}: " + e.Message);
                }

                Key[] M = new Key[64];
                Key[] P = new Key[64];

                for (int k = 0; k < 64; k++)
                {
                    M[k] = mixins[k].UXKey;
                    P[k] = mixins[k].Commitment;
                }

                var sigexc = PSignatures[i].Verify(M, P, PseudoOutputs[i], SigningHash.ToKey(), PInputs[i].KeyImage);

                if (sigexc != null)
                {
                    return sigexc;
                }

                if (inBlock)
                {
                    if (!dataView.CheckSpentKey(PInputs[i].KeyImage))
                    {
                        return new VerifyException("MixedTransaction", $"Key image for input at index {i} ({PInputs[i].KeyImage.ToHexShort()}) already spent! (double spend)");
                    }
                }
                else
                {
                    if (!dataView.CheckSpentKey(PInputs[i].KeyImage) || Daemon.TXPool.GetTXPool().ContainsSpentKey(PInputs[i].KeyImage))
                    {
                        return new VerifyException("MixedTransaction", $"Key image for input at index {i} ({PInputs[i].KeyImage.ToHexShort()}) already spent! (double spend)");
                    }
                }

                uncheckedTags.Remove(PInputs[i].KeyImage);
                if (uncheckedTags.Any(x => x == PInputs[i].KeyImage))
                {
                    return new VerifyException("MixedTransaction", $"Key image for {i} ({PInputs[i].KeyImage.ToHexShort()}) already spent in this transaction! (double spend)");
                }
            }

            /* this *should* be everything needed to validate a mixed transaction */
            return null;
        }
    }
}
