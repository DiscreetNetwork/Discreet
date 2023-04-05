using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Wallets.Models;
using Discreet.Wallets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discreet.DB;
using System.Security.Principal;
using Discreet.Wallets.Comparers;
using System.Drawing;
using System.Collections.Concurrent;
using Discreet.Coin.Models;

namespace Discreet.Wallets.Extensions
{
    public static class AccountEx
    {
        public static (List<UTXO>? spents, List<UTXO>? utxos, List<HistoryTx>? htxs) ProcessBlock(this Account account, Block block)
        {
            List<UTXO> utxos = new();
            List<HistoryTx> txs = new();
            List<UTXO> spents = new();

            if (account.Encrypted) throw new Exception("account is encrypted");
            if (block.Header.Version != 1)
            {
                var Coinbase = block.Transactions[0].ToPrivate();
                if (Coinbase != null && account.Type == 0)
                {
                    Key txKey = Coinbase.TransactionKey;
                    Key outputSecKey = KeyOps.DKSAPRecover(ref txKey, ref account.SecViewKey, ref account.SecSpendKey, 0);
                    Key outputPubKey = KeyOps.ScalarmultBase(ref outputSecKey);

                    if (Coinbase.Outputs[0].UXKey.Equals(outputPubKey))
                    {
                        var utxo = new UTXO
                        {
                            Address = account.Address,
                            Type = 0,
                            IsCoinbase = true,
                            TransactionSrc = Coinbase.TxID,
                            Amount = Coinbase.Outputs[0].Amount,
                            UXKey = outputPubKey,
                            UXSecKey = outputSecKey,
                            Commitment = Coinbase.Outputs[0].Commitment,
                            DecodeIndex = 0,
                            TransactionKey = txKey,
                            DecodedAmount = Coinbase.Outputs[0].Amount,
                            LinkingTag = KeyOps.GenerateLinkingTag(ref outputSecKey),
                            Encrypted = false,
                            Account = account
                        };

                        utxo.Index = ViewProvider.GetDefaultProvider().GetOutputIndices(Coinbase.TxID)[0];
                        utxos.Add(utxo);

                        txs.Add(new HistoryTx
                        {
                            Address = account.Address,
                            TxID = Coinbase.TxID,
                            Timestamp = new DateTime((long)block.Header.Timestamp).ToLocalTime().Ticks,
                            Inputs = new(),
                            Outputs = new(
                                new HistoryTxOutput[]
                                {
                                    new HistoryTxOutput
                                    {
                                        Amount = Coinbase.Outputs[0].Amount,
                                        Address = account.Address,
                                    },
                                }
                                ),
                            Account = account
                        });
                    }
                }
            }
            
            for (int i = block.Header.Version == 1 ? 0 : 1; i < block.Transactions.Length; i++)
            {
                account.ProcessTransaction(block.Transactions[i], (long)block.Header.Timestamp, spents, utxos, txs);
            }

            return (spents.Count > 0 ? spents : null, 
                utxos.Count > 0 ? utxos : null, 
                txs.Count > 0 ? txs : null);
        }

