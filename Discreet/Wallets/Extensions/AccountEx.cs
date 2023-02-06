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
                        utxos.Add(new UTXO
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
                        });

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
                Key cscalar = numPOutputs > 0 ? KeyOps.ScalarmultKey(ref transaction.TransactionKey, ref account.SecViewKey) : default;
                for (int i = 0; i < transaction.POutputs.Length; i++)
                {
                    var pubkey = account.PubSpendKey;
                    if (KeyOps.CheckForBalance(ref cscalar, ref pubkey, ref transaction.POutputs[i].UXKey, i))
                    {
                        var outputSecKey = KeyOps.DKSAPRecover(ref transaction.TransactionKey, ref account.SecViewKey, ref account.SecSpendKey, i);
                        var utxo = new UTXO
                        {
                            Address = account.Address,
                            Type = 0,
                            IsCoinbase = false,
                            TransactionSrc = transaction.TxID,
                            Amount = transaction.POutputs[i].Amount,
                            UXKey = transaction.POutputs[i].UXKey,
                            UXSecKey = outputSecKey,
                            Commitment = transaction.POutputs[i].Commitment,
                            DecodeIndex = i,
                            TransactionKey = transaction.TransactionKey,
                            DecodedAmount = KeyOps.GenAmountMask(ref transaction.TransactionKey, ref account.SecViewKey, i, transaction.POutputs[i].Amount),
                            LinkingTag = KeyOps.GenerateLinkingTag(ref outputSecKey),
                            Encrypted = false,
                            Account = account
                        };
                        //TODO: remember to include txoutput index data for SPV (part of refactor)
                        utxo.Index = ViewProvider.GetDefaultProvider().GetOutputIndices(transaction.TxID)[i];

                        newUtxo.Add((utxo, i));
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
                            DecodedAmount = transaction.TOutputs[i].Amount,
                            Encrypted = false,
                            Account = account
                        };

                        utxos.Add(utxo);
                    }
                }
            }

            if (!account.TxHistory.Contains(transaction.TxID))
            {
                txs.Add(AddTransactionToHistory(account, transaction, newSpents, newUtxo, new DateTime(timestamp).ToLocalTime().Ticks));
            }

            spents.AddRange(newUtxo.Select(x => x.Item1));
        }

        public static HistoryTx AddTransactionToHistory(this Account account, FullTransaction tx, List<UTXO> spents, List<(UTXO, int)> unspents, long timestamp)
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
                        inputAmounts.Add(0);
                        inputAddresses.Add("Unknown");
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
    }
}
