using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.RPC.Common;
using Discreet.Coin;
using Discreet.Common;
using Discreet.Cipher;
using System.Text.Json;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;

namespace Discreet.RPC.Endpoints
{
    public static class ReadEndpoints
    {
        public class GetBlockCountRV
        {
            public long Height { get; set; }
            public string Status { get; set; }
            public bool Untrusted { get; set; }
            public bool Synced { get; set; }

            public GetBlockCountRV() { }
        }

        [RPCEndpoint("get_block_count", APISet.READ)]
        public static GetBlockCountRV GetBlockCount()
        {
            try
            {
                var _handler = Network.Handler.GetHandler();

                GetBlockCountRV _rv = new GetBlockCountRV
                {
                    Height = DB.DataView.GetView().GetChainHeight(),
                    Synced = _handler.State == Network.PeerState.Normal,
                    Untrusted = false
                };

                if (_handler.State != Network.PeerState.Normal && _handler.State != Network.PeerState.Startup)
                {
                    var _versions = Network.MessageCache.GetMessageCache().Versions.Values;

                    foreach (var _version in _versions)
                    {
                        if (_version.Height > _rv.Height)
                        {
                            _rv.Untrusted = true;
                            _rv.Height = _version.Height;
                        }
                    }
                }

                _rv.Status = "OK";

                return _rv;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBlockCount failed: {ex.Message}", ex);

                return new GetBlockCountRV
                {
                    Height = 0,
                    Synced = false,
                    Untrusted = false,
                    Status = "An internal error was encountered"
                };
            }
        }

        [RPCEndpoint("get_block_hash_by_height", APISet.READ)]
        public static string GetBlockHashByHeight(long height)
        {
            try
            {
                return DB.DataView.GetView().GetBlockHeader(height).BlockHash.ToHex();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBlockHashByHeight failed: {ex.Message}", ex);

                return "";
            }
        }

        [RPCEndpoint("get_block_header_by_height", APISet.READ)]
        public static object GetBlockHeaderByHeight(long height)
        {
            try
            {
                return DB.DataView.GetView().GetBlockHeader(height).ToReadable();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBlockHeaderByHeight failed: {ex.Message}", ex);

                return new RPCError($"Could not get header at height {height}");
            }
        }

        [RPCEndpoint("get_block_headers", APISet.READ)]
        public static object GetBlockHeaders(long startHeight, long endHeight)
        {
            var dataView = DB.DataView.GetView();

            List<Readable.BlockHeader> headers = new();

            for (long height = startHeight; height <= endHeight; height++)
            {
                try
                {
                    headers.Add((Readable.BlockHeader)dataView.GetBlockHeader(height).ToReadable());
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"RPC call to GetBlockHeaders failed: {ex.Message}", ex);

                    return new RPCError(-1, $"Could not get header at height {height}", headers);
                }
            }

            return headers;
        }

        [RPCEndpoint("get_block", APISet.READ)]
        public static object GetBlock(object _jsonElement)
        {
            try
            {
                var _kind = ((JsonElement)_jsonElement).ValueKind;

                if (_kind == JsonValueKind.String)
                {
                    string hash = ((JsonElement)_jsonElement).GetString();

                    if (!Printable.IsHex(hash) || hash.Length != 64) return new RPCError($"Malformed or invalid block hash {hash}");

                    return DB.DataView.GetView().GetBlock(SHA256.FromHex(hash)).ToReadable();
                }
                else if (_kind == JsonValueKind.Number)
                {
                    long height = ((JsonElement)_jsonElement).GetInt64();

                    return DB.DataView.GetView().GetBlock(height).ToReadable();
                }
                else
                {
                    return new RPCError($"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBlock failed: {ex.Message}", ex);

                return new RPCError($"Could not get block with parameter {((JsonElement)_jsonElement).GetRawText()}");
            }
        }