        public static void ProcessTransaction(this Account account, FullTransaction transaction, long timestamp, List<UTXO> spents, List<UTXO> utxos, List<HistoryTx> txs)
        {
            int numPOutputs = (transaction.Version == 4) ? transaction.NumPOutputs : ((transaction.Version == 3) ? 0 : transaction.NumOutputs);
            int numTOutputs = (transaction.Version == 4) ? transaction.NumTOutputs : ((transaction.Version == 3) ? transaction.NumOutputs : 0);

            bool tToP = (transaction.Version == 4 && transaction.NumTInputs > 0 && transaction.NumPOutputs > 0);
            List<UTXO> newSpents = new();
            List<(UTXO, int)> newUtxo = new();
            List<UTXO> newTutxo = new();

            if (account.Type == 0 && transaction.PInputs != null)
            {
                foreach (var pin in transaction.PInputs)
                {
                    lock (account.UTXOs)
                    {
                        bool contained = account.UTXOs.TryGetValue(pin, out var utxo);
                        if (contained) newSpents.Add(utxo);
                    }
                }
            }
            else if (account.Type == 1 && transaction.TInputs != null)
            {
                foreach (var tin in transaction.TInputs)
                {
                    lock (account.UTXOs)
                    {
                        bool contained = account.UTXOs.TryGetValue(tin, out var utxo);
                        if (contained) newSpents.Add(utxo);
                    }
                }
            }

            if (account.Type == 0 && transaction.POutputs != null)
            {
                var txkey = transaction.TransactionKey;
                Key cscalar = numPOutputs > 0 ? KeyOps.ScalarmultKey(ref txkey, ref account.SecViewKey) : default;
                for (int i = 0; i < transaction.POutputs.Length; i++)
                {
                    var pubkey = account.PubSpendKey.Value;
                    var uxkey = transaction.POutputs[i].UXKey;
                    if (KeyOps.CheckForBalance(ref cscalar, ref pubkey, ref uxkey, i))
                    {
                        var outputSecKey = KeyOps.DKSAPRecover(ref txkey, ref account.SecViewKey, ref account.SecSpendKey, i);
                        var utxo = new UTXO
                        {
                            Address = account.Address,
                            Type = 0,
                            IsCoinbase = tToP,
                            TransactionSrc = transaction.TxID,
                            Amount = transaction.POutputs[i].Amount,
                            UXKey = transaction.POutputs[i].UXKey,
                            UXSecKey = outputSecKey,
                            Commitment = transaction.POutputs[i].Commitment,
                            DecodeIndex = i,
                            TransactionKey = transaction.TransactionKey,
                            DecodedAmount = tToP ? transaction.POutputs[i].Amount : KeyOps.GenAmountMaskRecover(ref txkey, ref account.SecViewKey, i, transaction.POutputs[i].Amount),
                            LinkingTag = KeyOps.GenerateLinkingTag(ref outputSecKey),
                            Encrypted = false,
                            Account = account
                        };
                        //TODO: remember to include txoutput index data for SPV (part of refactor)
                        utxo.Index = ViewProvider.GetDefaultProvider().GetOutputIndices(transaction.TxID)[i];

                        // if an attacker wanted he could arbitrarily set the amount field wrong and corrupt wallet. check correctness.
                        if (utxo.IsCoinbase)
                        {
                            // 1G + bH
                            var checkCommitment = new Key(new byte[32]);
                            var mask = Key.Identity();
                            KeyOps.GenCommitment(ref checkCommitment, ref mask, utxo.DecodedAmount);
                            if (checkCommitment != utxo.Commitment)
                            {
                                Daemon.Logger.Error($"Account.ProcessTransaction: potential malformed amount field in coinbase utxo. Output ignored.");
                            }
                            else
                            {
                                newUtxo.Add((utxo, i));
                            }
                        }
                        else
                        {
                            var checkCommitment = new Key(new byte[32]);
                            var mask = KeyOps.GenCommitmentMaskRecover(ref txkey, ref account.SecViewKey, i);
                            KeyOps.GenCommitment(ref checkCommitment, ref mask, utxo.DecodedAmount);

                            if (checkCommitment != utxo.Commitment)
                            {
                                Daemon.Logger.Error($"Account.ProcessTransaction: potential malformed amount field in private utxo. Output ignored.");
                            }
                            else
                            {
                                newUtxo.Add((utxo, i));
                            }
                        }
                    }
                }
            }
            else if (account.Type == 1 && transaction.TOutputs != null)
            {
                for (int i = 0; i < transaction.TOutputs.Length; i++)
                {
                    if (account.Address == transaction.TOutputs[i].Address.ToString())
                    {
                        var utxo = new UTXO
                        {
                            Address = account.Address,
                            Type = 1,
                            IsCoinbase = false,
                            TransactionSrc = transaction.TxID,
                            Amount = transaction.TOutputs[i].Amount,
                            DecodeIndex = i,
                            Index = (uint)i,
                            DecodedAmount = transaction.TOutputs[i].Amount,
                            Encrypted = false,
                            Account = account
                        };

                        newTutxo.Add(utxo);
                    }
                }
            }

            if (!account.TxHistory.Contains(transaction.TxID) && (newSpents.Count > 0 || newUtxo.Count > 0 || newTutxo.Count > 0))
            {
                txs.Add(AddTransactionToHistory(account, transaction, newSpents, newUtxo, new DateTime(timestamp).ToLocalTime().Ticks));
            }

            utxos.AddRange(newUtxo.Select(x => x.Item1));
            utxos.AddRange(newTutxo);
            spents.AddRange(newSpents);
        }

