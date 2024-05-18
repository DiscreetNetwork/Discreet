using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Wallets.Comparers;
using Discreet.DB;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.Wallets.Services
{
    public abstract class CreateTx
    {
        public abstract (IEnumerable<UTXO>, FullTransaction) CreateTransaction(Account account, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts);

        public static byte GetTransactionType(byte walletType, IEnumerable<IAddress> addresses)
        {
            var to = addresses.ToArray();
            Config.TransactionVersions ver = (walletType == 0) ? Config.TransactionVersions.BP_PLUS : Config.TransactionVersions.TRANSPARENT;
            byte ptype = to[0].Type();

            for (int i = 1; i < to.Length; i++)
            {
                if (ptype != to[i].Type())
                {
                    return (byte)Config.TransactionVersions.MIXED;
                }
            }

            if (walletType != ptype) return (byte)Config.TransactionVersions.MIXED;

            return (byte)ver;
        }

        protected StealthAddress[] GetStealthAddresses(IEnumerable<IAddress> to)
        {
            return to.Where(x => x.GetType() == typeof(StealthAddress)).Select(x => (StealthAddress)x).ToArray();

        }

        protected TAddress[] GetTAddresses(IAddress[] to)
        {
            return to.Where(x => x.GetType() == typeof(TAddress)).Select(x => (TAddress)x).ToArray();
        }

        protected IEnumerable<UTXO> GetUTXOsForTransaction(Account account, IEnumerable<ulong> amount)
        {
            if (account.Encrypted) throw new Exception("account is encrypted");

            var totalAmount = amount.Aggregate(0UL, (x, y) => x + y);
            if (totalAmount > account.Balance) throw new Exception("sending amount greater than balance");

            if (account.SortedUTXOs == null)
            {
                account.SortedUTXOs = new(account.UTXOs, new UTXOAmountComparer());
            }

            ulong neededAmount = 0;
            IEnumerable<UTXO> utxos;
            lock (account.SortedUTXOs)
            {
                lock (account.SelectedUTXOs)
                {
                    List<UTXO> utxoList = new();
                    foreach (var x in account.SortedUTXOs)
                    {
                        if (account.SelectedUTXOs.Contains(x))
                        {
                            continue;
                        }

                        if (neededAmount >= totalAmount) break;

                        neededAmount += x.DecodedAmount;
                        account.SelectedUTXOs.Add(x);
                        utxoList.Add(x);
                    }

                    utxos = utxoList;
                }
            }

            if (neededAmount < totalAmount) throw new Exception("sending amount may be greater than available balance");

            return utxos;
        }

        protected IEnumerable<TTXInput> BuildTransparentInputs(IEnumerable<UTXO> utxos)
        {
            return utxos.Where(x => x.Type == 1)
                .Select(x => new TTXInput
                {
                    TxSrc = x.TransactionSrc,
                    Offset = (byte)x.Index
                }).ToList();
        }

        protected IEnumerable<ScriptTXOutput> BuildTransparentOutputs(IEnumerable<IAddress> dests, IEnumerable<ulong> amounts)
        {
            var toutputs = dests.Zip(amounts)
                .Where(x => x.First.Type() == (byte)AddressType.TRANSPARENT)
                .Select(x => new ScriptTXOutput(default, (TAddress)x.First, x.Second)).ToList();

            return toutputs;
        }

        protected (TXOutput?, ScriptTXOutput?) BuildChangeOutput(Account account, IEnumerable<UTXO> utxos, IEnumerable<ulong> amounts, Key? txPrivateKey, int i, out (Key, ulong)? extraPair)
        {
            var totalIn = utxos.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y);
            var totalOut = amounts.Aggregate(0UL, (x, y) => { return x + y; });
            var _txPrivateKey = txPrivateKey.HasValue ? txPrivateKey.Value : default;
            if (totalIn != totalOut && account.Type == 1)
            {
                extraPair = null;
                return (null, new(default, new TAddress(account.Address), totalIn - totalOut));
            }
            else if (totalIn != totalOut && account.Type == 0)
            {
                var pk = account.PubViewKey.Value;
                var extraMask = KeyOps.GenCommitmentMask(ref _txPrivateKey, ref pk, i);
                extraPair = (extraMask, totalIn - totalOut);
                Key comm = new(new byte[32]);
                KeyOps.GenCommitment(ref comm, ref extraMask, totalIn - totalOut);
                return (new TXOutput
                {
                    UXKey = KeyOps.DKSAP(ref _txPrivateKey, account.PubViewKey.Value, account.PubSpendKey.Value, i),
                    Commitment = comm,
                    Amount = KeyOps.GenAmountMask(ref _txPrivateKey, ref pk, i, totalIn - totalOut)
                }, null);
            }
            else
            {
                extraPair = null;
                return (null, null);
            }
        }

        protected IEnumerable<PrivateTxInput> BuildPrivateInputs(IEnumerable<UTXO> utxos)
        {
            return utxos.Where(x => x.Type == 0)
                .Select(x =>
                {
                    (TXOutput[] mixins, int l) = ViewProvider.GetDefaultProvider().GetMixins(x.Index);
                    return new PrivateTxInput
                    {
                        M = mixins.Select(x => x.UXKey).ToArray(),
                        P = mixins.Select(x => x.Commitment).ToArray(),
                        l = l,
                        Input = new TXInput
                        {
                            Offsets = mixins.Select(x => x.Index).ToArray(),
                            KeyImage = x.LinkingTag.Value
                        }
                    };
                }).ToList();
        }

        protected (IEnumerable<(Key, ulong)>, IEnumerable<TXOutput>) BuildPrivateOutputs(bool tToP, IEnumerable<IAddress> dests, IEnumerable<ulong> amounts, Key txPrivateKey)
        {
            return BuildPrivateOutputs(new Account { Type = tToP ? (byte)1 : (byte)0 }, dests, amounts, txPrivateKey);
        }

        protected (IEnumerable<(Key, ulong)>, IEnumerable<TXOutput>) BuildPrivateOutputs(Account account, IEnumerable<IAddress> dests, IEnumerable<ulong> amounts, Key txPrivateKey)
        {
            //var totalIn = utxos.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y);
            //var totalOut = amounts.Aggregate(0UL, (x, y) => x + y);

            int i = 0;
            List<(Key, ulong)> maskAmountPairs = new();
            var poutputs = dests.Zip(amounts)
                .Where(x => x.First.Type() == (byte)AddressType.STEALTH)
                .Select(x => ((StealthAddress)x.First, x.Second))
                .Select(x =>
                {
                    var mask = KeyOps.GenCommitmentMask(ref txPrivateKey, ref x.Item1.view, i);
                    if (account.Type == 1) mask = Key.Identity();
                    maskAmountPairs.Add((mask, x.Second));
                    Key comm = new(new byte[32]);
                    KeyOps.GenCommitment(ref comm, ref mask, x.Second);
                    var _out = new TXOutput
                    {
                        UXKey = KeyOps.DKSAP(ref txPrivateKey, x.Item1.view, x.Item1.spend, i),
                        Commitment = comm,
                        Amount = (account.Type == 1) ? x.Second : KeyOps.GenAmountMask(ref txPrivateKey, ref x.Item1.view, i, x.Second),
                    };
                    i++;
                    return _out;
                }).ToList();

            //if (totalIn != totalOut && account.Type == 0)
            //{
            //    var pk = account.PubViewKey;
            //    var extraMask = KeyOps.GenCommitmentMask(ref txPrivateKey, ref pk, i);
            //    maskAmountPairs.Add((extraMask, totalIn - totalOut));
            //    poutputs = poutputs.Append(
            //        new TXOutput
            //        {
            //            UXKey = KeyOps.DKSAP(ref txPrivateKey, account.PubViewKey, account.PubSpendKey, i),
            //            Commitment = KeyOps.Commit(ref extraMask, totalIn - totalOut),
            //            Amount = KeyOps.GenAmountMask(ref txPrivateKey, ref pk, i, totalIn - totalOut)
            //        });
            //}

            return (maskAmountPairs, poutputs);
        }

        protected Coin.Models.BulletproofPlus BuildRangeProof(IEnumerable<(Key, ulong)> outs)
        {
            var bp = Cipher.BulletproofPlus.Prove(
                outs.Select(x => x.Item2).ToArray(), 
                outs.Select(x => x.Item1).ToArray());

            return new Coin.Models.BulletproofPlus(bp);
        }

        protected (IEnumerable<Key> pseudos, IEnumerable<Key> blindingFactors) BuildPseudoOutputs(IEnumerable<UTXO> utxos, IEnumerable<(Key, ulong)> outs)
        {
            var utxosList = utxos.Where(x => x.Type == 0).ToList();
            var keyAccumulator = ((Key x, Key y) =>
            {
                var tmp = new Key(new byte[32]);
                KeyOps.ScalarAdd(ref tmp, ref x, ref y);
                Array.Copy(tmp.bytes, x.bytes, tmp.bytes.Length);
                return x;
            });

            var sumGammas = outs.Select(x => x.Item1).Aggregate(Key.Zero(), keyAccumulator);

            var blindingFactors = utxosList.Take(utxosList.Count - 1).Select(_ => KeyOps.GenerateSeckey()).ToList();
            var z = new Key(new byte[32]);
            var sumBlinds = blindingFactors.Aggregate(Key.Zero(), keyAccumulator);
            KeyOps.ScalarSub(ref z, ref sumGammas, ref sumBlinds);
            blindingFactors.Add(z);

            var pseudos = utxosList.Zip(blindingFactors)
                .Select(x => 
                {
                    Key comm = new(new byte[32]);
                    KeyOps.GenCommitment(ref comm, ref x.Second, x.First.DecodedAmount);
                    return comm;
                }).ToList();

            return (pseudos, blindingFactors);
        }

        protected IEnumerable<Coin.Models.Triptych> BuildPrivateSignatures(IEnumerable<UTXO> utxos, IEnumerable<PrivateTxInput> inputs, IEnumerable<Key> pseudos, IEnumerable<Key> blindingFactors, SHA256 message)
        {
            return utxos.Where(x => x.Type == 0).Zip(inputs).Zip(pseudos).Zip(blindingFactors)
                .Select(x => (x.First.First.First, x.First.First.Second, x.First.Second, x.Second))
                .Select(x =>
                {
                    var txKey = x.First.TransactionKey.Value;
                    var sign_r = KeyOps.DKSAPRecover(ref txKey, ref x.First.Account.SecViewKey, ref x.First.Account.SecSpendKey, x.First.DecodeIndex ?? 0);
                    var sign_x = KeyOps.GenCommitmentMaskRecover(ref txKey, ref x.First.Account.SecViewKey, x.First.DecodeIndex ?? 0);
                    if (x.First.IsCoinbase) sign_x = Key.Identity();
                    var sign_s = Key.Zero();
                    KeyOps.ScalarSub(ref sign_s, ref sign_x, ref x.Item4);

                    var ringsig = Cipher.Triptych.Prove(x.Item2.M, x.Item2.P, x.Item3, (uint)x.Item2.l, sign_r, sign_s, message.ToKey());
                    return new Coin.Models.Triptych(ringsig);
                }).ToList();
        }

        protected IEnumerable<Signature> BuildTransparentSignatures(IEnumerable<UTXO> utxos, IEnumerable<TTXInput> tinputs, SHA256 message)
        {
            return utxos.Where(x => x.Type == 1).Zip(tinputs).Select(x =>
            {
                byte[] data = new byte[64];
                Array.Copy(message.Bytes, data, 32);
                Array.Copy(x.Second.Hash(new TTXOutput
                {
                    TransactionSrc = x.First.TransactionSrc,
                    Address = new TAddress(x.First.Account.Address),
                    Amount = x.First.DecodedAmount
                }).Bytes, 0, data, 32, 32);

                var hashToSign = SHA256.HashData(data);
                return new Signature(x.First.Account.SecKey, x.First.Account.PubKey.Value, hashToSign);
            }).ToList();
        }
    }
}
