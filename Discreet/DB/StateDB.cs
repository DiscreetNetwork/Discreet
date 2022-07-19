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

        private RocksDb statedb;

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
                return statedb.Get(Encoding.ASCII.GetBytes("meta"), cf: Meta) != null;
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

                statedb = RocksDb.Open(options, path, _colFamilies);

                SpentKeys = statedb.GetColumnFamily(SPENT_KEYS);
                Outputs = statedb.GetColumnFamily(OUTPUTS);
                OutputIndices = statedb.GetColumnFamily(OUTPUT_INDICES);
                PubOutputs = statedb.GetColumnFamily(PUB_OUTPUTS);
                Meta = statedb.GetColumnFamily(META);

                if (!MetaExists())
                {
                    statedb.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                    statedb.Put(Encoding.ASCII.GetBytes("indexer_output"), Serialization.UInt32(indexer_output.Value), cf: Meta);
                    statedb.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
                }
                else
                {
                    var result = statedb.Get(Encoding.ASCII.GetBytes("indexer_output"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"StateDB: Fatal error: could not get indexer_output");
                    }

                    indexer_output.Value = Serialization.GetUInt32(result, 0);

                    result = statedb.Get(Encoding.ASCII.GetBytes("height"), cf: Meta);

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
    }
}