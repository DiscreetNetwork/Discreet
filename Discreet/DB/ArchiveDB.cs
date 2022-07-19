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
    public class ArchiveDB
    {
        public static ColumnFamilyHandle Txs;
        public static ColumnFamilyHandle TxIndices;
        public static ColumnFamilyHandle BlockHeights;
        public static ColumnFamilyHandle Blocks;
        public static ColumnFamilyHandle BlockHeaders;
        public static ColumnFamilyHandle BlockCache;
        public static ColumnFamilyHandle Meta;

        public const string TXS = "txs";
        public const string TX_INDICES = "tx_indices";
        public const string BLOCK_HEIGHTS = "block_heights";
        public const string BLOCKS = "blocks";
        public const string BLOCK_HEADERS = "block_headers";
        public const string BLOCK_CACHE = "block_cache";
        public const string META = "meta";

        public static byte[] ZEROKEY = new byte[8];

        private RocksDb archivedb;

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        private U64 indexer_tx = new U64(0);
        private L64 height = new L64(-1);

        public bool MetaExists()
        {
            try
            {
                return archivedb.Get(Encoding.ASCII.GetBytes("meta"), cf: Meta) != null;
            }
            catch
            {
                return false;
            }
        }

        public ArchiveDB(string path)
        {
            try
            {
                if (File.Exists(path)) throw new Exception("ArchiveDB: expects a valid directory path, not a file");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5);

                var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(TXS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(TX_INDICES, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_HEIGHTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCKS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_CACHE, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(BLOCK_HEADERS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(META, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                };

                archivedb = RocksDb.Open(options, path, _colFamilies);

                Txs = archivedb.GetColumnFamily(TXS);
                TxIndices = archivedb.GetColumnFamily(TX_INDICES);
                BlockHeights = archivedb.GetColumnFamily(BLOCK_HEIGHTS);
                Blocks = archivedb.GetColumnFamily(BLOCKS);
                BlockCache = archivedb.GetColumnFamily(BLOCK_CACHE);
                BlockHeaders = archivedb.GetColumnFamily(BLOCK_HEADERS);
                Meta = archivedb.GetColumnFamily(META);

                if (!MetaExists())
                {
                    /* completely empty and has just been created */
                    archivedb.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                    archivedb.Put(Encoding.ASCII.GetBytes("indexer_tx"), Serialization.UInt64(indexer_tx.Value), cf: Meta);
                    archivedb.Put(Encoding.ASCII.GetBytes("height"), Serialization.Int64(height.Value), cf: Meta);
                }
                else
                {
                    var result = archivedb.Get(Encoding.ASCII.GetBytes("indexer_tx"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"ArchiveDB: Fatal error: could not get indexer_tx");
                    }

                    result = archivedb.Get(Encoding.ASCII.GetBytes("height"), cf: Meta);

                    if (result == null)
                    {
                        throw new Exception($"ArchiveDB: Fatal error: could not get height");
                    }

                    height.Value = Serialization.GetInt64(result, 0);
                }

                folder = path;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Fatal($"ArchiveDB: failed to create the database: {ex}");
            }
        }


    }
}