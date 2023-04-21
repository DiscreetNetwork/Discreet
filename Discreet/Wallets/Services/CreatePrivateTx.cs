using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Coin.Models;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public class CreatePrivateTx : CreateTx
    {
        public override (IEnumerable<UTXO>, FullTransaction) CreateTransaction(Account account, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
            return CreateTransaction(false, account, addresses, amounts);
        }

        private (IEnumerable<UTXO>, FullTransaction) CreateTransaction(bool useLegacy, Account account, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
            if (useLegacy) throw new Exception("legacy bulletproofs are depracated");

            var utxos = GetUTXOsForTransaction(account, amounts).ToArray();
            var inputs = BuildPrivateInputs(utxos).ToArray();
            (var txPrivateKey, var txPublicKey) = KeyOps.GenerateKeypair();
            (var secoutdata, var _outputs) = BuildPrivateOutputs(account, addresses, amounts, txPrivateKey);
            (var change, _) = BuildChangeOutput(account, utxos, amounts, txPrivateKey, _outputs.Count(), out var extraPair);
            if (change != null)
            {
                _outputs = _outputs.Append(change);
                secoutdata = secoutdata.Append(extraPair.Value);
            }
            var outputs = _outputs.ToArray();

            var bp = BuildRangeProof(secoutdata);

            var tx = new Transaction
            {
                Version = (byte)Config.TransactionVersions.BP_PLUS,
                NumInputs = (byte)inputs.Length,
                NumOutputs = (byte)outputs.Length,
                NumSigs = (byte)inputs.Length,
                Fee = 0,
                Inputs = inputs.Select(x => x.Input).ToArray(),
                Outputs = outputs,
                RangeProofPlus = bp,
                TransactionKey = txPublicKey
            };

            var signingHash = tx.SigningHash();
            (var pseudos, var blindingFactors) = BuildPseudoOutputs(utxos, secoutdata);
            tx.PseudoOutputs = pseudos.ToArray();
            tx.Signatures = BuildPrivateSignatures(utxos, inputs, pseudos, blindingFactors, signingHash).ToArray();

            return (utxos, tx.ToFull());
        }
    }
}
