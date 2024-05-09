using Discreet.Cipher;
using Discreet.Coin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin.Script
{
    public class TXInInfo
    {
        public TTXInput Reference { get; set; }
        public ScriptTXOutput Resolved { get; set; }
    }

    public class ScriptContext
    {
        public ulong Fee { get; set; }
        public SHA256 SigningHash { get; set; }
        public (long, long) ValidityInterval { get; set; }

        public TXInInfo[] Inputs { get; set; }
        public TXInInfo[] ReferenceInputs { get; set; }
        public ScriptTXOutput[] Outputs { get; set; }

        public TXInput[] PrivateInputs { get; set; }
        public TXOutput[] PrivateOutputs { get; set; }

        public Key? TransactionKey { get; set; }

        public ScriptContext() { }

        public ScriptContext(FullTransaction tx, ScriptTXOutput[] tinVals, ScriptTXOutput[] rinVals)
        {
            Fee = tx.Fee;
            SigningHash = tx.SigningHash;
            ValidityInterval = tx.ValidityInterval;
            Inputs = tx.TInputs.Zip(tinVals).Select(x => new TXInInfo { Reference = x.First, Resolved = x.Second }).ToArray();
            ReferenceInputs = (tx.RefInputs != null) ? tx.RefInputs.Zip(rinVals).Select(x => new TXInInfo { Reference = x.First, Resolved = x.Second }).ToArray() : Array.Empty<TXInInfo>();
            Outputs = tx.TOutputs;
            PrivateInputs = tx.PInputs ?? Array.Empty<TXInput>();
            PrivateOutputs = tx.POutputs ?? Array.Empty<TXOutput>();

            TransactionKey = (tx.TransactionKey == default) ? null : tx.TransactionKey;
        }
    }
}
