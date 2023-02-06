using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Daemon;
using Discreet.RPC.Common;
using Discreet.RPC.Converters;
using Discreet.Wallets.Comparers;
using Discreet.DB;
using Discreet.Wallets.Models;
using Discreet.Wallets.Services;
using Discreet.Wallets.Utilities;
using Discreet.Wallets.Utilities.Converters;
using System.Threading;
using System.IO;

namespace Discreet.Wallets
{
    public class SQLiteWallet
    {
        public string CoinName => "Discreet";
        public string Version => "0.2";

        public static ConcurrentDictionary<string, SQLiteWallet> Wallets = new();
        private static readonly string directory;
        private static CancellationTokenSource cancellationTokenSource = new();
        private static List<JsonConverter> converters = new List<JsonConverter>(new JsonConverter[]
                {
                    new BytesConverter(),
                    new IAddressConverter(),
                    new IPEndPointConverter(),
                    new KeccakConverter(),
                    new KeyConverter(),
                    new RIPEMD160Converter(),
                    new SHA256Converter(),
                    new SHA512Converter(),
                    new SignatureConverter(),
                    new StealthAddressConverter(),
                    new TAddressConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new StringConverter(),
                    new TXInputConverter(),
                    new SQLiteWalletConverter(),
                });

        static SQLiteWallet()
        {
            directory = DaemonConfig.GetConfig().WalletPath;
        }

        private readonly object db_lock = new();
        private byte[] EncryptedEntropy;
        private uint EntropyLen;
        private SHA256 EntropyChecksum;
        private long lastSeenHeight = -1;
        private readonly object req_lock = new();
        private bool lockRequested = false;
        private bool unlockRequested = false;

        public string Label;
        public string Path;

        public byte[] Entropy;
        public bool Encrypted { get; set; }
        public bool IsEncrypted { get; set; }
        public List<Account> Accounts { get; set; }
        public ulong Timestamp { get; set; }

        private List<IWalletService> services = new();
        private SemaphoreSlim requestSemaphore = new(1, 1);
        private WalletFundsService syncingService;

        internal WalletFundsService SyncingService { get { return syncingService; } }

        private void Register()
        {
            Wallets[Label] = this;
            _ = Task.Run(async () => await Start(cancellationTokenSource.Token)).ConfigureAwait(false);
        }

        public void RegisterService(IWalletService service)
        {
            lock (services)
            {
                services.Add(service);
            }
        }

        private void CleanServices()
        {
            lock (services) 
            { 
                var toRemove = services.Where(x => x.Completed);
                foreach (var service in toRemove)
                {
                    services.Remove(service);
                }
            }
        }

        public void RequestLock()
        {
            lock (req_lock)
            {
                lockRequested = true;
            }
        }