        public static HistoryTx AddTransactionToHistory(this Account account, FullTransaction tx, IEnumerable<UTXO> spents, IEnumerable<(UTXO, int)> unspents, long timestamp)
        {
            List<ulong> inputAmounts = new List<ulong>();
            List<ulong> outputAmounts = new List<ulong>();
            List<string> inputAddresses = new List<string>();
            List<string> outputAddresses = new List<string>();

            var numTInputs = tx.TInputs == null ? 0 : tx.TInputs.Length;
            var numPInputs = tx.PInputs == null ? 0 : tx.PInputs.Length;
            var numTOutputs = tx.TOutputs == null ? 0 : tx.TOutputs.Length;
            var numPOutputs = tx.POutputs == null ? 0 : tx.POutputs.Length;

            for (int i = 0; i < numTInputs; i++)
            {
                //TODO: remember to unpack transparent.txinputs for SPV (part of refactor)
                var _input = ViewProvider.GetDefaultProvider().MustGetPubOutput(tx.TInputs[i]);
                inputAmounts.Add(_input.Amount);
                inputAddresses.Add(_input.Address.ToString());
            }

            for (int i = 0; i < numPInputs; i++)
            {
                if (account.Type == (byte)AddressType.STEALTH)
                {
                    bool _added = false;

                    foreach (var utxo in spents)
                    {
                        if (tx.PInputs[i].KeyImage == utxo.LinkingTag)
                        {
                            inputAmounts.Add(utxo.DecodedAmount);
                            inputAddresses.Add(account.Address);
                            _added = true;
                        }
                    }

                    if (!_added)
                    {
                        inputAmounts.Add(0);
                        inputAddresses.Add("Unknown");
                    }
                }
                else
                {
                    inputAmounts.Add(0);
                    inputAddresses.Add("Unknown");
                }
            }

            for (int i = 0; i < numTOutputs; i++)
            {
                outputAmounts.Add(tx.TOutputs[i].Amount);
                outputAddresses.Add(tx.TOutputs[i].Address.ToString());
            }

            for (int i = 0; i < numPOutputs; i++)
            {
                if (account.Type == (byte)AddressType.STEALTH)
                {
                    bool _added = false;

                    foreach (var utxo in unspents)
                    {
                        if (i == utxo.Item2)
                        {
                            outputAmounts.Add(utxo.Item1.DecodedAmount);
                            outputAddresses.Add(account.Address);
                            _added = true;
                        }
                    }

                    if (!_added)
                    {
                        outputAmounts.Add(0);
                        outputAddresses.Add("Unknown");
                    }
                }
                else
                {
                    outputAmounts.Add(0);
                    outputAddresses.Add("Unknown");
                }
            }

            HistoryTx htx = new HistoryTx(account, tx.TxID, inputAmounts.ToArray(), inputAddresses.ToArray(), outputAmounts.ToArray(), outputAddresses.ToArray(), timestamp);
            return htx;
        }

        public static void ConstructHistoryTxInputs(this HistoryTx htx, Account account, FullTransaction tx, IEnumerable<UTXO> spents)
        {
            var numTInputs = tx.TInputs == null ? 0 : tx.TInputs.Length;
            var numPInputs = tx.PInputs == null ? 0 : tx.PInputs.Length;

            for (int i = 0; i < numPInputs; i++)
            {
                if (account.Type == (byte)AddressType.STEALTH)
                {
                    foreach (var utxo in spents)
                    {
                        if (tx.PInputs[i].KeyImage == utxo.LinkingTag)
                        {
                            htx.Inputs[numTInputs + i].Amount = utxo.DecodedAmount;
                            htx.Inputs[numTInputs + i].Address = account.Address;
                        }
                    }
                }
            }
        }