        [RPCEndpoint("get_block_height", APISet.READ)]
        public static object GetBlockHeight(string hash)
        {
            try
            {
                return DB.DataView.GetView().GetBlockHeight(SHA256.FromHex(hash));
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBlockHeight failed: {ex.Message}", ex);

                return new RPCError($"Could not get block height with hash {hash}");
            }
        }

        [RPCEndpoint("get_blocks", APISet.READ)]
        public static object GetBlocks(object[] idxs)
        {
            if (idxs == null) return new RPCError(-1, $"No arguments given", Array.Empty<object>());

            List<Readable.Block> blocks = new();

            foreach (object _jsonElement in idxs)
            {
                try
                {
                    var _kind = ((JsonElement)_jsonElement).ValueKind;

                    if (_kind == JsonValueKind.String)
                    {
                        string hash = ((JsonElement)_jsonElement).GetString();

                        if (!Printable.IsHex(hash) || hash.Length != 64) return new RPCError($"Malformed or invalid block hash {hash}");

                        blocks.Add((Readable.Block)DB.DataView.GetView().GetBlock(SHA256.FromHex(hash)).ToReadable());
                    }
                    else if (_kind == JsonValueKind.Number)
                    {
                        long height = ((JsonElement)_jsonElement).GetInt64();

                        blocks.Add((Readable.Block)DB.DataView.GetView().GetBlock(height).ToReadable());
                    }
                    else
                    {
                        return new RPCError(-1, $"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}", blocks);
                    }
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"RPC call to GetBlocks failed: {ex.Message}", ex);

                    return new RPCError(-1, $"Could not get block with parameter {((JsonElement)_jsonElement).GetRawText()}", blocks);
                }
            }

            return blocks;
        }

        [RPCEndpoint("get_outputs", APISet.READ)]
        public static object GetOutputs(object[] idxs)
        {
            if (idxs == null) return new RPCError(-1, $"No arguments given", Array.Empty<object>());

            List<object> outputs = new();

            var dataView = DB.DataView.GetView();

