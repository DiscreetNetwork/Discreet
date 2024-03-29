﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC.Common;
using Discreet.WalletsLegacy;
using Discreet.Common;
using Discreet.Coin;
using Discreet.Wallets;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;

namespace Discreet.RPC.Endpoints
{
    public static class TransactionEndpoints
    {
        public class RelayTxParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public FullTransaction Transaction { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Raw { get; set; }
        }

        [RPCEndpoint("relay_tx", APISet.TXN)]
        public static object RelayTx(RelayTxParams _params)
        {
            try
            {
                var _handler = Network.Handler.GetHandler();

                if (_handler.State == Network.PeerState.Startup) return new RPCError($"network not available during daemon startup");

                if (_params == null) return new RPCError("params was null");

                if (_params.Transaction == null && (_params.Raw == null || _params.Raw == "")) return new RPCError($"one of the following must be set: Transaction, Raw");

                if (_params.Transaction != null && _params.Raw != null && _params.Raw != "") return new RPCError($"only one of the following should be set: Transaction, Raw");

                FullTransaction tx;

                if (_params.Transaction != null)
                {
                    tx = _params.Transaction;
                }
                else if (_params.Raw != null)
                {
                    if (!Printable.IsHex(_params.Raw)) return new RPCError("raw transaction was not a hex string");

                    try
                    {
                        tx = new FullTransaction(Printable.Byteify(_params.Raw));
                    }
                    catch (Exception)
                    {
                        return new RPCError("raw transaction was malformed");
                    }
                }
                else
                {
                    tx = null;
                }

                var _exc = Daemon.TXPool.GetTXPool().CheckTx(tx);
                if (_exc != null)
                {
                    return new RPCError(-1, "raw transaction was invalid", _exc.Message);
                }

                _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));

                return tx.Hash();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to RelayTx failed: {ex.Message}", ex);

                return new RPCError($"Could not relay tx");
            }
        }

        public class CreateTransactionParam
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public IAddress To { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public ulong Amount { get; set; } 
        }

        public class CreateTransactionParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Address { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public List<CreateTransactionParam> Params { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool? Raw { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool? Relay { get; set; }
        }

        public class CreateTransactionRV
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Txid { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public FullTransaction Tx { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Raw { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Error { get; set; }
        }

        [RPCEndpoint("create_transaction", APISet.WALLET | APISet.TXN)]
        public static async Task<object> CreateTransaction(CreateTransactionParams _params)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (_params == null)
                    return new RPCError("check integrity parameters was null");

                if (_params.Address == null || _params.Address == "")
                    return new RPCError("one of the following must be set: Address, Label");

                var wallet = SQLiteWallet.Wallets.Values.Where(x => x.Accounts.Where(y => y.Address == _params.Address).Any()).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find address {_params.Address}");

                bool _raw = _params.Raw.HasValue ? _params.Raw.Value : false;
                bool _relay = _params.Relay.HasValue ? _params.Relay.Value : false;

                if (_params.Params == null || _params.Params.Count == 0) return new RPCError("create transaction expects at least one destination");

                var _to = _params.Params.Select(x => x.To).ToArray();
                var _amount = _params.Params.Select(x => x.Amount).ToArray();

                FullTransaction tx;

                try
                {
                    tx = await wallet.DoCreateTransaction(_params.Address, _to, _amount, true);
                }
                catch (Exception ex)
                {
                    return new RPCError(-1, "failed to create transaction", ex.Message);
                }

                var _rv = new CreateTransactionRV
                {
                    Txid = tx.Hash().ToHex(),
                    Tx = tx
                };

                if (_raw)
                {
                    _rv.Raw = Printable.Hexify(tx.Serialize());
                }

                if (_relay)
                {
                    var _exc = Daemon.TXPool.GetTXPool().CheckTx(tx);
                    if (_exc != null)
                    {
                        _rv.Error = _exc.Message;

                        return _rv;
                    }
                    else if (Network.Handler.GetHandler().State == Network.PeerState.Startup)
                    {
                        _rv.Error = $"network not available during daemon startup";

                        return _rv;
                    }

                    _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));
                }

                return _rv;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CreateTransaction failed: {ex.Message}", ex);

                return new RPCError($"Could not create transaction");
            }
        }
    }
}
