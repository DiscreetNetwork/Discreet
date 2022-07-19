using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using RocksDbSharp;

namespace Discreet.DB
{
    public class StateDB
    {
        public static ColumnFamilyHandle SpentKeys;
        public static ColumnFamilyHandle Outputs;
        public static ColumnFamilyHandle OutputIndices;
        public static ColumnFamilyHandle PubOutputs;
        public static ColumnFamilyHandle Meta;

        public const string SPENT_KEYS = "spent_keys";
        public const string OUTPUTS = "outputs";
        public const string OUTPUT_INDICES = "output_indices";
        public const string PUB_OUTPUTS = "pub_outputs";
        public const string META = "meta";

        public static byte[] ZEROKEY = new byte[8];

        private RocksDb rdb;

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U32 indexer_output = new U32(0);

        private L64 height = new L64(-1);

        public bool MetaExists()
        {
            try
            {
                return rdb.Get(Encoding.ASCII.GetBytes("meta"), cf: Meta) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates the StateDB at the specified path.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="Exception"></exception>
        public StateDB(string path)
        {
            try
            {
                if (File.Exists(path)) throw new Exception("StateDB: expects a valid directory path, not a file");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5);

                var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(SPENT_KEYS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(OUTPUTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(OUTPUT_INDICES, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(PUB_OUTPUTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(META, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                };

                rdb = RocksDb.Open(options, path, _colFamilies);

                SpentKeys = rdb.GetColumnFamily(SPENT_KEYS);
                Outputs = rdb.GetColumnFamily(OUTPUTS);
                OutputIndices = rdb.GetColumnFamily(OUTPUT_INDICES);
                PubOutputs = rdb.GetColumnFamily(PUB_OUTPUTS);
                Meta = rdb.GetColumnFamily(META);

                if (!MetaExists())
                {
                    rdb.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                    rdb.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
                    rdb.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
                }
                else
                {
                    var result = rdb.Get(Encoding.ASCII.GetBytes("indexer_output"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"StateDB: Fatal error: could not get indexer_output");
                    }

                    indexer_output.Value = Serialization.GetUInt32(result, 0);

                    result = rdb.Get(Encoding.ASCII.GetBytes("height"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"StateDB: Fatal error: could not get height");
                    }

                    height.Value = Serialization.GetInt64(result, 0);
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Fatal($"StateDB: failed to create the database: {ex}");
            }
        }

        public void AddBlock(Block blk)
        {
            foreach (var tx in blk.Transactions)
            {
                uint[] outputIndices = new uint[tx.NumPOutputs];
                for (int i = 0; i < tx.NumPOutputs; i++)
                {
                    tx.POutputs[i].TransactionSrc = tx.TxID;
                    outputIndices[i] = AddOutput(tx.POutputs[i]);
                }

                byte[] uintArr = Serialization.UInt32Array(outputIndices);
                rdb.Put(tx.TxID.Bytes, uintArr, cf: OutputIndices);

                for (int i = 0; i < tx.NumPInputs; i++)
                {
                    rdb.Put(tx.PInputs[i].KeyImage.bytes, ZEROKEY, cf: SpentKeys);
                }

                for (int i = 0; i < tx.NumTInputs; i++)
                {
                    rdb.Remove(tx.TInputs[i].Serialize(), cf: PubOutputs);
                }

                for (int i = 0; i < tx.NumTOutputs; i++)
                {
                    tx.TOutputs[i].TransactionSrc = tx.TxID;
                    rdb.Put(new Coin.Transparent.TXInput { TxSrc = tx.TOutputs[i].TransactionSrc, Offset = (byte)i }.Serialize(), tx.TOutputs[i].Serialize(), cf: PubOutputs);
                }
            }

            lock (indexer_output)
            {
                rdb.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
            }
        }

        public uint AddOutput(TXOutput output)
        {
            uint outputIndex;

            lock (indexer_output)
            {
                indexer_output.Value++;
                outputIndex = indexer_output.Value;
            }

            rdb.Put(Serialization.UInt32(outputIndex), output.Serialize(), cf: Outputs);

            return outputIndex;
        }

        public bool CheckSpentKey(Cipher.Key j)
        {
            bool rv = rdb.Get(j.bytes, cf: SpentKeys) == null;
            return rv && !Daemon.TXPool.GetTXPool().ContainsSpentKey(j);
        }

        public bool CheckSpentKeyBlock(Cipher.Key j)
        {
            return rdb.Get(j.bytes, cf: SpentKeys) == null;
        }
    }
}