            foreach (object _jsonElement in idxs)
            {
                try
                {
                    var _kind = ((JsonElement)_jsonElement).ValueKind;

                    if (_kind == JsonValueKind.String)
                    {
                        string hash = ((JsonElement)_jsonElement).GetString();

                        if (!Printable.IsHex(hash) || hash.Length != 64) return new RPCError($"Malformed or invalid block or tx hash {hash}");

                        try
                        {
                            var block = dataView.GetBlock(SHA256.FromHex(hash));

                            foreach (var _tx in block.Transactions)
                            {
                                var tx = (Readable.FullTransaction)_tx.ToReadable();

                                if (tx.TOutputs != null)
                                {
                                    foreach (var toutput in tx.TOutputs)
                                    {
                                        toutput.TransactionSrc = tx.TxID;
                                    }

                                    outputs.AddRange(tx.TOutputs);
                                }

                                if (tx.POutputs != null)
                                {
                                    foreach (var poutput in tx.POutputs)
                                    {
                                        poutput.TransactionSrc = tx.TxID;
                                    }

                                    outputs.AddRange(tx.POutputs);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            var tx = (Readable.FullTransaction)dataView.GetTransaction(SHA256.FromHex(hash)).ToReadable();

                            if (tx.TOutputs != null)
                            {
                                foreach (var toutput in tx.TOutputs)
                                {
                                    toutput.TransactionSrc = tx.TxID;
                                }

                                outputs.AddRange(tx.TOutputs);
                            }

                            if (tx.POutputs != null)
                            {
                                foreach (var poutput in tx.POutputs)
                                {
                                    poutput.TransactionSrc = tx.TxID;
                                }

                                outputs.AddRange(tx.POutputs);
                            }
                        }
                    }
                    else if (_kind == JsonValueKind.Number)
                    {
                        ulong id = ((JsonElement)_jsonElement).GetUInt64();
                        /* could be block height, tx id, or output index */
                        try
                        {
                            var block = dataView.GetBlock((long)id);

                            foreach (var _tx in block.Transactions)
                            {
                                var tx = (Readable.FullTransaction)_tx.ToReadable();

                                if (tx.TOutputs != null)
                                {
                                    foreach (var toutput in tx.TOutputs)
                                    {
                                        toutput.TransactionSrc = tx.TxID;
                                    }

                                    outputs.AddRange(tx.TOutputs);
                                }

                                if (tx.POutputs != null)
                                {
                                    foreach (var poutput in tx.POutputs)
                                    {
                                        poutput.TransactionSrc = tx.TxID;
                                    }

                                    outputs.AddRange(tx.POutputs);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var tx = (Readable.FullTransaction)dataView.GetTransaction(id).ToReadable();

                                if (tx.TOutputs != null)
                                {
                                    foreach (var toutput in tx.TOutputs)
                                    {
                                        toutput.TransactionSrc = tx.TxID;
                                    }

                                    outputs.AddRange(tx.TOutputs);
                                }

                                if (tx.POutputs != null)
                                {
                                    foreach (var poutput in tx.POutputs)
                                    {
                                        poutput.TransactionSrc = tx.TxID;
                                    }

                                    outputs.AddRange(tx.POutputs);
                                }
                            }
                            catch (Exception)
                            {
                                outputs.Add(dataView.GetOutput((uint)id).ToReadable());
                            }
                        }
                    }
                    else
                    {
                        return new RPCError(-1, $"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}", outputs);
                    }
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"RPC call to GetOutputs failed: {ex.Message}", ex);

                    return new RPCError(-1, $"Could not get outputs with parameter {((JsonElement)_jsonElement).GetRawText()}", outputs);
                }
            }

            return outputs;
        }

        [RPCEndpoint("get_output", APISet.READ)]
        public static object GetOutput(uint index)
        {
            try
            {
                return DB.DataView.GetView().GetOutput(index).ToReadable();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetOutput failed: {ex.Message}", ex);

                return new RPCError($"Could not get output at index {index}");
            }
        }

        [RPCEndpoint("get_private_outputs", APISet.READ)]
        public static object GetPrivateOutputs(List<uint> indices)
        {
            var rv = new List<object>();
            foreach (var index in indices)
            {
                try
                {
                    rv.Add(DB.DataView.GetView().GetOutput(index).ToReadable());
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"RPC call to GetOutput failed: {ex.Message}", ex);

                    return new RPCError($"Could not get output at index {index}");
                }
            }

            return rv;
        }

        [RPCEndpoint("get_transactions")]
        public static object GetTransactions(object[] idxs)
        {
            if (idxs == null) return new RPCError(-1, $"No arguments given", Array.Empty<object>());

            List<object> txs = new();

            foreach (object _jsonElement in idxs)
            {
                try
                {
                    var _kind = ((JsonElement)_jsonElement).ValueKind;

                    if (_kind == JsonValueKind.String)
                    {
                        string hash = ((JsonElement)_jsonElement).GetString();

                        if (!Printable.IsHex(hash) || hash.Length != 64) return new RPCError($"Malformed or invalid transaction hash or id {hash}");

                        var tx = DB.DataView.GetView().GetTransaction(SHA256.FromHex(hash));

                        txs.Add(tx.ToReadable());
                    }
                    else if (_kind == JsonValueKind.Number)
                    {
                        ulong id = ((JsonElement)_jsonElement).GetUInt64();

                        var tx = DB.DataView.GetView().GetTransaction(id);

                        txs.Add(tx.ToReadable());
                    }
                    else
                    {
                        return new RPCError(-1, $"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}", txs);
                    }
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"RPC call to GetTransactions failed: {ex.Message}", ex);

                    return new RPCError(-1, $"Could not get transaction with parameter {((JsonElement)_jsonElement).GetRawText()}", txs);
                }
            }

            return txs;
        }

