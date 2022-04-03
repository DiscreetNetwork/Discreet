using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using RocksDbSharp;

namespace Discreet.Wallets
{
    public class WalletDB
    {
        private static WalletDB disdb;

        private static object disdb_lock = new object();

        public static WalletDB GetDB()
        {
            lock (disdb_lock)
            {
                if (disdb == null) Initialize();

                return disdb;
            }
        }

        public static void Initialize()
        {
            lock (disdb_lock)
            {
                if (disdb == null)
                {
                    disdb = new WalletDB(Daemon.DaemonConfig.GetConfig().WalletPath);
                }
            }
        }

        private static readonly object db_update_lock = new object();

        public static object DBLock { get { return db_update_lock; } }

        public static string UTXOS = "utxos";
        public static string STORAGE = "storage";
        public static string WALLETS = "wallets";
        public static string WALLET_ADDRESSES = "wallet_addresses";
        public static string WALLET_HEIGHTS = "wallet_heights";
        public static string ADDRESS_BOOK = "address_book";
        public static string TX_HISTORY = "tx_history";
        public static string META = "meta";

        public static ColumnFamilyHandle UTXOs;
        public static ColumnFamilyHandle Storage;
        public static ColumnFamilyHandle Wallets;
        public static ColumnFamilyHandle WalletAddresses;
        public static ColumnFamilyHandle WalletHeights;
        public static ColumnFamilyHandle AddressBook;
        public static ColumnFamilyHandle TxHistory;
        public static ColumnFamilyHandle Meta;

        private DB.U32 indexer_owned_outputs = new DB.U32(0);
        private DB.U32 indexer_wallet_addresses = new DB.U32(0);
        private DB.U32 indexer_tx_history = new DB.U32(0);

        public string Folder
        {
            get { return folder; }
        }

        private string folder;

        public static byte[] ZEROKEY = new byte[8];

        private RocksDb db;

        public bool MetaExists()
        {
            try
            {
                return db.Get(Encoding.ASCII.GetBytes("meta"), cf: Meta) != null;
            }
            catch
            {
                return false;
            }
        }

        public WalletDB(string path)
        {
            if(File.Exists(path)) throw new Exception("Discreet.DisDB: expects a valid directory path, not a file");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5);