        public static HistoryTx ConstructHistoryTxOutputs(FullTransaction tx, Account account, ulong timestamp, IEnumerable<(UTXO, uint)> unspents)
        {
            List<ulong> inputAmounts = new List<ulong>();
            List<string> inputAddresses = new List<string>();
            List<ulong> outputAmounts = new List<ulong>();
            List<string> outputAddresses = new List<string>();

            var numTInputs = tx.TInputs == null ? 0 : tx.TInputs.Length;
            var numPInputs = tx.PInputs == null ? 0 : tx.PInputs.Length;
            var numTOutputs = tx.TOutputs == null ? 0 : tx.TOutputs.Length;
            var numPOutputs = tx.POutputs == null ? 0 : tx.POutputs.Length;

            for (int i = 0; i < numTInputs; i++)
            {
                //TODO: remember to unpack transparent.txinputs for SPV (part of refactor)
                var _input = ViewProvider.GetDefaultProvider().MustGetPubOutput(tx.TInputs[i]);
                inputAmounts.Add(_input.Amount);
                inputAddresses.Add(_input.Address.ToString());
            }

            for (int i = 0; i < numPInputs; i++)
            {
                inputAmounts.Add(0);
                inputAddresses.Add("Unknown");
            }

            for (int i = 0; i < numTOutputs; i++)
            {
                outputAmounts.Add(tx.TOutputs[i].Amount);
                outputAddresses.Add(tx.TOutputs[i].Address.ToString());
            }

            for (int i = 0; i < numPOutputs; i++)
            {
                if (account.Type == (byte)AddressType.STEALTH)
                {
                    bool _added = false;

                    foreach (var utxo in unspents)
                    {
                        if (i == utxo.Item2)
                        {
                            outputAmounts.Add(utxo.Item1.DecodedAmount);
                            outputAddresses.Add(account.Address);
                            _added = true;
                        }
                    }

                    if (!_added)
                    {
                        outputAmounts.Add(0);
                        outputAddresses.Add("Unknown");
                    }
                }
                else
                {
                    outputAmounts.Add(0);
                    outputAddresses.Add("Unknown");
                }
            }

            return new HistoryTx
            {
                Address = account.Address,
                TxID = tx.TxID,
                Timestamp = new DateTime((long)timestamp).ToLocalTime().Ticks,
                Account = account,
                Inputs = inputAddresses.Zip(inputAmounts).Select(x => new HistoryTxOutput { Address = x.First, Amount = x.Second }).ToList(),
                Outputs = outputAddresses.Zip(outputAmounts).Select(x => new HistoryTxOutput { Address = x.First, Amount = x.Second}).ToList()
            };
        }

        public static (IEnumerable<UTXO>? spents, IEnumerable<UTXO>? utxos, IEnumerable<HistoryTx>? htxs) ProcessBlocks(this Account account, IEnumerable<Block> blocks)
        {
            List<UTXO> utxos = new();
            List<HistoryTx> htxs = new();
            List<UTXO> spents = new();

            if (account.Encrypted) throw new Exception("account is encrypted");

            IEnumerable<(ulong, FullTransaction)> txs = blocks.SelectMany(x => Enumerable.Repeat(x.Header.Timestamp, (int)x.Header.NumTXs).Zip(x.Transactions));
            var beginUnspents = ProcessTxsForOutputs(account, txs);
            return ProcessTxsForInputs(account, txs, beginUnspents);
        }

