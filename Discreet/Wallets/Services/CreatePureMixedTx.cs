using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public class CreatePureMixedTx : CreateTx
    {
        protected List<UTXO> inputs;
        
        public CreatePureMixedTx(IEnumerable<UTXO> inputs) : base()
        {
            this.inputs = inputs.ToList();
        }

        public override (IEnumerable<UTXO>, FullTransaction) CreateTransaction(Account account, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
            if (inputs == null) throw new Exception(nameof(inputs) + " has not been set");

            if (inputs.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y) < amounts.Aggregate(0UL, (x, y) => x + y))
            {
                throw new Exception("input amounts less than output amounts");
            }

            // separate out necessary info
            var paddrs = addresses.Zip(amounts)
                .Where(x => x.First.Type() == (byte)AddressType.STEALTH)
                .Select(x => x.First);
            var pamounts = addresses.Zip(amounts)
                .Where(x => x.First.Type() == (byte)AddressType.STEALTH)
                .Select(x => x.Second);

            var taddrs = addresses.Zip(amounts)
                .Where(x => x.First.Type() == (byte)AddressType.TRANSPARENT)
                .Select(x => x.First);
            var tamounts = addresses.Zip(amounts)
                .Where(x => x.First.Type() == (byte)AddressType.TRANSPARENT)
                .Select(x => x.Second);

            // check for (1) transparent inputs and (2) private outputs, including potential private change output
            bool tToP = inputs.Where(x => x.Type == 1).Any() &&
                (paddrs.Any() ||
                (account.Type == 0
                && inputs.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y) != amounts.Aggregate(0UL, (x, y) => x + y)));

            var pinputs = BuildPrivateInputs(inputs).ToArray();
            var tinputs = BuildTransparentInputs(inputs).ToArray();
            (var txPrivateKey, var txPublicKey) = KeyOps.GenerateKeypair();
            (var secoutdata, var _poutputs) = BuildPrivateOutputs(tToP, paddrs, pamounts, txPrivateKey);
            var _toutputs = BuildTransparentOutputs(taddrs, tamounts);
            (var pchange, var tchange) = BuildChangeOutput(account, inputs, tamounts.Concat(pamounts), txPrivateKey, _poutputs.Count(), out var extraPair);
            if (pchange != null)
            {
                _poutputs = _poutputs.Append(pchange);
                secoutdata = secoutdata.Append(extraPair.Value);
            }
            else if (tchange != null)
            {
                _toutputs = _toutputs.Append(tchange);
            }

            var poutputs = _poutputs.ToArray();
            var toutputs = _toutputs.ToArray();
            Coin.BulletproofPlus bp = null;
            if (poutputs.Any()) bp = BuildRangeProof(secoutdata);

            var tx = new MixedTransaction
            {
                Version = (byte)Config.TransactionVersions.MIXED,
                NumInputs = (byte)(pinputs.Length + tinputs.Length),
                NumOutputs = (byte)(poutputs.Length + toutputs.Length),
                NumSigs = (byte)(pinputs.Length + tinputs.Length),
                NumPInputs = (byte)pinputs.Length,
                NumPOutputs = (byte)poutputs.Length,
                NumTInputs = (byte)tinputs.Length,
                NumTOutputs = (byte)toutputs.Length,
                Fee = 0,
                PInputs = pinputs.Select(x => x.Input).ToArray(),
                TInputs = tinputs,
                POutputs = poutputs,
                TOutputs = toutputs,
                RangeProofPlus = bp,
                TransactionKey = poutputs.Any() ? txPublicKey : default
            };

            tx.SigningHash = tx.TXSigningHash();
            (var pseudos, var blindingFactors) = BuildPseudoOutputs(inputs, secoutdata);
            tx.PseudoOutputs = pseudos.ToArray();
            tx.PSignatures = BuildPrivateSignatures(inputs, pinputs, pseudos, blindingFactors, tx.SigningHash).ToArray();
            tx.TSignatures = BuildTransparentSignatures(inputs, tinputs, tx.SigningHash).ToArray();

            return (inputs, tx.ToFull());
        }
    }
}
