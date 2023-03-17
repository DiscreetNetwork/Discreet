using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.Wallets.Models;

namespace Discreet.Wallets.Services
{
    public class CreateTransparentTx : CreateTx
    {
        public override (IEnumerable<UTXO>, FullTransaction) CreateTransaction(Account account, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
            var utxos = GetUTXOsForTransaction(account, amounts).ToArray();
            var inputs = BuildTransparentInputs(utxos).ToArray();
            var _outputs = BuildTransparentOutputs(addresses, amounts);
            (_, var change) = BuildChangeOutput(account, utxos, amounts, null, 0, out _);
            if (change != null) _outputs = _outputs.Append(change);
            var outputs = _outputs.ToArray();

            var tx = new Discreet.Coin.Transparent.Transaction
            {
                Version = (byte)Config.TransactionVersions.TRANSPARENT,
                NumInputs = (byte)inputs.Length,
                NumOutputs = (byte)outputs.Length,
                NumSigs = (byte)inputs.Length,
                Fee = 0,
                Inputs = inputs,
                Outputs = outputs
            };

            tx.InnerHash = tx.SigningHash();
            tx.Signatures = BuildTransparentSignatures(utxos, inputs, tx.InnerHash).ToArray();

            return (utxos, tx.ToFull());
        }
    }
}