        public static (IEnumerable<UTXO>? spents, IEnumerable<UTXO>? utxos, IEnumerable<HistoryTx>? htxs) ProcessTxsForInputs(this Account account, IEnumerable<(ulong, FullTransaction)> txs, IEnumerable<(IEnumerable<UTXO> unspents, HistoryTx htx)> unspentsAndHtxs)
        {
            HashSet<UTXO> unspents = null;
            Dictionary<SHA256, HistoryTx> htxs = null;
            ConcurrentQueue<HistoryTx> newHtxs = new();

            if (unspentsAndHtxs.Any())
            {
                unspents = new(unspentsAndHtxs.SelectMany(x => x.unspents), new UTXOEqualityComparer());
                htxs = unspentsAndHtxs.Select(x => x.htx).ToDictionary(x => x.TxID, new SHA256EqualityComparer());
            }
            if (account.Type == 0)
            {
                lock (account.UTXOs)
                {
                    var spents = txs.AsParallel().AsUnordered().SelectMany(tx =>
                    {
                        List<UTXO> spents = null;
                        bool existingHtx = false;
                        if (tx.Item2.NumPInputs > 0)
                        {
                            foreach (var pin in tx.Item2.PInputs)
                            {
                                bool contained = account.UTXOs.TryGetValue(pin, out var utxo);
                                if (contained)
                                {
                                    spents ??= new();
                                    if (htxs is not null && htxs.ContainsKey(tx.Item2.TxID))
                                    {
                                        existingHtx = true;
                                    }
                                    spents.Add(utxo);
                                }
                                else if (unspents is not null && unspents.TryGetValue(pin, out utxo))
                                {
                                    spents ??= new();
                                    lock (unspents) unspents.Remove(utxo);
                                    //spents.Add(utxo);
                                }
                            }

                            if (spents is not null)
                            {
                                if (existingHtx) htxs[tx.Item2.TxID].ConstructHistoryTxInputs(account, tx.Item2, spents);
                                else
                                {
                                    newHtxs.Enqueue(AddTransactionToHistory(account, tx.Item2, spents, Array.Empty<(UTXO, int)>(), new DateTime((long)tx.Item1).ToLocalTime().Ticks));
                                }
                            }
                        }

                        return spents ?? (IEnumerable<UTXO>)Array.Empty<UTXO>();
                    }).ToList();

                    var htxret = htxs == null ? (newHtxs.Count == 0 ? null : newHtxs) : htxs.Values.Concat(newHtxs);
                    return (spents.Any() ? spents : null, unspents, htxret);
                }
            }
            else if (account.Type == 1)
            {
                lock (account.UTXOs)
                {
                    var spents = txs.AsParallel().AsUnordered().SelectMany(tx =>
                    {
                        List<UTXO> spents = null;
                        bool existingHtx = false;
                        if (tx.Item2.NumTInputs > 0)
                        {
                            foreach (var tin in tx.Item2.TInputs)
                            {
                                bool contained = account.UTXOs.TryGetValue(tin, out var utxo);
                                if (contained)
                                {
                                    spents ??= new();
                                    if (htxs is not null && htxs.ContainsKey(tx.Item2.TxID))
                                    {
                                        existingHtx = true;
                                    }
                                    spents.Add(utxo);
                                }
                                else if (unspents is not null && unspents.TryGetValue(tin, out utxo))
                                {
                                    spents ??= new();
                                    lock (unspents) unspents.Remove(utxo);
                                    //spents.Add(utxo); WE REMOVED ALREADY
                                }
                            }

                            if (spents is not null)
                            {
                                if (!existingHtx)
                                {
                                    newHtxs.Enqueue(AddTransactionToHistory(account, tx.Item2, spents, Array.Empty<(UTXO, int)>(), new DateTime((long)tx.Item1).ToLocalTime().Ticks));
                                }
                            }
                        }

                        return spents ?? (IEnumerable<UTXO>)Array.Empty<UTXO>();
                    }).ToList();

                    var htxret = htxs == null ? (newHtxs.Count == 0 ? null : newHtxs) : htxs.Values.Concat(newHtxs);
                    return (spents.Any() ? spents : null, unspents, htxret);
                }
            }
            else throw new FormatException(nameof(account));
        }

