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
                    Height = DB.DisDB.GetDB().GetChainHeight(),
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
                Visor.Logger.Error($"RPC call to GetBlockCount failed: {ex.Message}");

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
                return DB.DisDB.GetDB().GetBlockHeader(height).BlockHash.ToHex();
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlockHashByHeight failed: {ex.Message}");

                return "";
            }
        }

        [RPCEndpoint("get_block_header_by_height", APISet.READ)]
        public static object GetBlockHeaderByHeight(long height)
        {
            try
            {
                return DB.DisDB.GetDB().GetBlockHeader(height).ToReadable();
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlockHeaderByHeight failed: {ex.Message}");

                return new RPCError($"Could not get header at height {height}");
            }
        }

        [RPCEndpoint("get_block_headers_range", APISet.READ)]
        public static object GetBlockHeadersRange(long startHeight, long endHeight)
        {
            var db = DB.DisDB.GetDB();

            List<Readable.BlockHeader> headers = new();

            for (long height = startHeight; height <= endHeight; height++)
            {
                try
                {
                    headers.Add((Readable.BlockHeader)db.GetBlockHeader(height).ToReadable());
                }
                catch (Exception ex)
                {
                    Visor.Logger.Error($"RPC call to GetBlockHeadersRange failed: {ex.Message}");

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

                    return DB.DisDB.GetDB().GetBlock(SHA256.FromHex(hash)).ToReadable();
                }
                else if (_kind == JsonValueKind.Number)
                {
                    long height = ((JsonElement)_jsonElement).GetInt64();

                    return DB.DisDB.GetDB().GetBlock(height).ToReadable();
                }
                else
                {
                    return new RPCError($"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlock failed: {ex.Message}");

                return new RPCError($"Could not get block with parameter {((JsonElement)_jsonElement).GetRawText()}");
            }
        }

        [RPCEndpoint("get_block_hash", APISet.READ)]
        public static object GetBlockHash(long height)
        {
            try
            {
                return DB.DisDB.GetDB().GetBlockHeader(height).BlockHash.ToHex();
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlockHash failed: {ex.Message}");

                return new RPCError($"Could not get block hash at height {height}");
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

                        blocks.Add((Readable.Block)DB.DisDB.GetDB().GetBlock(SHA256.FromHex(hash)).ToReadable());
                    }
                    else if (_kind == JsonValueKind.Number)
                    {
                        long height = ((JsonElement)_jsonElement).GetInt64();

                        blocks.Add((Readable.Block)DB.DisDB.GetDB().GetBlock(height).ToReadable());
                    }
                    else
                    {
                        return new RPCError(-1, $"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}", blocks);
                    }
                }
                catch (Exception ex)
                {
                    Visor.Logger.Error($"RPC call to GetBlocks failed: {ex.Message}");

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

            var db = DB.DisDB.GetDB();

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
                            var block = db.GetBlock(SHA256.FromHex(hash));

                            foreach (var _tx in block.Transactions)
                            {
                                var tx = (Readable.FullTransaction)_tx.ToReadable();

                                if (tx.POutputs != null) outputs.AddRange(tx.POutputs);
                                if (tx.TOutputs != null) outputs.AddRange(tx.TOutputs);
                            }
                        }
                        catch (Exception)
                        {
                            var tx = (Readable.FullTransaction)db.GetTransaction(SHA256.FromHex(hash)).ToReadable();

                            if (tx.POutputs != null) outputs.AddRange(tx.POutputs);
                            if (tx.TOutputs != null) outputs.AddRange(tx.TOutputs);
                        }
                    }
                    else if (_kind == JsonValueKind.Number)
                    {
                        ulong id = ((JsonElement)_jsonElement).GetUInt64();
                        /* could be block height, tx id, or output index */
                        try
                        {
                            var block = db.GetBlock((long)id);

                            foreach (var _tx in block.Transactions)
                            {
                                var tx = (Readable.FullTransaction)_tx.ToReadable();

                                if (tx.POutputs != null) outputs.AddRange(tx.POutputs);
                                if (tx.TOutputs != null) outputs.AddRange(tx.TOutputs);
                            }
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var tx = (Readable.FullTransaction)db.GetTransaction(id).ToReadable();

                                if (tx.POutputs != null) outputs.AddRange(tx.POutputs);
                                if (tx.TOutputs != null) outputs.AddRange(tx.TOutputs);
                            }
                            catch (Exception)
                            {
                                outputs.Add(db.GetOutput((uint)id));
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
                    Visor.Logger.Error($"RPC call to GetOutputs failed: {ex.Message}");

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
                return DB.DisDB.GetDB().GetOutput(index).ToReadable();
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetOutput failed: {ex.Message}");

                return new RPCError($"Could not get output at index {index}");
            }
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

                        var tx = DB.DisDB.GetDB().GetTransaction(SHA256.FromHex(hash));

                        txs.Add(tx.Version switch
                        {
                            0 => tx.ToCoinbase().ToReadable(),
                            1 or 2 => tx.ToPrivate().ToReadable(),
                            3 => tx.ToTransparent().ToReadable(),
                            4 => tx.ToMixed().ToReadable(),
                            _ => tx.ToReadable()
                        });
                    }
                    else if (_kind == JsonValueKind.Number)
                    {
                        ulong id = ((JsonElement)_jsonElement).GetUInt64();

                        var tx = DB.DisDB.GetDB().GetTransaction(id);

                        txs.Add(tx.Version switch
                        {
                            0 => tx.ToCoinbase().ToReadable(),
                            1 or 2 => tx.ToPrivate().ToReadable(),
                            3 => tx.ToTransparent().ToReadable(),
                            4 => tx.ToMixed().ToReadable(),
                            _ => tx.ToReadable()
                        });
                    }
                    else
                    {
                        return new RPCError(-1, $"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}", txs);
                    }
                }
                catch (Exception ex)
                {
                    Visor.Logger.Error($"RPC call to GetTransactions failed: {ex.Message}");

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

                    var tx = DB.DisDB.GetDB().GetTransaction(SHA256.FromHex(hash));

                    return tx.Version switch
                    {
                        0 => tx.ToCoinbase().ToReadable(),
                        1 or 2 => tx.ToPrivate().ToReadable(),
                        3 => tx.ToTransparent().ToReadable(),
                        4 => tx.ToMixed().ToReadable(),
                        _ => tx.ToReadable()
                    };
                }
                else if (_kind == JsonValueKind.Number)
                {
                    ulong id = ((JsonElement)_jsonElement).GetUInt64();

                    var tx = DB.DisDB.GetDB().GetTransaction(id);

                    return tx.Version switch
                    {
                        0 => tx.ToCoinbase().ToReadable(),
                        1 or 2 => tx.ToPrivate().ToReadable(),
                        3 => tx.ToTransparent().ToReadable(),
                        4 => tx.ToMixed().ToReadable(),
                        _ => tx.ToReadable()
                    };
                }
                else
                {
                    return new RPCError($"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetTransaction failed: {ex.Message}");

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
                    TransactionCount = DB.DisDB.GetDB().GetTransactionIndex(),
                    Synced = _handler.State != Network.PeerState.Normal,
                    Untrusted = false,
                    Status = "OK"
                };

                return _rv;
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetTransactionCount failed: {ex.Message}");

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
                    PrivateOutputCount = DB.DisDB.GetDB().GetOutputIndex(),
                    Synced = _handler.State != Network.PeerState.Normal,
                    Untrusted = false,
                    Status = "OK"
                };

                return _rv;
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetPrivateOutputCount failed: {ex.Message}");

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
                var db = DB.DisDB.GetDB();

                var _handler = Network.Handler.GetHandler();

                GetBlockchainRV _rv = new GetBlockchainRV
                {
                    Head = (Readable.Block)db.GetBlock(db.GetChainHeight()).ToReadable(),
                    TxPool = Visor.TXPool.GetTXPool().GetTransactions().Select(x => x.Version switch
                    {
                        0 => x.ToCoinbase().ToReadable(),
                        1 or 2 => x.ToPrivate().ToReadable(),
                        3 => x.ToTransparent().ToReadable(),
                        4 => x.ToMixed().ToReadable(),
                        _ => x.ToReadable()
                    }).ToList(),
                    Synced = _handler.State != Network.PeerState.Normal,
                    Status = "OK"
                };

                return _rv;
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetBlockchain failed: {ex.Message}");

                return new GetBlockchainRV
                {
                    Head = null,
                    TxPool = new List<object>(),
                    Synced = false,
                    Status = "An internal error was encountered"
                };
            }
        }

        [RPCEndpoint("get_last_blocks")]
        public static object GetLastBlocks(long num)
        {
            if (num <= 0) return new RPCError($"parameter {num} is zero or negative");

            var db = DB.DisDB.GetDB();

            var height = db.GetChainHeight();

            List<Readable.Block> blocks = new();

            for (long i = height; i > height - num; i--)
            {
                try
                {
                    blocks.Add((Readable.Block)db.GetBlock(i).ToReadable());
                }
                catch (Exception ex)
                {
                    Visor.Logger.Error($"RPC call to GetLastBlocks failed: {ex.Message}");

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
                return Visor.TXPool.GetTXPool().GetTransactions().Select(x => x.Version switch
                {
                    0 => x.ToCoinbase().ToReadable(),
                    1 or 2 => x.ToPrivate().ToReadable(),
                    3 => x.ToTransparent().ToReadable(),
                    4 => x.ToMixed().ToReadable(),
                    _ => x.ToReadable()
                }).ToList();
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetTransactionPool failed: {ex.Message}");

                return new RPCError("Could not get transaction pool");
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

                    return Printable.Hexify(DB.DisDB.GetDB().GetTransaction(SHA256.FromHex(hash)).Serialize());
                }
                else if (_kind == JsonValueKind.Number)
                {
                    ulong id = ((JsonElement)_jsonElement).GetUInt64();

                    return Printable.Hexify(DB.DisDB.GetDB().GetTransaction(id).Serialize());
                }
                else
                {
                    return new RPCError($"Malformed or invalid parameter {((JsonElement)_jsonElement).GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetRawTransaction failed: {ex.Message}");

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
                Visor.Logger.Error($"RPC call to VerifyAddress failed: {ex.Message}");

                return new RPCError($"unknown type address {addr} is malformed");
            }
        }
    }
}
