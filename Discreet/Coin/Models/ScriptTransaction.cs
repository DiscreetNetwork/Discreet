using Discreet.Cipher;
using Discreet.Coin.Script;
using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin.Models
{
    public class ScriptTransaction : IHashable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public byte NumRefInputs { get; set; }
        public byte NumScriptInputs { get; set; }

        public SHA256 InnerHash { get; set; }

        public ulong Fee { get; set; }

        public (long LowerBound, long UpperBound) ValidityInterval { get; set; }

        public TTXInput[] Inputs { get; set; }

        public TTXInput[] RefInputs { get; set; }

        public ScriptTXOutput[] Outputs { get; set; }

        internal ChainScript[] _scripts;
        internal Datum[] _datums;
        internal (byte, Datum)[] _redeemers; 

        public Dictionary<ScriptAddress, ChainScript> Scripts { get; set; }
        public Dictionary<SHA256, Datum> Datums { get; set; } // if a non-inlined datum hash is present in an input, datum must be supplied.
        public Dictionary<byte, Datum> Redeemers { get; set; } // byte corresponds to which input gets the redeemer

        public (byte, Signature)[] Signatures { get; set; }

        private SHA256 _txid;
        public SHA256 TxID { get { if (_txid == default || _txid.Bytes == null) _txid = this.Hash(); return _txid; } }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);
            writer.Write(NumRefInputs);
            writer.Write(NumScriptInputs);

            writer.WriteSHA256(InnerHash);
            writer.Write(Fee);
            writer.Write(ValidityInterval.LowerBound);
            writer.Write(ValidityInterval.UpperBound);
            writer.WriteSerializableArray(Inputs, false);
            writer.WriteSerializableArray(RefInputs, false);
            writer.WriteSerializableArray(Outputs, false, x => x.TXMarshal);
            writer.Write(Signatures?.Length ?? 0);
            if (Signatures != null)
            {
                foreach ((var index, var sig) in Signatures)
                {
                    writer.Write(index);
                    writer.Write(sig);
                }
            }

            // we do write length for scripts, datums and redeemers separately for safety reasons, for now
            writer.WriteSerializableArray(_scripts);
            writer.WriteSerializableArray(_datums);
            writer.Write(_redeemers?.Length ?? 0);

            if (_redeemers is not null)
            {
                foreach ((var b, var r) in _redeemers)
                {
                    writer.Write(b);
                    writer.Write(r);
                }
            }
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt8();
            NumInputs = reader.ReadUInt8();
            NumOutputs = reader.ReadUInt8();
            NumSigs = reader.ReadUInt8();
            NumRefInputs = reader.ReadUInt8();
            NumScriptInputs = reader.ReadUInt8();

            InnerHash = reader.ReadSHA256();
            Fee = reader.ReadUInt64();
            ValidityInterval = (reader.ReadInt64(), reader.ReadInt64());
            Inputs = reader.ReadSerializableArray<TTXInput>(NumInputs);
            RefInputs = reader.ReadSerializableArray<TTXInput>(NumRefInputs);
            Outputs = reader.ReadSerializableArray<ScriptTXOutput>(NumOutputs);
            var lentsigs = reader.ReadInt32();
            Signatures = reader.ReadSerializableArrayCustomDecoder(lentsigs, (ref MemoryReader _reader) => (_reader.ReadUInt8(), _reader.ReadSerializable<Signature>()));

            _scripts = reader.ReadSerializableArray<ChainScript>();
            _datums = reader.ReadSerializableArray<Datum>();
            _redeemers = reader.ReadSerializableArrayCustomDecoder(-1, (ref MemoryReader _reader) => (_reader.ReadUInt8(), _reader.ReadSerializable<Datum>()));

            Scripts = new Dictionary<ScriptAddress, ChainScript>(_scripts.Select(x => new KeyValuePair<ScriptAddress, ChainScript>(new ScriptAddress(x), x)));
            Datums = new Dictionary<SHA256, Datum>(_datums.Select(x => new KeyValuePair<SHA256, Datum>(x.Hash(), x)));
            Redeemers = new Dictionary<byte, Datum>(_redeemers.Select(p => new KeyValuePair<byte, Datum>(p.Item1, p.Item2)));
        }

        public int Size => 78 + 33 * (Inputs?.Length ?? 0 + RefInputs?.Length ?? 0) + Outputs?.Aggregate(0, (x, y) => x + y.Size) ?? 0
            + Scripts?.Values.Aggregate(0, (x, y) => x + y.Size) ?? 0 + Datums?.Values.Aggregate(0, (x, y) => x + y.Size) ?? 0
            + Redeemers?.Values.Aggregate(Redeemers?.Count ?? 0, (x, y) => x + y.Size) ?? 0 + 97 * (Signatures?.Length ?? 0);

        public SHA256 TXSigningHash()
        {
            byte[] bytes = new byte[42 + TTXInput.GetSize() * (Inputs?.Length ?? 0 + RefInputs?.Length ?? 0) + Outputs?.Aggregate(0, (x, y) => x + y.Size) ?? 0
            + Scripts?.Values.Aggregate(0, (x, y) => x + y.Size) ?? 0 + Datums?.Values.Aggregate(0, (x, y) => x + y.Size) ?? 0
            + Redeemers?.Values.Aggregate(Redeemers?.Count ?? 0, (x, y) => x + y.Size) ?? 0];

            var writer = new BEBinaryWriter(new MemoryStream(bytes));

            writer.Write(Version);
            writer.Write(NumInputs);
            writer.Write(NumOutputs);
            writer.Write(NumSigs);
            writer.Write(NumRefInputs);
            writer.Write(NumScriptInputs); // 6

            writer.Write(Fee); // 14
            writer.Write(ValidityInterval.LowerBound);
            writer.Write(ValidityInterval.UpperBound); // 30

            writer.WriteSerializableArray(Inputs, false);
            writer.WriteSerializableArray(RefInputs, false);
            writer.WriteSerializableArray(Outputs, false, (x) => x.TXMarshal);

            writer.WriteSerializableArray(_scripts);
            writer.WriteSerializableArray(_datums);
            writer.Write(_redeemers?.Length ?? 0);

            if (_redeemers is not null)
            {
                foreach ((var b, var r) in _redeemers)
                {
                    writer.Write(b);
                    writer.Write(r);
                }
            }

            return SHA256.HashData(bytes);
        }
    }
}