        [RPCEndpoint("get_transaction", APISet.READ)]
        public static object GetTransaction(object _jsonElement)
        {
            try
            {
                var _kind = ((JsonElement)_jsonElement).ValueKind;

                if (_kind == JsonValueKind.String)
                {
                    string hash = ((JsonElement)_jsonElement).GetString();

                    if (!Printable.IsHex(hash) || hash.Length != 64) return new RPCError($"Malformed or invalid transaction hash or id {hash}");

                    var tx = DB.DataView.GetView().GetTransaction(SHA256.FromHex(hash));

                    return tx.ToReadable();
                }
                else if (_kind == JsonValueKind.Number)
                {
                    ulong id = ((JsonElement)_jsonElement).GetUInt64();

                    var tx = DB.DataView.GetView().GetTransaction(id);

                    return tx.ToReadable();
                }
                else
                {
                    return new RPCError($"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetTransaction failed: {ex.Message}", ex);

                return new RPCError($"Could not get transaction with parameter {((JsonElement)_jsonElement).GetRawText()}");
            }
        }

        public class GetTransactionCountRV
        {
            public ulong TransactionCount { get; set; }
            public string Status { get; set; }
            public bool Untrusted { get; set; }
            public bool Synced { get; set; }

            public GetTransactionCountRV() { }
        }

        [RPCEndpoint("get_transaction_count", APISet.READ)]
        public static GetTransactionCountRV GetTransactionCount()
        {
            try
            {
                var _handler = Network.Handler.GetHandler();

                GetTransactionCountRV _rv = new GetTransactionCountRV
                {
                    TransactionCount = DB.DataView.GetView().GetTransactionIndexer(),
                    Synced = _handler.State == Network.PeerState.Normal,
                    Untrusted = false,
                    Status = "OK"
                };

                return _rv;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetTransactionCount failed: {ex.Message}", ex);

                return new GetTransactionCountRV
                {
                    TransactionCount = 0,
                    Synced = false,
                    Untrusted = false,
                    Status = "An internal error was encountered"
                };
            }
        }

        public class GetPrivateOutputCountRV
        {
            public uint PrivateOutputCount { get; set; }
            public string Status { get; set; }
            public bool Untrusted { get; set; }
            public bool Synced { get; set; }

            public GetPrivateOutputCountRV() { }
        }

        [RPCEndpoint("get_private_output_count", APISet.READ)]
        public static GetPrivateOutputCountRV GetPrivateOutputCount()
        {
            try
            {
                var _handler = Network.Handler.GetHandler();

                GetPrivateOutputCountRV _rv = new GetPrivateOutputCountRV
                {
                    PrivateOutputCount = DB.DataView.GetView().GetOutputIndex(),
                    Synced = _handler.State == Network.PeerState.Normal,
                    Untrusted = false,
                    Status = "OK"
                };

                return _rv;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetPrivateOutputCount failed: {ex.Message}", ex);

                return new GetPrivateOutputCountRV
                {
                    PrivateOutputCount = 0,
                    Synced = false,
                    Untrusted = false,
                    Status = "An internal error was encountered"
                };
            }
        }

        public class GetBlockchainRV
        {
            public Readable.Block Head { get; set; }
            public List<object> TxPool { get; set; }
            public string Status { get; set; }
            public bool Synced { get; set; }
            

            public GetBlockchainRV() { }
        }

        [RPCEndpoint("get_blockchain", APISet.READ)]
        public static GetBlockchainRV GetBlockchain()
        {
            try
            {
                var db = DB.DataView.GetView();

                var _handler = Network.Handler.GetHandler();

                GetBlockchainRV _rv = new GetBlockchainRV
                {
                    Head = (Readable.Block)db.GetBlock(db.GetChainHeight()).ToReadable(),
                    TxPool = Daemon.TXPool.GetTXPool().GetTransactions().Select(x => x.ToReadable()).ToList(),
                    Synced = _handler.State == Network.PeerState.Normal,
                    Status = "OK"
                };

                return _rv;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBlockchain failed: {ex.Message}", ex);

                return new GetBlockchainRV
                {
                    Head = null,
                    TxPool = new List<object>(),
                    Synced = false,
                    Status = "An internal error was encountered"
                };
            }
        }

