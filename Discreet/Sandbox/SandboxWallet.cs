using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Coin.Models;
using Discreet.DB;
using Discreet.Sandbox.Extensions;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox
{
    public class SandboxWallet
    {
        public string Address { get; set; }

        public string Name { get; set; }

        public Key? PubKey { get; set; }
        public Key? SecKey { get; set; }

        public Key? PubSpendKey { get; set; }
        public Key? PubViewKey { get; set; }

        public Key SecSpendKey;
        public Key SecViewKey;

        public byte Type { get; set; }

        public ulong Balance = 0;

        public HashSet<SandboxUtxo> Utxos { get; set; } = new HashSet<SandboxUtxo>(new SandboxUtxoEqualityComparer());
        public Dictionary<ScriptAddress, List<SandboxUtxo>> Watchers = new();

        public SandboxWallet(string name, bool @private = false)
        {
            Name = name;
            if (!@private)
            {
                (SecKey, PubKey) = KeyOps.GenerateKeypair();
                Address = new TAddress(PubKey.Value).ToString();

                Type = 1;
            }
            else
            {
                (SecSpendKey, PubSpendKey) = KeyOps.GenerateKeypair();
                (SecViewKey, PubViewKey) = KeyOps.GenerateKeypair();
                Address = new StealthAddress(PubViewKey.Value, PubSpendKey.Value).ToString();

                Type = 0;
            }
        }

        public void ParseBlock(Block block)
        {
            List<SandboxUtxo> utxos = new();
            List<SandboxUtxo> spents = new();

            if (block.Header.Version != 1 && block.Header.Height > 0)
            {
                var coinbase = block.Transactions[0].ToPrivate();
                if (coinbase != null && Type == 0)
                {
                    Key txk = coinbase.TransactionKey;
                    Key outSecKey = KeyOps.DKSAPRecover(ref txk, ref SecViewKey, ref SecSpendKey, 0);
                    Key outPubKey = KeyOps.ScalarmultBase(outSecKey);

                    if (coinbase.Outputs[0].UXKey.Equals(outPubKey))
                    {
                        var utxo = new SandboxUtxo
                        {
                            Type = 0,
                            IsCoinbase = true,
                            TxSrc = coinbase.TxID,
                            Amount = coinbase.Outputs[0].Amount,
                            UXKey = coinbase.Outputs[0].UXKey,
                            Commitment = coinbase.Outputs[0].Commitment,
                            DecodeIndex = 0,
                            OutputIndex = ViewProvider.GetDefaultProvider().GetOutputIndices(coinbase.TxID)[0],
                            TxKey = txk,
                            DecodedAmount = coinbase.Outputs[0].Amount,
                            LinkingTag = KeyOps.GenerateLinkingTag(ref outSecKey),
                        };

                        utxos.Add(utxo);
                    }
                }
            }

            for (int k = block.Header.Version == 1 ? 0 : 1; k < block.Transactions.Length; k++)
            {
                var tx = block.Transactions[k];
                bool tToP = (tx.Version == 4 && tx.NumTInputs > 0 && tx.NumPOutputs > 0);

                if (Type == 0 && tx.PInputs != null)
                {
                    foreach (var pin in tx.PInputs)
                    {
                        if (Utxos.TryGetValue(pin, out var ux))
                        {
                            spents.Add(ux);
                        }
                    }
                }
                else if (Type == 1 && tx.TInputs != null)
                {
                    foreach (var tin in tx.TInputs)
                    {
                        if (Utxos.TryGetValue(tin, out var ux))
                        {
                            spents.Add(ux);
                        }
                    }
                }

                if (Type == 0 && tx.POutputs != null)
                {
                    var txk = tx.TransactionKey;
                    Key cscalar = tx.NumPOutputs > 0 ? KeyOps.ScalarmultKey(ref txk, ref SecViewKey) : default;
                    for (int i = 0; i < tx.NumPOutputs; i++)
                    {
                        var pk = PubSpendKey.Value;
                        var uxkey = tx.POutputs[i].UXKey;
                        if (KeyOps.CheckForBalance(ref cscalar, ref pk, ref uxkey, i))
                        {
                            var outSecKey = KeyOps.DKSAPRecover(ref txk, ref SecViewKey, ref SecSpendKey, i);
                            var utxo = new SandboxUtxo
                            {
                                Type = 0,
                                IsCoinbase = tToP,
                                TxSrc = tx.TxID,
                                Amount = tx.POutputs[i].Amount,
                                UXKey = tx.POutputs[i].UXKey,
                                UXSecKey = outSecKey,
                                Commitment = tx.POutputs[i].Commitment,
                                DecodeIndex = i,
                                TxKey = tx.TransactionKey,
                                DecodedAmount = tToP ? tx.POutputs[i].Amount : KeyOps.GenAmountMaskRecover(ref txk, ref SecViewKey, i, tx.POutputs[i].Amount),
                                LinkingTag = KeyOps.GenerateLinkingTag(ref outSecKey),
                                OutputIndex = ViewProvider.GetDefaultProvider().GetOutputIndices(tx.TxID)[i],
                            };

                            // malformed check is done in the other wallet code. I'm sure I had a good reason for it, so I'm including it here too.
                            if (utxo.IsCoinbase)
                            {
                                var chkComm = new Key(new byte[32]);
                                var mask = Key.Identity();
                                KeyOps.GenCommitment(ref chkComm, ref mask, utxo.DecodedAmount);

                                if (chkComm == utxo.Commitment)
                                {
                                    utxos.Add(utxo);
                                }
                                else
                                {
                                    Daemon.Logger.Error($"SandboxWallet.ParseBlock: potential malformed amount field in coinbase utxo. Output ignored.");
                                }
                            }
                            else
                            {
                                var chkComm = new Key(new byte[32]);
                                var mask = KeyOps.GenCommitmentMaskRecover(ref txk, ref SecViewKey, i);
                                KeyOps.GenCommitment(ref chkComm, ref mask, utxo.DecodedAmount);

                                if (chkComm == utxo.Commitment)
                                {
                                    utxos.Add(utxo);
                                }
                                else
                                {
                                    Daemon.Logger.Error($"SandboxWallet.ParseBlock: potential malformed amount field in private utxo. Output ignored.");
                                }
                            }
                        }
                    }
                }
                else if (Type == 1 && tx.TOutputs != null)
                {
                    for (int i = 0; i < tx.TOutputs.Length; i++)
                    {
                        if (Address == tx.TOutputs[i].Address.ToString())
                        {
                            var utxo = new SandboxUtxo
                            {
                                Type = 1,
                                IsCoinbase = false,
                                TxSrc = tx.TxID,
                                Amount = tx.TOutputs[i].Amount,
                                DecodeIndex = i,
                                OutputIndex = (uint)i,
                                DecodedAmount = tx.TOutputs[i].Amount,
                            };

                            utxos.Add(utxo);
                        }
                    }
                }
            }

            // first add new utxos, then remove spents
            utxos.ForEach(x => Utxos.Add(x));
            spents.ForEach(x => Utxos.Remove(x));
        }

        public FullTransaction CreateTransaction(IList<(ulong, IAddress)> recipients)
        {
            byte type = Wallets.Services.CreateTx.GetTransactionType(Type, recipients.Select(x => x.Item2));

            var poutproto = recipients.Where(x => x.Item2.Type() == (byte)AddressType.STEALTH);
            var toutproto = recipients.Where(x => x.Item2.Type() == (byte)AddressType.TRANSPARENT);

            var neededAmount = 0ul;
            var totalAmount = recipients.Aggregate(0ul, (x, y) => x + y.Item1);
            var utxos = Utxos.TakeWhile((ux) =>
            {
                neededAmount += ux.DecodedAmount;
                return neededAmount < totalAmount;
            }).ToList();

            if (neededAmount < totalAmount) throw new Exception("Not enough coins");

            var pinputs = Type == 1 ? [] : utxos.Where(x => x.Type == 0).Select(x =>
            {
                (TXOutput[] mixins, int l) = ViewProvider.GetDefaultProvider().GetMixins(x.OutputIndex);
                return new PrivateTxInput
                {
                    M = mixins.Select(x => x.UXKey).ToArray(),
                    P = mixins.Select(x => x.Commitment).ToArray(),
                    l = l,
                    Input = new TXInput
                    {
                        Offsets = mixins.Select(x => x.Index).ToArray(),
                        KeyImage = x.LinkingTag,
                    },
                };
            }).ToArray();

            var tinputs = Type == 0 ? [] : utxos.Where(x => x.Type == 1).Select(x => new TTXInput { TxSrc = x.TxSrc, Offset = (byte)x.OutputIndex }).ToArray();

            (var txPrivateKey, var txPublicKey) = (Type == 0) ? KeyOps.GenerateKeypair() : (default, default);

            if (totalAmount != neededAmount && Type == 1)
            {
                toutproto = toutproto.Append(((neededAmount - totalAmount), new TAddress(Address)));
            }
            else if (totalAmount != neededAmount && Type == 0)
            {
                poutproto = poutproto.Append(((neededAmount - totalAmount), new StealthAddress(Address)));
            }

            var toutputs = toutproto.Where(x => x.Item2.Type() == (byte)AddressType.TRANSPARENT).Select(x => new ScriptTXOutput(default, (TAddress)x.Item2, x.Item1)).ToArray();
            var __i = 0;
            var _maskAmountPairs = new List<(Key, ulong)>();
            var poutputs = poutproto.Where(x => x.Item2.Type() == (byte)AddressType.STEALTH).Select(x => (x.Item1, (StealthAddress)x.Item2)).Select(x =>
            {
                var mask = Type == 1 ? Key.Identity() : KeyOps.GenCommitmentMask(ref txPrivateKey, ref x.Item2.view, __i);
                _maskAmountPairs.Add((mask, x.Item1));
                Key comm = new Key(new byte[32]);
                KeyOps.GenCommitment(ref comm, ref mask, x.Item1);
                return new TXOutput
                {
                    UXKey = KeyOps.DKSAP(ref txPrivateKey, x.Item2.view, x.Item2.spend, __i),
                    Commitment = comm,
                    Amount = (Type == 1) ? x.Item1 : KeyOps.GenAmountMask(ref txPrivateKey, ref x.Item2.view, __i++, x.Item1),
                };
            }).ToArray();

            Coin.Models.BulletproofPlus bp = (poutputs.Length != 0) ? new Coin.Models.BulletproofPlus(Cipher.BulletproofPlus.Prove(_maskAmountPairs.Select(x => x.Item2).ToArray(), _maskAmountPairs.Select(x => x.Item1).ToArray())) : null;

            var keyAccumulator = (Key x, Key y) =>
            {
                var tmp = new Key(new byte[32]);
                KeyOps.ScalarAdd(ref tmp, ref x, ref y);
                Array.Copy(tmp.bytes, x.bytes, tmp.bytes.Length);
                return x;
            };
            var utxoList = utxos.Where(x => x.Type == 0).ToList();
            var sumGammas = _maskAmountPairs.Select(x => x.Item1).Aggregate(Key.Zero(), keyAccumulator);

            var blindingFactors = utxoList.Take(utxoList.Count - 1).Select(_ => KeyOps.GenerateSeckey()).ToList();
            var z = new Key(new byte[32]);
            var sumBlinds = blindingFactors.Aggregate(Key.Zero(), keyAccumulator);
            KeyOps.ScalarSub(ref z, ref sumGammas, ref sumBlinds);
            blindingFactors.Add(z);

            var pseudoOuts = utxoList.Zip(blindingFactors).Select(x =>
            {
                Key comm = new(new byte[32]);
                KeyOps.GenCommitment(ref comm, ref x.Second, x.First.DecodedAmount);
                return comm;
            }).ToList();

            var ftx = new FullTransaction
            {
                Version = type,
                NumInputs = (byte)(tinputs.Length + pinputs.Length),
                NumOutputs = (byte)(toutputs.Length + poutputs.Length),
                NumSigs = (byte)(tinputs.Length + pinputs.Length),
                NumPInputs = (byte)(pinputs.Length),
                NumTInputs = (byte)(tinputs.Length),
                NumTOutputs = (byte)(toutputs.Length),
                NumPOutputs = (byte)(poutputs.Length),
                NumRefInputs = 0,
                NumScriptInputs = 0,
                Fee = 0,
                PInputs = pinputs.Select(x => x.Input).ToArray(),
                TInputs = tinputs,
                RefInputs = Array.Empty<TTXInput>(),
                TOutputs = toutputs,
                POutputs = poutputs,
                RangeProofPlus = bp,
                PseudoOutputs = pseudoOuts.ToArray(),
                _datums = [],
                _redeemers = [],
                _scripts = [],
                TransactionKey = (poutputs.Length != 0) ? txPublicKey : default,
            };

            ftx.SigningHash = ftx.GetSigningHash();

            ftx.PSignatures = utxos.Where(x => x.Type == 0).Zip(pinputs).Zip(pseudoOuts).Zip(blindingFactors)
                .Select(x => (x.First.First.First, x.First.First.Second, x.First.Second, x.Second))
                .Select(x =>
                {
                    var txkey = x.First.TxKey;
                    var sign_r = KeyOps.DKSAPRecover(ref txkey, ref SecViewKey, ref SecSpendKey, x.First.DecodeIndex);
                    var sign_x = KeyOps.GenCommitmentMaskRecover(ref txkey, ref SecViewKey, x.First.DecodeIndex);
                    if (x.First.IsCoinbase) sign_x = Key.Identity();
                    var sign_s = Key.Zero();
                    KeyOps.ScalarSub(ref sign_s, ref sign_x, ref x.Item4);

                    var ringsig = Cipher.Triptych.Prove(x.Item2.M, x.Item2.P, x.Item3, (uint)x.Item2.l, sign_r, sign_s, ftx.SigningHash.ToKey());
                    return new Coin.Models.Triptych(ringsig);
                }).ToArray();

            ftx.TSignatures = Enumerable.Range(0, tinputs.Length).Select(x => (byte)x).Zip(utxos.Where(x => x.Type == 1).Zip(tinputs).Select(x =>
            {
                byte[] data = new byte[64];
                Array.Copy(ftx.SigningHash.Bytes, data, 32);
                Array.Copy(x.Second.Hash(new TTXOutput
                {
                    TransactionSrc = x.First.TxSrc,
                    Address = new TAddress(Address),
                    Amount = x.First.DecodedAmount
                }).Bytes, 0, data, 32, 32);

                var hashToSign = SHA256.HashData(data);
                return new Signature(SecKey.Value, PubKey.Value, hashToSign);
            }).ToArray()).ToArray();

            return ftx;
        }
    }
}
