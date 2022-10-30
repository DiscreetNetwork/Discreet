using Discreet.Coin;
using Discreet.Daemon;
using Discreet.Network;
using Discreet.RPC.Common;
using Discreet.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discreet.RPC.Endpoints
{
    public static class DebugEndpoints
    {
        [RPCEndpoint("dbg_faucet_stealth")]
        public static object DbgFaucetStealthPayout(string address, ulong amount)
        {
            try
            {
                var _daemon = Handler.GetHandler().daemon;

                if (_daemon.IsMasternode)
                {
                    var wallet = _daemon.wallets.Where(x => x.Label == "DBG_MASTERNODE").FirstOrDefault();

                    if (wallet == null) return new RPCError("fatal error occurred. Masternode does not have a faucet.");

                    var tx = wallet.Addresses[0].CreateTransaction(new StealthAddress(address), amount).Item2.ToFull();

                    /* sanity check */
                    var err = Daemon.TXPool.GetTXPool().CheckTx(tx);
                    if (err != null)
                    {
                        Daemon.Logger.Error($"DbgFaucetStealth: failed to send tx: {err.Message}", err);
                        return new RPCError("failed to create transaction: " + err.Message);
                    }
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
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}", ex);

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
                var _daemon = Handler.GetHandler().daemon;

                var wal = _daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wal == null)
                {
                    return new RPCError($"could not find address {address}");
                }

                var addr = wal.Addresses.Where(x => x.Address == address).FirstOrDefault();

                return addr.UTXOs.Select(x => new DbgCheckUtxoRV { DecodedAmount = x.DecodedAmount, UXKey = x.UXKey.ToHex(), LinkingTag = x.LinkingTag.ToHex(), TransactionKey = x.TransactionKey.ToHex(), TransactionSrc = x.TransactionSrc.ToHex(), UKSecKey = x.UXSecKey.ToHex() }).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}", ex);

                return new RPCError($"Could not fulfill faucet request");
            }
        }

        [RPCEndpoint("dbg_check_db_utxos")]
        public static object DbgCheckDbUtxos(string address, int start, int end)
        {
            List<string> utxos = new();

            try
            {
                var _daemon = Handler.GetHandler().daemon;

                var wal = _daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

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
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}", ex);

                return new RPCError(-1, $"Could not fulfill faucet request", utxos);
            }
        }

        [RPCEndpoint("dbg_faucet_transparent")]
        public static object DbgFaucetTransparentPayout(string address, ulong amount)
        {
            try
            {
                var _daemon = Handler.GetHandler().daemon;

                if (_daemon.IsMasternode)
                {
                    var wallet = _daemon.wallets.Where(x => x.Label == "DBG_MASTERNODE").FirstOrDefault();

                    if (wallet == null) return new RPCError("fatal error occurred. Masternode does not have a faucet.");

                    var tx = wallet.Addresses[0].CreateTransaction(new IAddress[] { new TAddress(address) }, new ulong[] { amount }).ToFull();

                    var verify = Daemon.TXPool.GetTXPool().CheckTx(tx);
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
                Daemon.Logger.Error($"RPC call to DbgFaucet failed: {ex.Message}", ex);

                return new RPCError($"Could not fulfill faucet request");
            }
        }

        public class PeerObject
        {
            public System.Net.IPEndPoint Endpoint { get; set; }
            public System.Net.IPEndPoint Source { get; set; }
            public long LastSeen { get; set; }
            public long FirstSeen { get; set; }

            public bool InTried { get; set; }

            public int NumFailedConnectionAttempts { get; set; }

            public long LastSuccess { get; set; }
            public long LastAttempt { get; set; }

            // how many times this occurs in the NEW set
            public int RefCount { get; set; }

            public PeerObject(Network.Peerbloom.Peer p)
            {
                Endpoint = p.Endpoint;
                Source = p.Source;
                LastSeen = p.LastSeen;
                FirstSeen = p.FirstSeen;
                LastAttempt = p.LastAttempt;
                RefCount = p.RefCount;
                LastSuccess = p.LastSuccess;
                LastAttempt = p.LastAttempt;
                NumFailedConnectionAttempts = p.NumFailedConnectionAttempts;
                InTried = p.InTried;
            }
        }

        public class GetPeerlistRV
        {
            public int NumNew { get; set; }
            public int NumTried { get; set; }

            public List<PeerObject> Peers { get; set; }
        }

        [RPCEndpoint("dbg_get_peerlist")]
        public static object DbgGetPeerlist(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    var _network = Network.Peerbloom.Network.GetNetwork();
                    var _peers = _network.peerlist;

                    return new GetPeerlistRV { NumNew = _peers.NumNew, NumTried = _peers.NumTried, Peers = _peers.GetPeers().Select(p => new PeerObject(p)).ToList() };
                }
                else
                {
                    var _peers = new Network.Peerbloom.Peerlist(path);
                    return new GetPeerlistRV { NumNew = _peers.NumNew, NumTried = _peers.NumTried, Peers = _peers.GetPeers().Select(p => new PeerObject(p)).ToList() };
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to DbgGetPeerlist failed: {ex.Message}", ex);

                return new RPCError($"Could not fulfill request");
            }
        }

        [RPCEndpoint(endpoint_name: "dbg_send_message")]
        public static object SendMessage(string message)
        {
            _ = Network.Peerbloom.Network.GetNetwork().Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDMSG, new Network.Core.Packets.SendMessagePacket { MessageLen = (uint)Encoding.UTF8.GetBytes(message).Length, Message = message }));

            return true;
        }
    }
}