        public bool Unlock(string passphrase)
        {
            lock (req_lock)
            {
                if (!IsEncrypted) return true;

                try
                {
                    Decrypt(passphrase);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private SQLiteWallet(CreateWalletParameters parameters)
        {
            Timestamp = (ulong)DateTime.Now.Ticks;
            Encrypted = parameters.Encrypted;
            Label = parameters.Label;
            
            if (parameters.Mnemonic != null)
            {
                Entropy = new Discreet.Cipher.Mnemonics.Mnemonic(parameters.Mnemonic).GetEntropy();
            }
            else
            {
                Entropy = Randomness.Random(parameters.BIP39 == 12 ? 16U : 32U);
            }

            EntropyLen = (uint)Entropy.Length;
            if (Encrypted)
            {
                EncryptedEntropy = Entropy.EncryptEntropy(parameters.Passphrase, out var chksum);
                EntropyChecksum = chksum;
            }
            else
            {
                EncryptedEntropy = Entropy;
            }

            Accounts = new List<Account>();

            if (parameters.StealthAddressNames != null && parameters.StealthAddressNames.Count > 0)
            {
                parameters.StealthAddressNames.ForEach(name =>
                {
                    CreateAccount(
                        new CreateAccountParameters(name)
                        .SetDeterministic()
                        .Stealth()
                        .SkipScan()
                        .NoSave());
                });
            }
            else
            {
                for(int i = 0; i < parameters.NumStealthAddresses; i++)
                {
                    CreateAccount(
                        new CreateAccountParameters(parameters.NumStealthAddresses == 1 ? $"Stealth" : $"Stealth {i+1}")
                        .SetDeterministic()
                        .Stealth()
                        .SkipScan()
                        .NoSave());
                }
            }

            if (parameters.TransparentAddressNames != null && parameters.TransparentAddressNames.Count > 0)
            {
                parameters.TransparentAddressNames.ForEach(name =>
                {
                    CreateAccount(
                        new CreateAccountParameters(name)
                        .SetDeterministic()
                        .Transparent()
                        .SkipScan()
                        .NoSave());
                });
            }
            else
            {
                for (int i = 0; i < parameters.NumTransparentAddresses; i++)
                {
                    CreateAccount(
                        new CreateAccountParameters(parameters.NumTransparentAddresses == 1 ? $"Transparent" : $"Transparent {i + 1}")
                        .SetDeterministic()
                        .Transparent()
                        .SkipScan()
                        .NoSave());
                }
            }

            if (!parameters.ScanForBalance)
            {
                lastSeenHeight = ViewProvider.GetDefaultProvider().GetChainHeight();
            }

            // save
            Path = System.IO.Path.Combine(directory, $"{Label}.db");
            BuildDatabase();
            SaveKey("EncryptedEntropy", EncryptedEntropy);
            SaveKey("Encrypted", Encrypted);
            SaveKey("Timestamp", Timestamp);
            SaveKey("EntropyChecksum", EntropyChecksum.Bytes);
            SaveKey("LastSeenHeight", lastSeenHeight);
            if (Accounts.Count > 0)
            {
                lock (db_lock)
                {
                    using var ctx = new WalletDBContext(Path);
                    Accounts.ForEach(acc =>
                    {
                        ctx.Accounts.Add(acc);
                    });
                    ctx.SaveChanges();
                }
            }

            syncingService = new WalletFundsService(this, ViewProvider.GetDefaultProvider());
            RegisterService(syncingService);
            _ = Task.Run(() => syncingService.StartFundsScan()).ConfigureAwait(false);
        }

        private SQLiteWallet(string path, string passphrase)
        {
            Path = path;
            var entropyChecksum = LoadKey("EntropyChecksum");
            EntropyChecksum = new(entropyChecksum, false);

            EncryptedEntropy = LoadKey("EncryptedEntropy");
            Encrypted = Serialization.GetBool(LoadKey("Encrypted"), 0);
            if (!Encrypted)
            {
                Entropy = EncryptedEntropy;
            }
            else
            {
                Entropy = EncryptedEntropy.DecryptEntropy(EntropyChecksum, passphrase);
            }

            Timestamp = Serialization.GetUInt64(LoadKey("Timestamp"), 0);
            IsEncrypted = false;
            EntropyLen = (uint)Entropy.Length;
            lastSeenHeight = Serialization.GetInt64(LoadKey("LastSeenHeight"), 0);

            syncingService = new WalletFundsService(this, ViewProvider.GetDefaultProvider());
            RegisterService(syncingService);
            _ = Task.Run(() => syncingService.StartFundsScan()).ConfigureAwait(false);

            Accounts = LoadAccounts();
        }

        public SQLiteWallet()
        {
        }

        private void BuildDatabase()
        {
            using var ctx = new WalletDBContext(Path);
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        internal byte[] LoadKey(string name)
        {
            using var ctx = new WalletDBContext(Path);
            return ctx.KVPairs.FirstOrDefault(p => p.Name == name)?.Value;
        }

        private void SaveKey(string name, ulong value) => SaveKey(name, Serialization.UInt64(value));
        internal void SaveKey(string name, long value) => SaveKey(name, Serialization.Int64(value));
        private void SaveKey(string name, bool value) => SaveKey(name, Serialization.Bool(value));
        private void SaveKey(string name, string value) => SaveKey(name, Encoding.UTF8.GetBytes(value));
        private void SaveKey(string name, byte[] value)
        {
            lock (db_lock)
            {
                using var ctx = new WalletDBContext(Path);
                SaveKey(ctx, name, value);
                ctx.SaveChanges();
            }
        }

        private void SaveKey(WalletDBContext ctx, string name, byte[] value)
        {
            KVPair? kvpair = ctx.KVPairs.FirstOrDefault(p => p.Name == name);
            if (kvpair == null)
            {
                ctx.KVPairs.Add(new KVPair
                {
                    Name = name,
                    Value = value
                });
            }
            else
            {
                kvpair.Value = value;
            }
        }

        public void DeleteKey(string name)
        {
            lock (db_lock)
            {
                using var ctx = new WalletDBContext(Path);
                KVPair? kvpair = ctx.KVPairs.FirstOrDefault(p => p.Name == name);
                if (kvpair != null)
                {
                    ctx.KVPairs.Remove(kvpair);
                }
                ctx.SaveChanges();
            }
        }

        public static SQLiteWallet CreateWallet(CreateWalletParameters parameters)
        {
            // first check if wallet with label already exists
            var labels = Directory.GetFiles(directory).ToList()
                .Select(path => System.IO.Path.GetFileName(path))
                .Where(file => System.IO.Path.HasExtension(".db"))
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
            if (labels.Any(lbl => lbl == parameters.Label)) throw new ArgumentException($"{nameof(parameters.Label)} shares name with existing wallet");

            var wallet = new SQLiteWallet(parameters);
            wallet.Register();
            return wallet;
        }

        public static SQLiteWallet OpenWallet(string pathOrLabel, string passphrase)
        {
            var label = pathOrLabel;
            var path = System.IO.Path.Combine(directory, $"{label}.db");

            if (File.Exists(pathOrLabel))
            {
                // loading a wallet file externally
                label = System.IO.Path.GetFileNameWithoutExtension(pathOrLabel);
                path = pathOrLabel;
            }

            if (Wallets.Keys.Any(x => x == label)) throw new Exception("wallet already loaded");

            var wallet = new SQLiteWallet(path, passphrase);
            wallet.Register();
            return wallet;
        }

        public void SetLastSeenHeight(long height)
        {
            SaveKey("LastSeenHeight", height);
            Interlocked.Exchange(ref lastSeenHeight, height);
        }

        public long GetLastSeenHeight() => Interlocked.Read(ref lastSeenHeight);

        private Account CreateAccount(CreateAccountParameters parameters)
        {
            if (IsEncrypted)
            {
                throw new FormatException();
            }

            Account account = new Account();
            account.Name = parameters.Name;
            account.Deterministic = parameters.Deterministic;
            account.Type = parameters.Type;

            if (account.Deterministic)
            {
                if (account.Type == 0)
                {
                    ((account.SecSpendKey, account.PubSpendKey), (account.SecViewKey, account.PubViewKey)) = this.GetStealthAccountKeyPairs();
                    account.Address = new StealthAddress(account.PubViewKey, account.PubSpendKey).ToString();
                }
                else if (account.Type == 1)
                {
                    (account.SecKey, account.PubKey) = this.GetTransparentAccountKeyPair();
                    account.Address = new TAddress(account.PubKey).ToString();
                }
                else
                {
                    throw new FormatException();
                }
            }
            else
            {
                if (account.Type == 0)
                {
                    (account.SecSpendKey, account.PubSpendKey) = KeyOps.GenerateKeypair();
                    (account.SecViewKey, account.PubViewKey) = KeyOps.GenerateKeypair();
                    account.Address = new StealthAddress(account.PubViewKey, account.PubSpendKey).ToString();
                }
                else if (account.Type == 1)
                {
                    (account.SecKey, account.PubKey) = KeyOps.GenerateKeypair();
                    account.Address = new TAddress(account.PubKey).ToString();
                }
                else
                {
                    throw new FormatException();
                }
            }

            account.EncryptAccountPrivateKeys(this.Entropy);
            account.Balance = 0;
            account.Encrypted = false;
            account.Wallet = this;
            account.UTXOs = new(new UTXOEqualityComparer());
            account.TxHistory = new(new HistoryTxEqualityComparer());

            if (parameters.Save)
            {
                lock (db_lock)
                {
                    using var ctx = new WalletDBContext(Path);
                    ctx.Accounts.Add(account);
                    ctx.SaveChanges();
                }
            }

            if (parameters.ScanForBalance)
            {
                SaveKey(account.Address, -1);
                account.Syncing = true;
                AccountFundsService service = new(ViewProvider.GetDefaultProvider(), account);
                RegisterService(service);
                _ = Task.Run(() => service.StartFundsScan()).ConfigureAwait(false);
            }

            return account;
        }

        private JsonElement AccountToJson(Account account)
        {
            JsonSerializerOptions options = new();
            converters.ForEach(x => options.Converters.Add(x));
            var elem = JsonSerializer.SerializeToElement(account, account.GetType(), options);
            return elem;
        }

        private JsonElement WalletToJson(SQLiteWallet wallet)
        {
            JsonSerializerOptions options = new();
            converters.ForEach(x => options.Converters.Add(x));
            var elem = JsonSerializer.SerializeToElement(wallet, wallet.GetType(), options);
            return elem;
        }

        public async Task<JsonElement> DoCreateAccount(CreateAccountParameters parameters)
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) throw new Exception("wallet is encrypted");
            var account = CreateAccount(parameters);
            requestSemaphore.Release();
            return AccountToJson(account);
        }

        private bool CheckIntegrity()
        {
            return Accounts.All(x => x.TryCheckAccountIntegrity());
        }

        public async Task<bool> DoCheckIntegrity()
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) throw new Exception("wallet is encrypted");
            var success = CheckIntegrity();
            requestSemaphore.Release();
            return success;
        }