            var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(UTXOS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(STORAGE, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(WALLETS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(WALLET_ADDRESSES, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(WALLET_HEIGHTS, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(ADDRESS_BOOK, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(TX_HISTORY, new ColumnFamilyOptions()),
                    new ColumnFamilies.Descriptor(META, new ColumnFamilyOptions())
                };

            db = RocksDb.Open(options, path, _colFamilies);

            UTXOs = db.GetColumnFamily(UTXOS);
            Storage = db.GetColumnFamily(STORAGE);
            Wallets = db.GetColumnFamily(WALLETS);
            WalletAddresses = db.GetColumnFamily(WALLET_ADDRESSES);
            WalletHeights = db.GetColumnFamily(WALLET_HEIGHTS);
            AddressBook = db.GetColumnFamily(ADDRESS_BOOK);
            TxHistory = db.GetColumnFamily(TX_HISTORY);
            Meta = db.GetColumnFamily(META);

            if (!MetaExists())
            {
                /* completely empty and has just been created */
                db.Put(Encoding.ASCII.GetBytes("meta"), ZEROKEY, cf: Meta);
                db.Put(Encoding.ASCII.GetBytes("indexer_owned_outputs"), Serialization.UInt64(indexer_owned_outputs.Value), cf: Meta);
                db.Put(Encoding.ASCII.GetBytes("indexer_wallet_addresses"), Serialization.UInt32(indexer_wallet_addresses.Value), cf: Meta);
                db.Put(Encoding.ASCII.GetBytes("indexer_tx_history"), Serialization.UInt32(indexer_tx_history.Value), cf: Meta);
            }
            else
            {
                var result = db.Get(Encoding.ASCII.GetBytes("indexer_owned_outputs"), cf: Meta);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB: Fatal error: could not get indexer_owned_outputs");
                }

                indexer_owned_outputs.Value = Serialization.GetUInt32(result, 0);

                result = db.Get(Encoding.ASCII.GetBytes("indexer_wallet_addresses"), cf: Meta);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB: Fatal error: could not get indexer_wallet_addresses");
                }

                indexer_wallet_addresses.Value = Serialization.GetUInt32(result, 0);

                result = db.Get(Encoding.ASCII.GetBytes("indexer_tx_history"), cf: Meta);

                if (result == null)
                {
                    throw new Exception($"Discreet.DisDB: Fatal error: could not get indexer_tx_history");
                }

                indexer_tx_history.Value = Serialization.GetUInt32(result, 0);
            }

            folder = path;
        }

        public void AddWallet(Wallet wallet)
        {
            if (TryGetWallet(wallet.Label) != null)
            {
                throw new Exception($"Discreet.DisDB.GetWalletOutput: database get exception: wallet with label {wallet.Label} already exists");
            }

            wallet.Addresses.ToList().ForEach(address => AddWalletAddress(address));

            UpdateWallet(wallet);
        }

        public void UpdateWallet(Wallet wallet)
        {
            db.Put(Encoding.UTF8.GetBytes(wallet.Label), wallet.Serialize(), cf: Wallets);

            SetWalletHeight(wallet.Label, wallet.LastSeenHeight);
        }

        public bool ContainsWallet(string label)
        {
            return db.Get(Encoding.UTF8.GetBytes(label), cf: Wallets) != null;
        }

        public Wallet GetWallet(string label)
        {
            var res = db.Get(Encoding.UTF8.GetBytes(label), cf: Wallets);

            if (res == null)
            {
                throw new Exception($"Discreet.DisDB.GetWalletOutput: database get exception: could not get wallet with label {label}");
            }

            Wallet wallet = new Wallet();
            wallet.Deserialize(new MemoryStream(res));
            wallet.LastSeenHeight = GetWalletHeight(wallet.Label);

            return wallet;
        }

        public Wallet TryGetWallet(string label)
        {
            var res = db.Get(Encoding.UTF8.GetBytes(label), cf: Wallets);

            if (res == null)
            {
                return null;
            }

            Wallet wallet = new Wallet();
            wallet.Deserialize(new MemoryStream(res));
            wallet.LastSeenHeight = GetWalletHeight(wallet.Label);

            return wallet;
        }

        public List<Wallet> GetWallets()
        {
            List<Wallet> wallets = new();

            var iterator = db.NewIterator(cf: Wallets);

            iterator.SeekToFirst();

            while (iterator.Valid())
            {
                Wallet wallet = new Wallet();
                wallet.Deserialize(new MemoryStream(iterator.Value()));
                wallet.LastSeenHeight = GetWalletHeight(wallet.Label);

                wallets.Add(wallet);

                iterator.Next();
            }

            return wallets;
        }

        public void AddWalletAddress(WalletAddress address)
        {
            lock (indexer_wallet_addresses)
            {
                address.DBIndex = (int)indexer_wallet_addresses.Value++;
            }

            db.Put(Serialization.Int32(address.DBIndex), address.Serialize(), cf: WalletAddresses);

            lock (indexer_wallet_addresses)
            {
                db.Put(Encoding.ASCII.GetBytes("indexer_wallet_addresses"), Serialization.UInt32(indexer_wallet_addresses.Value), cf: Meta);
            }
        }

        public void UpdateWalletAddress(WalletAddress address)
        {
            db.Put(Serialization.Int32(address.DBIndex), address.Serialize(), cf: WalletAddresses);
        }

        public void SetWalletHeight(string label, long height)
        {
            db.Put(Encoding.UTF8.GetBytes(label), Serialization.Int64(height), cf: WalletHeights);
        }

        public long GetWalletHeight(string label)
        {
            var res = db.Get(Encoding.UTF8.GetBytes(label), cf: WalletHeights);

            if (res == null)
            {
                throw new Exception($"Discreet.DisDB.GetWalletOutput: database get exception: could not get wallet height with label {label}");
            }

            return Serialization.GetInt64(res, 0);
        }

        public WalletAddress GetWalletAddress(int index)
        {
            var res = db.Get(Serialization.Int32(index), cf: WalletAddresses);

            if (res == null)
            {
                throw new Exception($"Discreet.DisDB.GetWalletOutput: database get exception: could not get wallet address at index {index}");
            }

            WalletAddress address = new WalletAddress();
            address.Deserialize(new MemoryStream(res));

            return address;
        }

        public uint GetTXOutputIndex(FullTransaction tx, int i)
        {
            return DB.DisDB.GetDB().GetOutputIndices(tx.Hash())[i];
        }

        public (int, UTXO) AddWalletOutput(WalletAddress addr, FullTransaction tx, int i, bool transparent)
        {
            return AddWalletOutput(addr, tx, i, transparent, false);
        }

        public (int, UTXO) AddWalletOutput(WalletAddress addr, FullTransaction tx, int i, bool transparent, bool coinbase)
        {
            UTXO utxo;

            if (tx.Version == 3)
            {
                tx.TOutputs[i].TransactionSrc = tx.Hash();
                utxo = new UTXO(tx.TOutputs[i]);
            }
            else if (tx.Version == 4)
            {
                if (transparent)
                {
                    tx.TOutputs[i].TransactionSrc = tx.Hash();
                    utxo = new UTXO(tx.TOutputs[i]);
                }
                else
                {
                    tx.POutputs[i].TransactionSrc = tx.Hash();
                    uint index = GetTXOutputIndex(tx, i);
                    utxo = new UTXO(addr, index, tx.POutputs[i], tx.ToMixed(), i, coinbase);
                }
            }
            else
            {
                tx.POutputs[i].TransactionSrc = tx.Hash();
                uint index = GetTXOutputIndex(tx, i);
                utxo = new UTXO(addr, index, tx.POutputs[i], tx.ToPrivate(), i, coinbase);
            }

            int outputIndex;

            lock (indexer_owned_outputs)
            {
                indexer_owned_outputs.Value++;
                outputIndex = (int)indexer_owned_outputs.Value;
            }

            utxo.OwnedIndex = outputIndex;

            db.Put(Serialization.Int32(outputIndex), utxo.Serialize(), cf: UTXOs);

            lock (indexer_owned_outputs)
            {
                db.Put(Encoding.ASCII.GetBytes("indexer_owned_outputs"), Serialization.UInt32(indexer_owned_outputs.Value), cf: Meta);
            }

            return (outputIndex, utxo);
        }

        public UTXO GetWalletOutput(int index)
        {
            var result = db.Get(Serialization.Int32(index), cf: UTXOs);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetWalletOutput: database get exception: could not get output at index {index}");
            }

            UTXO utxo = new UTXO();
            utxo.Deserialize(result);

            return utxo;
        }

        public void AddTxToHistory(WalletTx tx)
        {
            lock (indexer_tx_history)
            {
                indexer_tx_history.Value++;
                tx.Index = (int)indexer_tx_history.Value;
            }

            db.Put(Serialization.Int32(tx.Index), tx.EncryptedString, cf: TxHistory);
        }

        public WalletTx GetTxFromHistory(WalletAddress address, int index)
        {
            var result = db.Get(Coin.Serialization.Int32(index), cf: TxHistory);

            if (result == null)
            {
                throw new Exception($"Discreet.DisDB.GetTxFromHistory: database get exception: could not get tx from history at index {index}");
            }

            WalletTx tx = new WalletTx(address, result);

            return tx;
        }

        public byte[] KVGet(byte[] key)
        {
            return db.Get(key, cf: Storage);
        }

        public byte[] KVPut(byte[] key, byte[] value)
        {
            var _rv = db.Get(key, cf: Storage);

            lock (DBLock)
            {
                db.Put(key, value, cf: Storage);
            }

            return _rv ?? new byte[0];
        }

        public byte[] KVDel(byte[] key)
        {
            var _rv = db.Get(key, cf: Storage);

            lock (DBLock)
            {
                db.Remove(key, cf: Storage);
            }

            return _rv ?? new byte[0];
        }

        public Dictionary<byte[], byte[]> KVAll()
        {
            Dictionary<byte[], byte[]> _rv = new();

            var iterator = db.NewIterator(cf: Storage);

            iterator.SeekToFirst();

            while (iterator.Valid())
            {
                _rv[iterator.Key()] = iterator.Value();

                iterator.Next();
            }

            return _rv;
        }
    }
}