        [RPCEndpoint("get_last_blocks", APISet.READ)]
        public static object GetLastBlocks(long num)
        {
            if (num <= 0) return new RPCError($"parameter {num} is zero or negative");

            var dataView = DB.DataView.GetView();

            var height = dataView.GetChainHeight();

            List<Readable.Block> blocks = new();

            for (long i = height; i > height - num; i--)
            {
                try
                {
                    blocks.Add((Readable.Block)dataView.GetBlock(i).ToReadable());
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"RPC call to GetLastBlocks failed: {ex.Message}", ex);

                    return new RPCError(-1, $"Could not get block with parameter {i}", blocks);
                }
            }

            return blocks;
        }

        [RPCEndpoint("get_transaction_pool", APISet.READ)]
        public static object GetTransactionPool()
        {
            try
            {
                return Daemon.TXPool.GetTXPool().GetTransactions().Select(x => x.ToReadable()).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetTransactionPool failed: {ex.Message}", ex);

                return new RPCError("Could not get transaction pool");
            }
        }

        [RPCEndpoint("get_pub_output", APISet.READ)]
        public static object GetPubOutput(TTXInput input)
        {
            if (input == null) return new RPCError(-1, $"No arguments given", Array.Empty<object>());

            try
            {
                return (Readable.Transparent.TXOutput)DB.DataView.GetView().MustGetPubOutput(input).ToReadable();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetPubOutput failed: {ex.Message}", ex);

                return new RPCError("Could not get pub output");
            }
        }

        [RPCEndpoint("get_pub_outputs", APISet.READ)]
        public static object GetPubOutputs(TTXInput[] inputs)
        {
            if (inputs == null) return new RPCError(-1, $"No arguments given", Array.Empty<object>());

            try
            {
                List<Readable.Transparent.TXOutput> outputs = new();
                foreach (var input in inputs)
                {
                    outputs.Add((Readable.Transparent.TXOutput)DB.DataView.GetView().MustGetPubOutput(input).ToReadable());
                }

                return outputs;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetPubOutputs failed: {ex.Message}", ex);

                return new RPCError("Could not get pub outputs");
            }
        }

        [RPCEndpoint("get_raw_transaction", APISet.READ)]
        public static object GetRawTransaction(object _jsonElement)
        {
            try
            {
                var _kind = ((JsonElement)_jsonElement).ValueKind;

                if (_kind == JsonValueKind.String)
                {
                    string hash = ((JsonElement)_jsonElement).GetString();

                    if (!Printable.IsHex(hash) || hash.Length != 64) return new RPCError($"Malformed or invalid transaction hash or id {hash}");

                    return Printable.Hexify(DB.DataView.GetView().GetTransaction(SHA256.FromHex(hash)).Serialize());
                }
                else if (_kind == JsonValueKind.Number)
                {
                    ulong id = ((JsonElement)_jsonElement).GetUInt64();

                    return Printable.Hexify(DB.DataView.GetView().GetTransaction(id).Serialize());
                }
                else
                {
                    return new RPCError($"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetRawTransaction failed: {ex.Message}", ex);

                return new RPCError($"Could not get transaction with parameter {((JsonElement)_jsonElement).GetRawText()}");
            }
        }

        [RPCEndpoint("verify_address", APISet.READ)]
        public static object VerifyAddress(string addr)
        {
            try
            {
                if (addr.Length == 95)
                {
                    var _addr = new StealthAddress(addr);

                    return _addr.Verify() == null ? "OK" : new RPCError($"stealth address {addr} is invalid");
                }
                else
                {
                    var _addr = new TAddress(addr);

                    return _addr.Verify() == null ? "OK" : new RPCError($"transparent address {addr} is invalid");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to VerifyAddress failed: {ex.Message}", ex);

                return new RPCError($"unknown type address {addr} is malformed");
            }
        }
    }
}