        private void ChangeAccountName(string address, string newName)
        {
            var account = Accounts.Where(x => x.Address == address).FirstOrDefault();
            if (account == null) return;    // account not found
            lock (db_lock)
            {
                using var ctx = new WalletDBContext(Path);
                account.Name = newName;
                ctx.SaveChanges();
            }
        }

        public async Task DoChangeAccountName(string address, string newName)
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) throw new Exception("wallet is encrypted");
            ChangeAccountName(address, newName);
            requestSemaphore.Release();
        }

        public async Task<IEnumerable<JsonElement>> DoGetAccounts()
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) throw new Exception("wallet is encrypted");
            var accounts = Accounts.Select(x => AccountToJson(x)).ToList();
            requestSemaphore.Release();
            return accounts;
        }

        public async Task<ulong> DoGetBalance()
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) return 0;
            var balance = Accounts.Select(x => x.Balance).Aggregate((x, y) => x + y);
            requestSemaphore.Release();
            return balance;
        }

        public async Task<ulong> DoGetAccountBalance(string address)
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) return 0;
            var account = Accounts.Where(x => x.Address == address).FirstOrDefault();
            if (account == null) throw new Exception("could not find account");
            var balance = account.Balance;
            requestSemaphore.Release();
            return balance;
        }

        public async Task<long> DoGetAccountHeight() => throw new Exception("depracated");

        public async Task<long> DoGetWalletHeight()
        {
            await requestSemaphore.WaitAsync();
            if (IsEncrypted) throw new Exception("wallet is encrypted");
            var height = GetLastSeenHeight();
            requestSemaphore.Release();
            return height;
        }

        public async Task<JsonElement> DoGetTransactionHistory(string address)
        {
            await requestSemaphore.WaitAsync();
            var account = Accounts.Where(x => x.Address == address).FirstOrDefault();
            if (account == null) throw new Exception("could not find account");

            JsonSerializerOptions options = new();
            converters.ForEach(x => options.Converters.Add(x));
            var txhistory = JsonSerializer.SerializeToElement(account.TxHistory, options);
            requestSemaphore.Release();
            return txhistory;
        }

        public async Task<JsonElement> DoGetWallet()
        {
            await requestSemaphore.WaitAsync();
            var wallet = WalletToJson(this);
            requestSemaphore.Release();
            return wallet;
        }

        public static async Task<IEnumerable<JsonElement>> DoGetWallets()
        {
            return await Task.WhenAll(Wallets.Values.AsParallel()
                .Select(async (x) => await x.DoGetWallet()));
        }

        public static IEnumerable<string> DoGetWalletsFromDir()
        {
            return Directory.GetFiles(directory)
                .Select(path => System.IO.Path.GetFileName(path))
                .Where(file => System.IO.Path.HasExtension(".db"))
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
        }

        public static int GetWalletStatus(string label)
        {
            if (Wallets.Keys.Any(x => x == label))
            {
                var wallet = Wallets[label];
                return wallet.IsEncrypted ? 1 : 0;
            }

            return 2;
        }

        public static IEnumerable<(string,int)> DoGetWalletStatuses()
        {
            return Directory.GetFiles(directory)
                .Select(path => System.IO.Path.GetFileName(path))
                .Where(file => System.IO.Path.HasExtension(".db"))
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file))
                .Except(Wallets.Keys).Select(x => (x, 2))
                .Union(Wallets.Values.Where(x => x.IsEncrypted).Select(x => (x.Label, 1)))
                .Union(Wallets.Values.Where(x => !x.IsEncrypted).Select(x => (x.Label, 0)));
        }

        public List<Account> LoadAccounts()
        {
            using var ctx = new WalletDBContext(Path);
            List<Account> accs = ctx.Accounts.AsEnumerable().ToList();
            accs.ForEach(acc =>
            {
                acc.Wallet = this;
                acc.DecryptAccountPrivateKeys(Entropy);

                acc.UTXOs = new HashSet<UTXO>(ctx.UTXOs.Where(u => u.Address == acc.Address), new UTXOEqualityComparer());
                acc.DecryptUTXOs();
                acc.TxHistory = new HashSet<HistoryTx>(ctx.HistoryTxs.Where(tx => tx.Address == acc.Address), new HistoryTxEqualityComparer());
                acc.DecryptHistoryTxs();

                var lshb = LoadKey(acc.Address);
                if (lshb != null)
                {
                    acc.Syncing = true;
                    AccountFundsService service = new(ViewProvider.GetDefaultProvider(), acc);
                    RegisterService(service);
                    _ = Task.Run(() => service.StartFundsScan()).ConfigureAwait(false);
                }
            });
            return accs;
        }

        public void SaveAccountFundData(Account account, IEnumerable<UTXO> spents, IEnumerable<UTXO> utxos, IEnumerable<HistoryTx> txs)
        {
            if (spents != null || utxos != null || txs != null)
            {
                using var ctx = new WalletDBContext(Path);

                lock (db_lock)
                {
                    if (spents != null)
                    {
                        lock (account.UTXOs)
                        {
                            spents.ToList().ForEach(x => { account.UTXOs.Remove(x); account.Balance -= x.DecodedAmount; });
                        }
                        if (account.SortedUTXOs != null)
                        {
                            lock (account.SortedUTXOs)
                            {
                                spents.ToList().ForEach(x => account.SortedUTXOs.Remove(x));
                            }
                        }
                        ctx.UTXOs.RemoveRange(spents);
                    }

                    if (utxos != null)
                    {
                        lock (account.UTXOs)
                        {
                            account.UTXOs.UnionWith(utxos);
                            utxos.ToList().ForEach(x => account.Balance += x.DecodedAmount);
                        }
                        if (account.SortedUTXOs != null)
                        {
                            lock (account.SortedUTXOs)
                            {
                                account.SortedUTXOs.UnionWith(utxos);
                            }
                        }
                        ctx.UTXOs.AddRange(utxos);
                    }

                    if (txs != null)
                    {

                        txs = txs.Where(tx => !account.TxHistory.Contains(tx));
                        lock (account.TxHistory)
                        {
                            account.TxHistory.UnionWith(txs);
                        }
                        ctx.HistoryTxs.AddRange(txs);
                    }

                    ctx.SaveChanges();
                }
            }
        }

        public void Encrypt()
        {
            if (IsEncrypted) return;

            Accounts.ForEach(acc =>
            {
                acc.EncryptAccountPrivateKeys(Entropy);
                acc.EncryptHistoryTxs();
                acc.EncryptUTXOs();
                acc.Balance = 0;
            });

            Array.Clear(Entropy);
            IsEncrypted = true;
        }

        public void Decrypt(string passphrase)
        {
            Entropy = EncryptedEntropy.DecryptEntropy(EntropyChecksum, passphrase);
            Accounts.ForEach(acc =>
            {
                acc.DecryptAccountPrivateKeys(Entropy);
                acc.DecryptHistoryTxs();
                acc.DecryptUTXOs();
                acc.Balance = acc.UTXOs.Select(x => x.DecodedAmount).Aggregate((x, y) => x + y);
            });
        }

        public bool ChangeLabel(string newLabel)
        {
            if (string.IsNullOrEmpty(newLabel)) return false;
            if (newLabel == Label) return false;

            var labels = Directory.GetFiles(directory).ToList()
                .Select(path => System.IO.Path.GetFileName(path))
                .Where(file => System.IO.Path.HasExtension(".db"))
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
            if (labels.Any(lbl => lbl == newLabel)) return false;

            lock (db_lock)
            {
                var newPath = System.IO.Path.Combine(directory, $"{newLabel}.db");
                File.Move(Path, newPath);
                Path = newPath;
            }

            Label = newLabel;

            return true;
        }

        public async Task Start(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (lockRequested)
                {
                    services.ForEach(x => x.Interrupt());
                    while (!services.All(x => x.Paused)) await Task.Delay(100, token);
                    Encrypt();
                    lockRequested = false;
                }
                
                if (!IsEncrypted)
                {
                    CleanServices();
                    services.Where(x => x.Paused).ToList().ForEach(x => x.Resume());
                }
            }

            await Task.Delay(500, token);
        }
    }
}
