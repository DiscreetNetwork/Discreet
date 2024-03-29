﻿using Discreet.Cipher;
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
    public class CreateMixedTx : CreateTx
    {
        public override (IEnumerable<UTXO>, FullTransaction) CreateTransaction(Account account, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
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

            var utxos = GetUTXOsForTransaction(account, amounts);

            if (account.Type == 0) return (utxos, CreatePtoTTransaction(account, paddrs, pamounts, taddrs, tamounts, utxos));
            else if (account.Type == 1) return (utxos, CreateTtoPTransaction(account, paddrs, pamounts, taddrs, tamounts, utxos));
            else throw new Exception("unknown account type");
        }

        public FullTransaction CreatePtoTTransaction(Account account, IEnumerable<IAddress> paddrs, IEnumerable<ulong> pamounts, IEnumerable<IAddress> taddrs, IEnumerable<ulong> tamounts, IEnumerable<UTXO> utxos)
        {
            var pinputs = BuildPrivateInputs(utxos).ToArray();
            var toutputs = BuildTransparentOutputs(taddrs, tamounts).ToArray();
            (var txPrivateKey, var txPublicKey) = KeyOps.GenerateKeypair();
            (var secoutdata, var _poutputs) = BuildPrivateOutputs(account, paddrs, pamounts, txPrivateKey);
            (var change, _) = BuildChangeOutput(account, utxos, tamounts.Concat(pamounts), txPrivateKey, _poutputs.Count(), out var extraPair);
            if (change != null)
            {
                _poutputs = _poutputs.Append(change);
                secoutdata = secoutdata.Append(extraPair.Value);
            }
            var poutputs = _poutputs.ToArray();
            var bp = poutputs.Length > 0 ? BuildRangeProof(secoutdata) : null;

            var tx = new MixedTransaction
            {
                Version = (byte)Config.TransactionVersions.MIXED,
                NumInputs = (byte)pinputs.Length,
                NumOutputs = (byte)(poutputs.Length + toutputs.Length),
                NumSigs = (byte)pinputs.Length,
                NumPInputs = (byte)pinputs.Length,
                NumPOutputs = (byte)poutputs.Length,
                NumTInputs = 0,
                NumTOutputs = (byte)toutputs.Length,
                Fee = 0,
                PInputs = pinputs.Select(x => x.Input).ToArray(),
                POutputs = poutputs,
                TInputs = Array.Empty<TTXInput>(),
                TOutputs = toutputs,
                RangeProofPlus = bp,
                TSignatures = Array.Empty<Signature>()
            };

            if (tx.NumPOutputs > 0) tx.TransactionKey = txPublicKey;
            tx.SigningHash = tx.TXSigningHash();
            (var pseudos, var blindingFactors) = BuildPseudoOutputs(utxos, secoutdata);
            tx.PseudoOutputs = pseudos.ToArray();
            tx.PSignatures = BuildPrivateSignatures(utxos, pinputs, pseudos, blindingFactors, tx.SigningHash).ToArray();

            return tx.ToFull();
        }

        public FullTransaction CreateTtoPTransaction(Account account, IEnumerable<IAddress> paddrs, IEnumerable<ulong> pamounts, IEnumerable<IAddress> taddrs, IEnumerable<ulong> tamounts, IEnumerable<UTXO> utxos)
        {
            var tinputs = BuildTransparentInputs(utxos).ToArray();
            var _toutputs = BuildTransparentOutputs(taddrs, tamounts);
            (var txPrivateKey, var txPublicKey) = KeyOps.GenerateKeypair();
            (var secoutdata, var _poutputs) = BuildPrivateOutputs(account, paddrs, pamounts, txPrivateKey);
            (_, var change) = BuildChangeOutput(account, utxos, tamounts.Concat(pamounts), txPrivateKey, _poutputs.Count(), out _);
            if (change != null)
            {
                _toutputs = _toutputs.Append(change);
            }
            var poutputs = _poutputs.ToArray();
            var toutputs = _toutputs.ToArray();
            var bp = BuildRangeProof(secoutdata);

            var tx = new MixedTransaction
            {
                Version = (byte)Config.TransactionVersions.MIXED,
                NumInputs = (byte)tinputs.Length,
                NumOutputs = (byte)(poutputs.Length + toutputs.Length),
                NumSigs = (byte)tinputs.Length,
                NumPInputs = 0,
                NumPOutputs = (byte)poutputs.Length,
                NumTInputs = (byte)tinputs.Length,
                NumTOutputs = (byte)toutputs.Length,
                Fee = 0,
                PInputs = Array.Empty<TXInput>(),
                POutputs = poutputs,
                TInputs = tinputs,
                TOutputs = toutputs,
                RangeProofPlus = bp,
                TransactionKey = txPublicKey,
                PSignatures = Array.Empty<Coin.Models.Triptych>(),
                PseudoOutputs = Array.Empty<Key>()
            };

            tx.SigningHash = tx.TXSigningHash();
            tx.TSignatures = BuildTransparentSignatures(utxos, tinputs, tx.SigningHash).ToArray();

            return tx.ToFull();
        }
    }
}
