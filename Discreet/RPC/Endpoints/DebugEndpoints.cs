using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC;
using Discreet.RPC.Common;
using Discreet.Wallets;
using Discreet.Network;
using Discreet.Coin;

namespace Discreet.RPC.Endpoints
{
    public static class DebugEndpoints
    {
        [RPCEndpoint("dbg_faucet_stealth")]
        public static object DbgFaucetStealthPayout(string address, ulong amount)
        {
            try
            {
                var _visor = Handler.GetHandler().daemon;

                if (_visor.IsMasternode)
                {
                    var wallet = _visor.wallets.Where(x => x.Label == "DBG_MASTERNODE").FirstOrDefault();

                    if (wallet == null) return new RPCError("fatal error occurred. Masternode does not have a faucet.");

                    var tx = wallet.Addresses[0].CreateTransaction(new StealthAddress(address), amount).ToFull();

                    _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));

                    return tx.Hash().ToHex();
                }
                else
                {
                    return new RPCError("you are not a masternode!");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }

        public class DbgFaucetTransparentRV
        {
            public Readable.FullTransaction Tx { get; set; }
            public string Txid { get; set; }
            public string Verify { get; set; }
        }

        public class DbgCheckUtxoRV
        {
            public string UXKey { get; set; }
            public string UKSecKey { get; set; }
            public string LinkingTag { get; set; }
            public string TransactionSrc { get; set; }
            public string TransactionKey { get; set; }
            public ulong DecodedAmount { get; set; }
        }

        [RPCEndpoint("dbg_check_utxos")]
        public static object DbgCheckUtxos(string address)
        {
            try
            {
                var _visor = Handler.GetHandler().daemon;

                var wal = _visor.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wal == null)
                {
                    return new RPCError($"could not find address {address}");
                }

                var addr = wal.Addresses.Where(x => x.Address == address).FirstOrDefault();

                return addr.UTXOs.Select(x => new DbgCheckUtxoRV { DecodedAmount = x.DecodedAmount, UXKey = x.UXKey.ToHex(), LinkingTag = x.LinkingTag.ToHex(), TransactionKey = x.TransactionKey.ToHex(), TransactionSrc = x.TransactionSrc.ToHex(), UKSecKey = x.UXSecKey.ToHex() }).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }

        [RPCEndpoint("dbg_check_db_utxos")]
        public static object DbgCheckDbUtxos(string address, int start, int end)
        {
            List<string> utxos = new();

            try
            {
                var _visor = Handler.GetHandler().daemon;

                var wal = _visor.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wal == null)
                {
                    return new RPCError($"could not find address {address}");
                }

                var addr = wal.Addresses.Where(x => x.Address == address).FirstOrDefault();

                for (int i = start; i <= end; i++)
                {
                    var x = WalletDB.GetDB().GetWalletOutput(i);

                    if (x.Type == UTXOType.PRIVATE)
                    {
                        utxos.Add(x.LinkingTag.ToHex());
                    }
                }

                return utxos;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex}");

                return new RPCError(-1, $"Could not fulfill faucet request", utxos);
            }
        }

        [RPCEndpoint("dbg_faucet_transparent")]
        public static object DbgFaucetTransparentPayout(string address, ulong amount)
        {
            try
            {
                var _visor = Handler.GetHandler().daemon;

                if (_visor.IsMasternode)
                {
                    var wallet = _visor.wallets.Where(x => x.Label == "DBG_MASTERNODE").FirstOrDefault();

                    if (wallet == null) return new RPCError("fatal error occurred. Masternode does not have a faucet.");

                    var tx = wallet.Addresses[0].CreateTransaction(new IAddress[] { new TAddress(address) }, new ulong[] { amount }).ToFull();

                    var verify = tx.Verify();
                    string _verify = "";

                    if (verify == null)
                    {
                        _verify = "all good!";
                    }
                    else
                    {
                        _verify = verify.Message;
                    }

                    _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));

                    return new DbgFaucetTransparentRV
                    {
                        Tx = (Readable.FullTransaction)tx.ToReadable(),
                        Txid = tx.Hash().ToHex(),
                        Verify = _verify
                    };
                }
                else
                {
                    return new RPCError("you are not a masternode!");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex}");

                return new RPCError($"Could not fulfill faucet request");
            }
        }
    }
}
