using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Daemon;
using Discreet.Common.Converters;
using Discreet.Wallets.Comparers;
using Discreet.DB;
using Discreet.Wallets.Models;
using Discreet.Wallets.Services;
using Discreet.Wallets.Utilities;
using Discreet.Wallets.Utilities.Converters;
using System.Threading;
using System.IO;
using Discreet.ZMQ;
using Discreet.Common;
using Microsoft.EntityFrameworkCore;
using Discreet.Cipher.Mnemonics;
using System.Reflection.Emit;
using Discreet.Coin.Models;
using Discreet.Wallets.Extensions;

namespace Discreet.Wallets
{
    public class SQLiteWallet
    {
        public string CoinName => "Discreet";
        public string Version => "0.2";

        public static ConcurrentDictionary<string, SQLiteWallet> Wallets = new();
        private static readonly string directory;
        private CancellationTokenSource cancellationTokenSource = new();
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
        public SHA256 EntropyChecksum { get; set; }

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

        public async Task DoLockWallet()
        {
            await requestSemaphore.WaitAsync();
            try
            {
                RequestLock();
                while (!IsEncrypted) await Task.Delay(100, cancellationTokenSource.Token);
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<bool> Unlock(string passphrase)
        {
            await requestSemaphore.WaitAsync();
            try
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
            finally
            {
                requestSemaphore.Release();
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
                    Accounts.Add(CreateAccount(
                        new CreateAccountParameters(name)
                        .SetDeterministic()
                        .Stealth()
                        .SkipScan()
                        .NoSave()));
                });
            }
            else
            {
                for(int i = 0; i < parameters.NumStealthAddresses; i++)
                {
                    Accounts.Add(CreateAccount(
                        new CreateAccountParameters(parameters.NumStealthAddresses == 1 ? $"Stealth" : $"Stealth {i+1}")
                        .SetDeterministic()
                        .Stealth()
                        .SkipScan()
                        .NoSave()));
                }
            }

            if (parameters.TransparentAddressNames != null && parameters.TransparentAddressNames.Count > 0)
            {
                parameters.TransparentAddressNames.ForEach(name =>
                {
                    Accounts.Add(CreateAccount(
                        new CreateAccountParameters(name)
                        .SetDeterministic()
                        .Transparent()
                        .SkipScan()
                        .NoSave()));
                });
            }
            else
            {
                for (int i = 0; i < parameters.NumTransparentAddresses; i++)
                {
                    Accounts.Add(CreateAccount(
                        new CreateAccountParameters(parameters.NumTransparentAddresses == 1 ? $"Transparent" : $"Transparent {i + 1}")
                        .SetDeterministic()
                        .Transparent()
                        .SkipScan()
                        .NoSave()));
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

            syncingService = new ParallelWalletFundsService(this, ViewProvider.GetDefaultProvider());
            RegisterService(syncingService);
            _ = Task.Run(() => syncingService.StartFundsScanAsync(cancellationTokenSource.Token)).ConfigureAwait(false);
        }

        private SQLiteWallet(string path, string passphrase)
        {
            Path = path;
            Label = System.IO.Path.GetFileNameWithoutExtension(Path);
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

            Accounts = LoadAccounts();

            syncingService = new ParallelWalletFundsService(this, ViewProvider.GetDefaultProvider());
            RegisterService(syncingService);
            _ = Task.Run(() => syncingService.StartFundsScanAsync(cancellationTokenSource.Token)).ConfigureAwait(false);
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

        private static byte[] LoadKey(string label, string name)
        {
            var path = System.IO.Path.Combine(directory, $"{label}.db");
            using var ctx = new WalletDBContext(path);
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
            // check if the wallet name is null or empty
            if (string.IsNullOrEmpty(parameters.Label))
            {
                throw new FormatException($"{nameof(parameters.Label)} is null or empty");
            }

            // first check if wallet with label already exists
            var labels = Directory.GetFiles(directory, "*.db")
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

            if (!File.Exists(path)) throw new Exception($"could not find wallet at \"{path}\"");

            if (Wallets.Keys.Any(x => x == label)) throw new Exception("wallet already loaded");

            var wallet = new SQLiteWallet(path, passphrase);
            wallet.Register();
            return wallet;
        }

        public static (string, Mnemonic) GetMnemonic(string label, string password)
        {
            switch (GetWalletStatus(label))
            {
                case 0:
                    {
                        var wallet = Wallets[label];
                        var success = EncryptionUtil.CheckPassword(wallet.EntropyChecksum, password);
                        if (!success) return (null, null);
                        return (Printable.Hexify(wallet.Entropy), new Mnemonic(wallet.Entropy));
                    }
                case 1:
                    {
                        var wallet = Wallets[label];
                        var success = EncryptionUtil.CheckPassword(wallet.EntropyChecksum, password);
                        if (!success) return (null, null);
                        var entropy = wallet.EncryptedEntropy.DecryptEntropy(wallet.EntropyChecksum, password);
                        return (Printable.Hexify(entropy), new Mnemonic(entropy));
                    }
                case 2:
                    {
                        var checksum = new SHA256(LoadKey(label, "EntropyChecksum"), false);
                        var encryptedEntropy = LoadKey(label, "EncryptedEntropy");
                        var success = EncryptionUtil.CheckPassword(checksum, password);
                        if (!success) return (null, null);
                        var entropy = encryptedEntropy.DecryptEntropy(checksum, password);
                        return (Printable.Hexify(entropy), new Mnemonic(entropy));
                    }
                default:
                    throw new FormatException(nameof(GetWalletStatus));
            }
        }

        public static (Mnemonic Spend, Mnemonic View, Mnemonic Sec) GetAccountPrivateKeys(string label, string address, string password)
        {
            if (Wallets.Values.Where(x => x.Accounts.Where(y => y.Address.Equals(address)).Any()).Any())
            {
                var wallet = Wallets.Values.Where(x => x.Accounts.Where(y => y.Address.Equals(address)).Any()).First();
                if (GetWalletStatus(wallet.Label) == 0)
                {
                    var success = EncryptionUtil.CheckPassword(wallet.EntropyChecksum, password);
                    if (!success) return (null, null, null);
                    var account = wallet.Accounts.Where(x => x.Address.Equals(address)).First();
                    if (account.Type == 1)
                    {
                        return (null, null, new Mnemonic(Key.Clone(account.SecKey).bytes));
                    }
                    else
                    {
                        return (new Mnemonic(Key.Clone(account.SecSpendKey).bytes), new Mnemonic(Key.Clone(account.SecViewKey).bytes), null);
                    }
                }
                else
                {
                    var success = EncryptionUtil.CheckPassword(wallet.EntropyChecksum, password);
                    if (!success) return (null, null, null);
                    var account = wallet.Accounts.Where(x => x.Address.Equals(address)).First();
                    var entropy = wallet.EncryptedEntropy.DecryptEntropy(wallet.EntropyChecksum, password);
                    (var spend, var view, var sec) = account.DecryptPrivateKeys(entropy);
                    Array.Clear(entropy);

                    if (account.Type == 1)
                    {
                        return (null, null, new Mnemonic(sec.bytes));
                    }
                    else
                    {
                        return (new Mnemonic(spend.bytes), new Mnemonic(view.bytes), null);
                    }
                }
            }

            {
                var checksum = new SHA256(LoadKey(label, "EntropyChecksum"), false);
                var encryptedEntropy = LoadKey(label, "EncryptedEntropy");
                var success = EncryptionUtil.CheckPassword(checksum, password);
                if (!success) return (null, null, null);
                var entropy = encryptedEntropy.DecryptEntropy(checksum, password);

                var path = System.IO.Path.Combine(directory, $"{label}.db");
                using var ctx = new WalletDBContext(path);

                var account = ctx.Accounts.Where(x => x.Address == address).FirstOrDefault();
                if (account == null) return (null, null, null);

                (var spend, var view, var sec) = account.DecryptPrivateKeys(entropy);
                Array.Clear(entropy);

                if (account.Type == 1)
                {
                    return (null, null, new Mnemonic(sec.bytes));
                }
                else
                {
                    return (new Mnemonic(spend.bytes), new Mnemonic(view.bytes), null);
                }
            }
        }

        public void SetLastSeenHeight(long height)
        {
            SaveKey("LastSeenHeight", height);
            Interlocked.Exchange(ref lastSeenHeight, height);
        }

        public long GetLastSeenHeight() => Interlocked.Read(ref lastSeenHeight);

        public async Task<(bool, long)> DoGetHeight()
        {
            await requestSemaphore.WaitAsync();

            try
            {
                var height = Math.Max(GetLastSeenHeight(), syncingService.GetLastSeenHeight());
                var synced = syncingService.State == ServiceState.SYNCED;
                return (synced, height);
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<(bool, bool, long)> DoGetAccountHeight(string address)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                var accountSyncer = services.Where(x => x.GetType() == typeof(AccountFundsService))
                .Select(x => (AccountFundsService)x)
                .Where(x => x.Account.Address == address)
                .FirstOrDefault();
                var height = Math.Max(GetLastSeenHeight(), syncingService.GetLastSeenHeight());
                var synced = syncingService.State == ServiceState.SYNCED;
                var syncer = false;
                if (accountSyncer != null)
                {
                    height = accountSyncer.GetLastSeenHeight();
                    synced = false;
                    syncer = true;
                }

                return (synced, syncer, height);
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

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

            if (string.IsNullOrEmpty(parameters.Name))
            {
                throw new FormatException(nameof(parameters.Name) + " is null or empty");
            }

            if (account.Deterministic)
            {
                if (account.Type == 0)
                {
                    ((account.SecSpendKey, account.PubSpendKey), (account.SecViewKey, account.PubViewKey)) = this.GetStealthAccountKeyPairs();
                    account.Address = new StealthAddress(account.PubViewKey.Value, account.PubSpendKey.Value).ToString();
                }
                else if (account.Type == 1)
                {
                    (account.SecKey, account.PubKey) = this.GetTransparentAccountKeyPair();
                    account.Address = new TAddress(account.PubKey.Value).ToString();
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
                    account.Address = new StealthAddress(account.PubViewKey.Value, account.PubSpendKey.Value).ToString();
                }
                else if (account.Type == 1)
                {
                    (account.SecKey, account.PubKey) = KeyOps.GenerateKeypair();
                    account.Address = new TAddress(account.PubKey.Value).ToString();
                }
                else
                {
                    throw new FormatException();
                }
            }

            account.EncryptAccountPrivateKeys(this.Entropy);
            account.DecryptAccountPrivateKeys(this.Entropy);
            account.Balance = 0;
            account.Encrypted = false;
            account.Wallet = this;
            account.UTXOs = new(new UTXOEqualityComparer());
            account.SelectedUTXOs = new(new UTXOEqualityComparer());
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
                _ = Task.Run(() => service.StartFundsScan(cancellationTokenSource.Token)).ConfigureAwait(false);
            }

            return account;
        }

        private JsonElement AccountToJson(Account account)
        {
            JsonSerializerOptions options = new();
            converters.ForEach(x => options.Converters.Add(x));
            var json = JsonSerializer.Serialize(account, account.GetType(), options);
            var elem = JsonSerializer.Deserialize(json, typeof(object));
            return (JsonElement)elem;
        }

        private JsonElement WalletToJson(SQLiteWallet wallet)
        {
            JsonSerializerOptions options = new();
            converters.ForEach(x => options.Converters.Add(x));
            var json = JsonSerializer.Serialize(wallet, wallet.GetType(), options);
            var elem = JsonSerializer.Deserialize(json, typeof(object));
            return (JsonElement)elem;
        }

        public async Task<FullTransaction> DoCreateTransaction(string address, IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts, bool relay)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                var acc = Accounts.Where(x => x.Address == address).FirstOrDefault();
                if (acc == null) throw new Exception("could not get account");
                CreateTransactionService ctxs = new(acc, relay);
                RegisterService(ctxs);
                var tx = ctxs.CreateTransaction(addresses, amounts);
                _ = Task.Run(() => ctxs.WaitForSuccess(cancellationTokenSource.Token)).ConfigureAwait(false);
                return tx;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<bool> DoUnload()
        {
            await requestSemaphore.WaitAsync();

            try
            {
                cancellationTokenSource.Cancel();
                while (services.Any(x => !x.Completed)) await Task.Delay(500);
                Wallets.Remove(Label, out _);
                Encrypt();
                return true;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<JsonElement> DoCreateAccount(CreateAccountParameters parameters)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                if (IsEncrypted) throw new Exception("wallet is encrypted");
                var account = CreateAccount(parameters.SetSave());
                lock (Accounts)
                {
                    Accounts.Add(account);
                }
                return AccountToJson(account);
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        private bool CheckIntegrity()
        {
            return Accounts.All(x => x.TryCheckAccountIntegrity());
        }

        public async Task<bool> DoCheckIntegrity()
        {
            await requestSemaphore.WaitAsync();

            try
            {
                if (IsEncrypted) throw new Exception("wallet is encrypted");
                var success = CheckIntegrity();
                return success;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<bool> DoCheckAccountIntegrity(string address)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                if (IsEncrypted) throw new Exception("wallet is encrypted");
                var success = Accounts.Where(x => x.Address == address).First().TryCheckAccountIntegrity();
                return success;
            }
            finally
            {
                requestSemaphore.Release();
            }
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

            try
            {
                if (IsEncrypted) throw new Exception("wallet is encrypted");
                ChangeAccountName(address, newName);
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<IEnumerable<JsonElement>> DoGetAccounts()
        {
            await requestSemaphore.WaitAsync();

            try
            {
                if (IsEncrypted) throw new Exception("wallet is encrypted");
                var accounts = Accounts.Select(x => AccountToJson(x)).ToList();
                return accounts;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<ulong> DoGetBalance()
        {
            await requestSemaphore.WaitAsync();

            try
            {
                if (IsEncrypted) return 0;
                var balance = Accounts.Select(x => x.Balance).Aggregate(0UL, (x, y) => x + y);
                return balance;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<ulong> DoGetAccountBalance(string address)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                if (IsEncrypted) return 0;
                var account = Accounts.Where(x => x.Address == address).FirstOrDefault();
                if (account == null) throw new Exception("could not find account");
                var balance = account.Balance;
                return balance;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<JsonElement> DoGetTransactionHistory(string address)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                var account = Accounts.Where(x => x.Address == address).FirstOrDefault();
                if (account == null) throw new Exception("could not find account");

                JsonSerializerOptions options = new();
                converters.ForEach(x => options.Converters.Add(x));
                var json = JsonSerializer.Serialize(account.TxHistory, options);
                var elem = JsonSerializer.Deserialize(json, typeof(object));
                return (JsonElement)elem;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public async Task<JsonElement> DoGetWallet()
        {
            await requestSemaphore.WaitAsync();

            try
            {
                var wallet = WalletToJson(this);
                return wallet;
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        public static async Task<IEnumerable<JsonElement>> DoGetWallets()
        {
            return await Task.WhenAll(Wallets.Values.AsParallel()
                .Select(async (x) => await x.DoGetWallet()));
        }

        public static IEnumerable<string> DoGetWalletsFromDir()
        {
            return Directory.GetFiles(directory, "*.db")
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
        }

        public static int GetWalletStatus(string label)
        {
            if (Wallets.Keys.Any(x => x == label))
            {
                var wallet = Wallets[label];
                return wallet.IsEncrypted ? 1 : 0;
            }

            var check = Directory.GetFiles(directory, "*.db")
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file))
                .Where(lbl => lbl == label).Any();

            if (check) return 2;
            else throw new Exception("cannot find wallet with label \"" + label + "\"");
        }

        public static IEnumerable<(string,int)> DoGetWalletStatuses()
        {
            return Directory.GetFiles(directory, "*.db")
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
                acc.Balance = acc.UTXOs.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y);

                var txs = ctx.HistoryTxs.Where(tx => tx.Address == acc.Address).ToList();
                acc.DecryptHistoryTxs(txs);
                acc.TxHistory = new HashSet<HistoryTx>(txs, new HistoryTxEqualityComparer());

                acc.SelectedUTXOs = new(new UTXOEqualityComparer());
                // check if invalid utxos
                var inv = acc.CheckObsoleteUTXOs();
                if (inv != null && inv.Count > 0)
                {
                    SaveAccountFundData(acc, inv, Enumerable.Empty<UTXO>(), Enumerable.Empty<HistoryTx>());
                }

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
                ctx.Database.ExecuteSqlRaw("PRAGMA journal_mode = off;");
                lock (db_lock)
                {
                    if (spents != null)
                    {
                        lock (account.UTXOs)
                        {
                            spents.ToList().ForEach(x => { account.UTXOs.Remove(x); account.Balance -= x.DecodedAmount; });
                        }
                        lock (account.SelectedUTXOs)
                        {
                            // check if utxos were selected, then remove them
                            spents.ToList().ForEach(x => account.SelectedUTXOs.Remove(x));
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
                        // txs is transformed since a HistoryTx might be included as part of CreateTransactionService's WaitForSuccess()
                        txs = txs.Where(tx => !account.TxHistory.Contains(tx)).ToList();
                        account.EncryptHistoryTxs(txs);
                        lock (account.TxHistory)
                        {
                            account.TxHistory.UnionWith(txs);
                        }
                        ctx.HistoryTxs.AddRange(txs);
                    }

                    ctx.SaveChanges();
                }

                Publisher.Instance.Publish("processblocknotify", "");
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
            if (!IsEncrypted) return;

            Entropy = EncryptedEntropy.DecryptEntropy(EntropyChecksum, passphrase);
            Accounts.ForEach(acc =>
            {
                acc.DecryptAccountPrivateKeys(Entropy);
                acc.DecryptHistoryTxs();
                acc.DecryptUTXOs();
                acc.Balance = acc.UTXOs.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y);
            });

            IsEncrypted = false;
        }

        public bool ChangeLabel(string newLabel)
        {
            if (string.IsNullOrEmpty(newLabel)) return false;
            if (newLabel == Label) return false;

            var labels = Directory.GetFiles(directory, "*.db")
                .Select(file => System.IO.Path.GetFileNameWithoutExtension(file));
            if (labels.Any(lbl => lbl == newLabel)) return false;

            lock (db_lock)
            {
                var newPath = System.IO.Path.Combine(directory, $"{newLabel}.db");
                File.Move(Path, newPath);
                if (File.Exists(System.IO.Path.Combine(directory, $"{Label}.db-shm")))
                {
                    File.Move(Path, System.IO.Path.Combine(directory, $"{Label}.db-shm"));
                }
                if (File.Exists(System.IO.Path.Combine(directory, $"{Label}.db-shm")))
                {
                    File.Move(Path, System.IO.Path.Combine(directory, $"{Label}.db-wal"));
                }
                Path = newPath;
            }

            Label = newLabel;

            return true;
        }

        public async Task<bool> DoChangeLabel(string newLabel)
        {
            await requestSemaphore.WaitAsync();

            try
            {
                var success = ChangeLabel(newLabel);
                return success;
            }
            finally
            {
                requestSemaphore.Release();
            }
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

                //Daemon.Logger.Log($"wallet height: {syncingService.GetLastSeenHeight()}");

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException e)
                {
                    return;
                }
            }
        }

        public class WalletMigration
        {
            /// <summary>
            /// Migrates a Legacy Wallet to the new Wallet framework.
            /// </summary>
            /// <param name="wallet"></param>
            /// <returns></returns>
            public static bool Migrate(string label, string passphrase, bool resync = true)
            {
                var legacyWallet = WalletsLegacy.WalletDB.GetDB().TryGetWallet(label);
                if (legacyWallet == null)
                {
                    Daemon.Logger.Error($"WalletMigration.Migrate: could not find legacy wallet with label \"{label}\"");
                    return false;
                }
                if (!legacyWallet.TryDecrypt(passphrase))
                {
                    Daemon.Logger.Error("WalletMigration.Migrate: could not decrypt wallet");
                    return false;
                }

                if (Directory.GetFiles(directory, "*.db").Select(file => System.IO.Path.GetFileNameWithoutExtension(file)).Any(lbl => lbl == label))
                {
                    Daemon.Logger.Error($"WalletMigration.Migrate: could not migrate wallet with label \"{label}\" as it shares a name with another wallet");
                    return false;
                }

                var newWallet = new SQLiteWallet();
                newWallet.Entropy = legacyWallet.Entropy;
                newWallet.EncryptedEntropy = newWallet.Entropy.EncryptEntropy(passphrase, out var echksm);
                newWallet.EntropyChecksum = echksm;

                newWallet.Path = System.IO.Path.Combine(directory, $"{label}.db");
                newWallet.Label = label;

                newWallet.lastSeenHeight = resync ? -1 : legacyWallet.LastSeenHeight;
                newWallet.IsEncrypted = false;
                newWallet.Encrypted = legacyWallet.Encrypted;
                newWallet.Timestamp = legacyWallet.Timestamp;
                newWallet.Accounts = new();

                newWallet.BuildDatabase();

                newWallet.SaveKey("EncryptedEntropy", newWallet.EncryptedEntropy);
                newWallet.SaveKey("Encrypted", newWallet.Encrypted);
                newWallet.SaveKey("Timestamp", newWallet.Timestamp);
                newWallet.SaveKey("EntropyChecksum", newWallet.EntropyChecksum.Bytes);
                newWallet.SaveKey("LastSeenHeight", newWallet.lastSeenHeight);

                Dictionary<Account, (List<UTXO>, List<HistoryTx>)> accFundData = new();
                Dictionary<Account, WalletsLegacy.WalletAddress> accAssociations = new();
                foreach (var lacc in legacyWallet.Addresses)
                {
                    var acc = new Account();
                    acc.Name = lacc.Name;
                    acc.Address = lacc.Address;
                    acc.Wallet = newWallet;
                    acc.Deterministic = lacc.Deterministic;
                    acc.Type = lacc.Type;
                    acc.PubKey = lacc.PubKey;
                    acc.PubSpendKey = lacc.PubSpendKey;
                    acc.PubViewKey = lacc.PubViewKey;
                    acc.SecKey = lacc.SecKey;
                    acc.SecSpendKey = lacc.SecSpendKey;
                    acc.SecViewKey = lacc.SecViewKey;
                    acc.Syncing = resync ? false : lacc.Syncer;

                    acc.EncryptAccountPrivateKeys(newWallet.Entropy);
                    acc.DecryptAccountPrivateKeys(newWallet.Entropy);

                    acc.Balance = resync ? 0 : lacc.Balance;
                    acc.UTXOs = new();
                    acc.SelectedUTXOs = new(new UTXOEqualityComparer());
                    acc.TxHistory = new();

                    if (!resync && lacc.Syncer)
                    {
                        newWallet.SaveKey(acc.Address, lacc.LastSeenHeight);
                    }

                    accAssociations[acc] = lacc;
                    newWallet.Accounts.Add(acc);
                }

                if (newWallet.Accounts.Count > 0)
                {
                    lock (newWallet.db_lock)
                    {
                        using var ctx = new WalletDBContext(newWallet.Path);
                        newWallet.Accounts.ForEach(acc =>
                        {
                            ctx.Accounts.Add(acc);
                        });
                        ctx.SaveChanges();
                    }
                }

                if (!resync)
                {
                    foreach ((var acc, var lacc) in accAssociations)
                    {
                        var utxos = lacc.UTXOs.Select(ux => new UTXO
                        {
                            Address = acc.Address,
                            Type = (byte)ux.Type,
                            IsCoinbase = ux.IsCoinbase,
                            TransactionSrc = ux.TransactionSrc,
                            Amount = ux.Amount,
                            Index = ux.Index,
                            UXKey = ux.UXKey,
                            UXSecKey = ux.UXSecKey,
                            Commitment = ux.Commitment,
                            DecodeIndex = ux.DecodeIndex,
                            TransactionKey = ux.TransactionKey,
                            DecodedAmount = ux.DecodedAmount,
                            LinkingTag = ux.LinkingTag,
                            Encrypted = false,
                            Account = acc
                        }).ToList();

                        var htxs = lacc.TxHistory.Select(htx => new HistoryTx
                        {
                            Address = acc.Address,
                            TxID = htx.TxID,
                            Timestamp = htx.Timestamp,
                            Account = acc,
                            Inputs = htx.Inputs.Select(x => new HistoryTxOutput { Address = x.Address, Amount = x.Amount }).ToList(),
                            Outputs = htx.Outputs.Select(x => new HistoryTxOutput { Address = x.Address, Amount = x.Amount }).ToList(),
                        }).ToList();

                        acc.EncryptHistoryTxs(htxs);

                        accFundData[acc] = (utxos, htxs);

                        //newWallet.Accounts.Add(acc);
                    }

                    foreach ((var acc, (var utxos, var htxs)) in accFundData)
                    {
                        newWallet.SaveAccountFundData(acc, Array.Empty<UTXO>(), utxos, htxs);
                    }
                }
                
                newWallet.Register();

                newWallet.syncingService = new ParallelWalletFundsService(newWallet, ViewProvider.GetDefaultProvider());
                newWallet.RegisterService(newWallet.syncingService);
                _ = Task.Run(() => newWallet.syncingService.StartFundsScanAsync(newWallet.cancellationTokenSource.Token)).ConfigureAwait(false);

                foreach (var acc in newWallet.Accounts)
                {
                    if (acc.Syncing)
                    {
                        AccountFundsService service = new(ViewProvider.GetDefaultProvider(), acc);
                        newWallet.RegisterService(service);
                        _ = Task.Run(() => service.StartFundsScan(newWallet.cancellationTokenSource.Token)).ConfigureAwait(false);
                    }
                }

                return true;
            }
        }
    }
}