        public static IEnumerable<(IEnumerable<UTXO> unspents, HistoryTx htx)> ProcessTxsForOutputs(this Account account, IEnumerable<(ulong, FullTransaction)> txs)
        {
            if (account.Type == 0)
            {
                var view = ViewProvider.GetDefaultProvider();
                return txs.AsParallel().AsUnordered().Select(x => new MarkableFullTransaction(x, account)).Where(tx =>
                {
                    var numPOutputs = tx.tx.NumPOutputs;
                    var txkey = tx.tx.TransactionKey;
                    Key cscalar = numPOutputs > 0 ? KeyOps.ScalarmultKey(ref txkey, ref account.SecViewKey) : default;
                    bool any = false;
                    for (int i = 0; i < numPOutputs; i++)
                    {
                        var pubkey = account.PubSpendKey.Value;
                        var uxkey = tx.tx.POutputs[i].UXKey;
                        if (KeyOps.CheckForBalance(ref cscalar, ref pubkey, ref uxkey, i))
                        {
                            tx.markedbalance[i] = true;
                            any = true;
                        }
                    }

                    return any;
                }).Select(tx =>
                {
                    List<(UTXO, uint)> newUtxos = new();
                    for (int i = 0; i < tx.tx.NumPOutputs; i++)
                    {
                        if (tx.markedbalance[i])
                        {
                            bool tToP = (tx.tx.Version == 4 && tx.tx.NumTInputs > 0 && tx.tx.NumPOutputs > 0);
                            var txkey = tx.tx.TransactionKey;
                            var outputSecKey = KeyOps.DKSAPRecover(ref txkey, ref account.SecViewKey, ref account.SecSpendKey, i);
                            var utxo = new UTXO
                            {
                                Address = account.Address,
                                Type = 0,
                                IsCoinbase = tx.tx.Version == 0 || tToP,
                                TransactionSrc = tx.tx.TxID,
                                Amount = tx.tx.POutputs[i].Amount,
                                UXKey = tx.tx.POutputs[i].UXKey,
                                UXSecKey = outputSecKey,
                                Commitment = tx.tx.POutputs[i].Commitment,
                                Index = view.GetOutputIndices(tx.tx.TxID)[i],
                                DecodeIndex = i,
                                TransactionKey = tx.tx.TransactionKey,
                                DecodedAmount = (tx.tx.Version == 0 || tToP) ? tx.tx.POutputs[i].Amount : KeyOps.GenAmountMaskRecover(ref txkey, ref account.SecViewKey, i, tx.tx.POutputs[i].Amount),
                                LinkingTag = KeyOps.GenerateLinkingTag(ref outputSecKey),
                                Encrypted = false,
                                Account = account
                            };

                            // fixes a user error in block 1875896 and 1875898
                            if (utxo.UXKey.Value.ToHex() == "67ac33ab1a47c4b017c2e84d88c3e05b23b082c9ed359f87e1e4e77026843311"
                                || utxo.UXKey.Value.ToHex() == "e85f4db372bf70a0d7881d8659e6870a89a08ff4d3e1fc6cb71c02bc184e0bbc")
                            {
                                utxo.DecodedAmount = KeyOps.GenAmountMaskRecover(ref txkey, ref account.SecViewKey, i, tx.tx.POutputs[i].Amount);
                            }

                            if (utxo.IsCoinbase)
                            {
                                // 1G + bH
                                var checkCommitment = new Key(new byte[32]);
                                var mask = Key.Identity();
                                KeyOps.GenCommitment(ref checkCommitment, ref mask, utxo.DecodedAmount);
                                if (checkCommitment != utxo.Commitment)
                                {
                                    Daemon.Logger.Error($"Account.ProcessTransaction: potential malformed amount field in coinbase utxo. Output ignored.");
                                }
                                else
                                {
                                    newUtxos.Add((utxo, (uint)i));
                                }
                            }
                            else
                            {
                                var checkCommitment = new Key(new byte[32]);
                                var mask = KeyOps.GenCommitmentMaskRecover(ref txkey, ref account.SecViewKey, i);
                                KeyOps.GenCommitment(ref checkCommitment, ref mask, utxo.DecodedAmount);

                                if (checkCommitment != utxo.Commitment)
                                {
                                    Daemon.Logger.Error($"Account.ProcessTransaction: potential malformed amount field in private utxo. Output ignored.");
                                }
                                else
                                {
                                    newUtxos.Add((utxo, (uint)i));
                                }
                            }
                        }
                    }

                    return (newUtxos.Select(x => x.Item1), ConstructHistoryTxOutputs(tx.tx, account, tx.timestamp, newUtxos));
                }).ToList();
            }
            else if (account.Type == 1)
            {
                return txs.AsParallel().AsUnordered().Select(x => new MarkableFullTransaction(x, account)).Where(tx =>
                {
                    var numTOutputs = tx.tx.NumTOutputs;
                    bool any = false;
                    for (int i = 0; i < numTOutputs; i++)
                    {
                        if (account.Address == tx.tx.TOutputs[i].Address.ToString())
                        {
                            tx.markedbalance[i] = true;
                            any = true;
                        }
                    }

                    return any;
                }).Select(tx =>
                {
                    List<(UTXO, uint)> newUtxos = new();
                    for (int i = 0; i < tx.tx.NumTOutputs; i++)
                    {
                        if (tx.markedbalance[i])
                        {
                            var utxo = new UTXO
                            {
                                Address = account.Address,
                                Type = 1,
                                IsCoinbase = false,
                                TransactionSrc = tx.tx.TxID,
                                Amount = tx.tx.TOutputs[i].Amount,
                                DecodeIndex = i,
                                Index = (uint)i,
                                DecodedAmount = tx.tx.TOutputs[i].Amount,
                                Encrypted = false,
                                Account = account
                            };

                            newUtxos.Add((utxo, (uint)i));
                        }
                    }

                    return (newUtxos.Select(x => x.Item1), ConstructHistoryTxOutputs(tx.tx, account, tx.timestamp, Array.Empty<(UTXO, uint)>()));
                }).ToList();
            }
            else throw new FormatException(nameof(account));
        }

        internal class MarkableFullTransaction
        {
            internal FullTransaction tx;
            internal bool[] markedbalance;
            internal ulong timestamp;

            internal MarkableFullTransaction((ulong, FullTransaction) tx, Account account)
            {
                this.tx = tx.Item2;
                this.timestamp = tx.Item1;
                if (account.Type == 0) markedbalance = new bool[tx.Item2.NumPOutputs];
                else if (account.Type == 1) markedbalance = new bool[tx.Item2.NumTOutputs];
                else throw new ArgumentException(nameof(account));
            }
        }
    }
}
