using Discreet.Cipher;
using Discreet.Cipher.Mnemonics;
using Discreet.Coin;
using Discreet.Common;
using Discreet.RPC.Common;
using Discreet.Wallets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Discreet.RPC.Endpoints
{
    public static class WalletEndpoints
    {
        [RPCEndpoint("get_wallet", APISet.WALLET)]
        public static object GetWallet(string label)
        {
            try
            {
                var _wallet = Network.Handler.GetHandler().daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (_wallet == null)
                {
                    return new RPCError($"could not get wallet with label {label}");
                }

                return new Readable.Wallet(_wallet);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWallet failed: {ex.Message}");

                return new RPCError($"Could not get wallet {label}");
            }
        }

        [RPCEndpoint("get_wallets_from_db", APISet.WALLET)]
        public static object GetWalletsFromDb()
        {
            try
            {
                var db = WalletDB.GetDB();

                return db.GetWallets().Select(x => new Readable.Wallet(x)).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletsFromDb failed: {ex.Message}");

                return new RPCError($"Could not get wallets from WalletDB");
            }
        }

        public class CreateWalletParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Label { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Passphrase { get; set; }


            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public byte[] Seed { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Mnemonic { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public uint? Bip39 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool? Encrypted { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public uint? NumStealthAddresses { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public uint? NumTransparentAddresses { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public List<string> StealthAddressNames { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public List<string> TransparentAddressNames { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool? Save { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool? ScanForBalance { get; set; }

            public CreateWalletParams() { }
        }

        [RPCEndpoint("create_wallet", APISet.WALLET)]
        public static object CreateWallet(CreateWalletParams _params)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (_params == null) return new RPCError("create wallet parameters was null");

                if (_params.Label == null || _params.Label == "") return new RPCError("label was null or empty");

                if (_daemon.wallets.Any(x => x.Label == _params.Label)) return new RPCError("wallet with specified label already exists");

                if ((_params.Bip39.HasValue && (_params.Mnemonic != null || _params.Seed != null)) ||
                    (_params.Mnemonic != null && (_params.Seed != null || _params.Bip39.HasValue)) ||
                    (_params.Seed != null && (_params.Bip39.HasValue || _params.Mnemonic != null))) 
                    return new RPCError("Seed, Mnemonic, and Bip39 are mutually exclusive; only set one of these");

                Cipher.Mnemonics.Mnemonic mnemonic = null;

                if (_params.Mnemonic != null)
                {
                    mnemonic = new(_params.Mnemonic);
                }
                else if (_params.Seed != null)
                {
                    mnemonic = new(_params.Seed);
                }

                bool _encrypted = _params.Encrypted.HasValue ? _params.Encrypted.Value : true;

                if (_encrypted && (_params.Passphrase == null || _params.Passphrase == "")) return new RPCError("passphrase was null or empty and encrypted was true");

                if (_params.NumStealthAddresses.HasValue && _params.StealthAddressNames != null && _params.NumStealthAddresses.Value != _params.StealthAddressNames.Count)
                {
                    return new RPCError($"number of stealth addresses does not match the number of provided stealth address names: {_params.NumStealthAddresses.Value} != {_params.StealthAddressNames.Count}");
                }

                if (_params.NumTransparentAddresses.HasValue && _params.TransparentAddressNames != null && _params.NumTransparentAddresses.Value != _params.TransparentAddressNames.Count)
                {
                    return new RPCError($"number of transparent addresses does not match the number of provided transparent address names: {_params.NumTransparentAddresses.Value} != {_params.TransparentAddressNames.Count}");
                }

                uint numStealthAddresses = _params.NumStealthAddresses.HasValue ? _params.NumStealthAddresses.Value : (_params.StealthAddressNames != null ? (uint)_params.StealthAddressNames.Count : 1);
                uint numTransparentAddresses = _params.NumTransparentAddresses.HasValue ? _params.NumTransparentAddresses.Value : (_params.TransparentAddressNames != null ? (uint)_params.TransparentAddressNames.Count : 0);

                bool _save = _params.Save.HasValue ? _params.Save.Value : false;
                bool _scan = _params.ScanForBalance.HasValue ? _params.ScanForBalance.Value : false;

                if (_scan && !_save) return new RPCError("Save must be true if scan is true");

                Wallet wallet;

                if (mnemonic == null)
                {
                    wallet = new Wallet(_params.Label, _params.Passphrase, _params.Bip39.HasValue ? _params.Bip39.Value : 24, _encrypted, true, numStealthAddresses, numTransparentAddresses, _params.StealthAddressNames, _params.TransparentAddressNames);
                }
                else
                {
                    wallet = new Wallet(_params.Label, _params.Passphrase, mnemonic.GetMnemonic(), _encrypted, true, (uint)mnemonic.Words.Length, numStealthAddresses, numTransparentAddresses, _params.StealthAddressNames, _params.TransparentAddressNames);
                }

                if (_save)
                {
                    _daemon.wallets.Add(wallet);

                    wallet.Save(true);

                    _ = Task.Run(() => _daemon.WalletSyncer(wallet, _scan)).ConfigureAwait(false);
                }

                return new Readable.Wallet(wallet);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CreateWallet failed: {ex}");

                return new RPCError($"Could not create wallet");
            }
        }

        public class LoadWalletParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Path { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Label { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Passphrase { get; set; }

            public LoadWalletParams() { }
        }

        [RPCEndpoint("load_wallet", APISet.WALLET)]
        public static object LoadWallet(LoadWalletParams _params)
        {
            var _daemon = Network.Handler.GetHandler().daemon;

            if (_params == null) 
                return new RPCError("load wallet parameters was null");

            if ((_params.Path == null || _params.Path == "") && (_params.Label == null || _params.Label == "")) 
                return new RPCError("one of the following must be set: Path, Label");

            if (_params.Path != null && _params.Path != "" && _params.Label != null && _params.Label != "") 
                return new RPCError("only one of the following must be set: Path, Label");

            Wallet wallet;

            WalletDB db = WalletDB.GetDB();

            try
            {
                if (_params.Path != null && _params.Path != "")
                {
                    if (!Path.IsPathRooted(_params.Path))
                        return new RPCError("Path must be absolute");

                    wallet = Wallet.FromFile(_params.Path);

                    if (db.ContainsWallet(wallet.Label))
                    {
                        return new RPCError($"Label conflict: walletdb already has a wallet with label {wallet.Label}; consider changing the label");
                    }

                    wallet.Save(true);
                }
                else
                {
                    try
                    {
                        wallet = db.GetWallet(_params.Label);
                    }
                    catch (Exception ex)
                    {
                        Daemon.Logger.Error($"RPC call resulted in an error: {ex}");

                        return new RPCError($"could not load wallet with label {_params.Label}; try checking wallet integrity or seeing if it is missing");
                    }
                }

                if (_daemon.wallets.Any(x => x.Label == wallet.Label || (x.WalletPath == _params.Path && x.WalletPath != null && x.WalletPath != "")))
                    return new RPCError(-1, "wallet shares label or path with another loaded wallet", new Readable.Wallet(wallet));

                if (wallet.Encrypted && (_params.Passphrase == null || _params.Passphrase == ""))
                    return new RPCError("Wallet is encrypted; passphrase was not supplied");

                if (!wallet.TryDecrypt(_params.Passphrase))
                    return new RPCError("Wallet passphrase incorrect");

                wallet.WalletPath = _params.Path;

                _daemon.wallets.Add(wallet);

                _ = Task.Run(() => _daemon.WalletSyncer(wallet, true)).ConfigureAwait(false);

                foreach (var addr in wallet.Addresses)
                {
                    if (addr.Synced == false && addr.Syncer == true)
                    {
                        _ = Task.Run(() => _daemon.AddressSyncer(addr)).ConfigureAwait(false);
                    }
                }

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LoadWallet failed: {ex.Message}");

                return new RPCError($"Could not load wallet");
            }
        }

        public class CheckIntegrityParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Label { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Address { get; set; }

            public CheckIntegrityParams() { }
        }

        [RPCEndpoint("check_integrity", APISet.WALLET)]
        public static object CheckIntegrity(CheckIntegrityParams _params)
        {
            var _daemon = Network.Handler.GetHandler().daemon;

            if (_params == null)
                return new RPCError("check integrity parameters was null");

            if ((_params.Address == null || _params.Address == "") && (_params.Label == null || _params.Label == ""))
                return new RPCError("one of the following must be set: Address, Label");

            if (_params.Address != null && _params.Address != "" && _params.Label != null && _params.Label != "")
                return new RPCError("only one of the following must be set: Address, Label");

            try
            {
                if (_params.Label != null && _params.Label != "")
                {
                    var wallet = _daemon.wallets.Where(x => x.Label == _params.Label).FirstOrDefault();

                    if (wallet == null) return new RPCError($"could not find wallet with label {_params.Label}");

                    return wallet.CheckIntegrity() ? "OK" : "wallet integrity check failed. consider using restore_wallet to attempt recovery.";
                }
                else
                {

                    var wallet = _daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == _params.Address).FirstOrDefault() != null).FirstOrDefault();

                    if (wallet == null) return new RPCError($"could not find address {_params.Address}");

                    return wallet.Addresses.Where(x => x.Address == _params.Address).First().TryCheckIntegrity() 
                        ? "OK" : $"address integrity check failed. consider using restore_wallet on wallet {wallet.Label} to attempt recovery.";
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CheckIntegrity failed: {ex.Message}");

                return new RPCError($"failed");
            }
        }

        public class LoadWalletsParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Label { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Passphrase { get; set; }

            public LoadWalletsParams() { }
        }

        [RPCEndpoint("load_wallets", APISet.WALLET)]
        public static object LoadWallets(List<LoadWalletParams> _params)
        {
            var _daemon = Network.Handler.GetHandler().daemon;

            WalletDB db = WalletDB.GetDB();

            if (_params == null || _params.Count == 0) return new RPCError("load wallets params was null");

            bool dup = _params.Aggregate(new List<string>(), (lst, elem) => { lst.Add(elem.Label); return lst; }).Distinct().Count() < _params.Count;

            if (dup) return new RPCError($"load wallet params contains duplicate wallets to load; cannot load wallets");

            try
            {
                List<Wallet> wallets = new List<Wallet>();

                foreach (var param in _params)
                {
                    if (param == null) return new RPCError("one of the load wallets params was null");

                    if (param.Label == null || param.Label == "") return new RPCError("one of the labels was null");

                    Wallet wallet = db.TryGetWallet(param.Label);

                    if (wallet == null) return new RPCError($"no wallet exists with label {param.Label}");

                    if (wallet.Encrypted)
                    {
                        if (param.Passphrase == null || param.Passphrase == "") return new RPCError($"wallet {param.Label} requires passphrase");

                        if (!wallet.TryDecrypt(param.Passphrase)) return new RPCError($"wallet {param.Label}: wrong passphrase");
                    }
                    else
                    {
                        if (!wallet.TryDecrypt()) return new RPCError($"wallet {param.Label} could not be decrypted");
                    }

                    wallets.Add(wallet);
                }

                wallets.ForEach(wallet =>
                {
                    _daemon.wallets.Add(wallet);

                    _ = Task.Run(() => _daemon.WalletSyncer(wallet, true)).ConfigureAwait(false);

                    foreach (var addr in wallet.Addresses)
                    {
                        if (addr.Synced == false && addr.Syncer == true)
                        {
                            _ = Task.Run(() => _daemon.AddressSyncer(addr)).ConfigureAwait(false);
                        }
                    }
                });

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LoadWallets failed: {ex.Message}");

                return new RPCError($"could not load wallets");
            }
        }

        [RPCEndpoint("lock_wallet", APISet.WALLET)]
        public static object LockWallet(string label)
        {
            try
            {
                if (label == null || label == "") return new RPCError("label was null");

                var wallet = Network.Handler.GetHandler().daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find wallet with label {label}");

                wallet.MustEncrypt();

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LockWallet failed: {ex.Message}");

                return new RPCError($"could not lock wallet");
            }
        }

        [RPCEndpoint("lock_wallets", APISet.WALLET)]
        public static object LockWallets()
        {
            try
            {
                foreach (Wallet wallet in Network.Handler.GetHandler().daemon.wallets)
                {
                    wallet.MustEncrypt();
                }

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LockWallets failed: {ex.Message}");

                return new RPCError($"could not lock wallets");
            }
        }

        public class UnlockWalletParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Label { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Passphrase { get; set; }

            public UnlockWalletParams() { }
        }

        [RPCEndpoint("unlock_wallet", APISet.WALLET)]
        public static object UnlockWallet(UnlockWalletParams _params)
        {
            var _daemon = Network.Handler.GetHandler().daemon;

            if (_params == null) return new RPCError("unlock wallet params was null");

            if (_params.Label == null || _params.Label == "") return new RPCError("label was null");

            try
            {
                Wallet wallet = _daemon.wallets.Where(x => x.Label == _params.Label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {_params.Label}");

                if (!wallet.IsEncrypted) return new RPCError("wallet is not encrypted");

                if (wallet.Encrypted)
                {
                    if (_params.Passphrase == null || _params.Passphrase == "") return new RPCError($"wallet {_params.Label} requires passphrase");

                    if (!wallet.TryDecrypt(_params.Passphrase)) return new RPCError($"wallet {_params.Label}: wrong passphrase");
                }
                else
                {
                    if (!wallet.TryDecrypt()) return new RPCError($"wallet {_params.Label} could not be decrypted");
                }

                _ = Task.Run(() => _daemon.WalletSyncer(wallet, true)).ConfigureAwait(false);

                foreach (var addr in wallet.Addresses)
                {
                    if (addr.Synced == false && addr.Syncer == true)
                    {
                        _ = Task.Run(() => _daemon.AddressSyncer(addr)).ConfigureAwait(false);
                    }
                }

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to UnlockWallet failed: {ex.Message}");

                return new RPCError($"Could not unlock wallet");
            }
        }

        [RPCEndpoint("get_wallet_balance", APISet.WALLET)]
        public static object GetWalletBalance(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {label}");

                return wallet.Addresses.Select(x => { if (x.Synced && !x.Syncer) return x.Balance; else return 0UL; }).Aggregate((x, y) => x + y);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletBalance failed: {ex.Message}");

                return new RPCError($"Could not get wallet balance");
            }
        }

        [RPCEndpoint("get_balance", APISet.WALLET)]
        public static object GetBalance(string address)
        {
            try
            {
                var __daemon = Network.Handler.GetHandler().daemon;

                if (address == null || address == "") return new RPCError("parameter was null");

                var wallet = __daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find address {address}");

                return wallet.Addresses.Where(x => x.Address == address).FirstOrDefault().Balance;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBalance failed: {ex.Message}");

                return new RPCError($"Could not get address balance");
            }
        }

        public class CreateAddressParams
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Label { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Type { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public bool? Deterministic { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Name { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Secret { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Spend { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string View { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool? ScanForBalance { get; set; }
        }

        [RPCEndpoint("create_address", APISet.WALLET)]
        public static object CreateAddress(CreateAddressParams _params)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (_params == null) return new RPCError("unlock wallet params was null");

                if (_params.Label == null || _params.Label == "") return new RPCError("label was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == _params.Label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {_params.Label}");

                if (_params.Type == null || _params.Type == "") return new RPCError("type was null");

                AddressType? addrType = null;

                if (_params.Type.ToLower() == "private" || _params.Type.ToLower() == "shielded" || _params.Type.ToLower() == "stealth" || _params.Type.ToLower() == "stealthaddress" || _params.Type.ToLower() == "p")
                {
                    addrType = AddressType.STEALTH;
                }
                else if (_params.Type.ToLower() == "transparent" || _params.Type.ToLower() == "public" || _params.Type.ToLower() == "t" || _params.Type.ToLower() == "unshielded")
                {
                    addrType = AddressType.TRANSPARENT;
                }

                if (addrType == null) return new RPCError($"unknown address type {_params.Type}");

                if (!_params.Deterministic.HasValue) return new RPCError("deterministic was null");

                bool deterministic = _params.Deterministic.Value;

                if (deterministic && ((_params.Secret != null && _params.Secret != "") ||
                                      (_params.Spend != null && _params.Spend != "") ||
                                      (_params.View != null && _params.View != "")))
                    return new RPCError("Secret, Spend and View cannot be set if deterministic is true");

                WalletAddress addr = null;

                if (deterministic)
                {
                    addr = wallet.AddWallet(true, addrType == AddressType.TRANSPARENT, _params.Name);
                }
                else
                {
                    if (addrType == AddressType.STEALTH && (_params.Secret != null && _params.Secret != ""))
                        return new RPCError($"Secret cannot be set if type is {_params.Type}");

                    if (addrType == AddressType.TRANSPARENT && ((_params.Spend != null && _params.Spend != "") || (_params.View != null && _params.View != "")))
                        return new RPCError($"Spend and View cannot be set if type is {_params.Type}");

                    bool random = !deterministic && (_params.Spend == null || _params.Spend == "") && (_params.View == null || _params.View == "") && (_params.Secret == null || _params.Secret == "");

                    if (random)
                    {
                        addr = wallet.AddWallet(false, addrType == AddressType.TRANSPARENT, _params.Name);
                    }
                    else
                    {
                        if (addrType == AddressType.STEALTH)
                        {
                            Key spend, view;

                            if (Printable.IsHex(_params.Spend) && _params.Spend.Length == 64)
                            {
                                spend = Key.FromHex(_params.Spend);
                            }
                            else if (Mnemonic.IsMnemonic(_params.Spend, 24))
                            {
                                spend = new Key(Mnemonic.GetEntropy(_params.Spend));
                            }
                            else
                            {
                                return new RPCError($"Spend is not a valid key mnemonic or key hex string");
                            }

                            if (Printable.IsHex(_params.View) && _params.View.Length == 64)
                            {
                                view = Key.FromHex(_params.View);
                            }
                            else if (Mnemonic.IsMnemonic(_params.View))
                            {
                                view = new Key(Mnemonic.GetEntropy(_params.View));
                            }
                            else
                            {
                                return new RPCError($"View is not a valid key mnemonic or key hex string");
                            }

                            addr = new WalletAddress(wallet, (byte)addrType, default, spend, view);
                            addr.Name = _params.Name ?? "";
                        }
                        else if (addrType == AddressType.TRANSPARENT)
                        {
                            Key secret;

                            if (Printable.IsHex(_params.Secret) && _params.Secret.Length == 64)
                            {
                                secret = Key.FromHex(_params.Spend);
                            }
                            else if (Mnemonic.IsMnemonic(_params.Secret, 24))
                            {
                                secret = new Key(Mnemonic.GetEntropy(_params.Spend));
                            }
                            else
                            {
                                return new RPCError($"Spend is not a valid key mnemonic or key hex string");
                            }

                            addr = new WalletAddress(wallet, (byte)addrType, secret, default, default);
                            addr.Name = _params.Name ?? "";
                        }

                        wallet.AddWallet(addr);
                    }
                }

                bool scan = _params.ScanForBalance.HasValue ? _params.ScanForBalance.Value : false;

                if (scan)
                {
                    _ = Task.Run(() => _daemon.AddressSyncer(addr)).ConfigureAwait(false);
                }

                return new Readable.WalletAddress(addr);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CreateAddress failed: {ex.Message}");

                return new RPCError($"Could not create address");
            }
        }

        [RPCEndpoint("get_addresses", APISet.WALLET)]
        public static object GetAddresses(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {label}");

                return wallet.Addresses.Select(x => x.Address).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetAddresses failed: {ex.Message}");

                return new RPCError($"Could not get addresses");
            }
        }

        [RPCEndpoint("get_wallets", APISet.WALLET)]
        public static object GetWallets()
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                return _daemon.wallets.Select(x => new Readable.Wallet(x)).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWallets failed: {ex.Message}");

                return new RPCError($"Could not get wallets");
            }
        }

        [RPCEndpoint("stop_wallet", APISet.WALLET)]
        public static object StopWallet(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("label was null");

                var wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find wallet with label {label}");

                wallet.MustEncrypt();

                _daemon.wallets = new ConcurrentBag<Wallet>(_daemon.wallets.Where(x => x.Label != label).ToList());

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to StopWallet failed: {ex.Message}");

                return new RPCError($"could not stop wallet");
            }
        }

        [RPCEndpoint("get_wallet_version", APISet.WALLET)]
        public static object GetWalletVersion(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {label}");

                return wallet.Version;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletVersion failed: {ex.Message}");

                return new RPCError($"Could not get wallet version");
            }
        }

        [RPCEndpoint("change_wallet_label", APISet.WALLET)]
        public static object ChangeWalletLabel(string label, string newLabel)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {label}");

                if (label != newLabel && _daemon.wallets.Where(x => x.Label == newLabel).FirstOrDefault() != null)
                    return new RPCError($"cannot change label to {newLabel}; wallet already loaded with this label");

                return wallet.ChangeLabel(label) ? "OK" : "failed to change wallet label";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to ChangeWalletLabel failed: {ex.Message}");

                return new RPCError($"Could not change wallet label");
            }
        }

        [RPCEndpoint("change_address_name", APISet.WALLET)]
        public static object ChangeAddressName(string address, string name)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                var wallet = _daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find address {address}");

                WalletAddress addr = wallet.Addresses.Where(x => x.Address == address).FirstOrDefault();

                addr.ChangeName(name);

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to ChangeAddressName failed: {ex.Message}");

                return new RPCError($"Could not change address name");
            }
        }

        public enum WalletStatus: int
        {
            UNLOCKED = 0,
            LOCKED = 1,
            UNLOADED = 2,
        }

        [RPCEndpoint("get_wallet_status", APISet.WALLET)]
        public static object GetWalletStatus(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return (int)WalletStatus.UNLOADED;

                return wallet.IsEncrypted ? (int)WalletStatus.LOCKED : (int)WalletStatus.UNLOCKED;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletStatus failed: {ex.Message}");

                return new RPCError($"Could not get wallet status");
            }
        }

        public class WalletStatusRV
        {
            public int Status { get; set; }
            public string Label { get; set; }
        }

        [RPCEndpoint("get_wallet_statuses")]
        public static object GetWalletStatuses()
        {
            try
            {
                var db = WalletDB.GetDB();
                var _daemon = Network.Handler.GetHandler().daemon;
                var wallets = db.GetWallets();
                List<WalletStatusRV> statuses = new();

                foreach (var wallet in wallets)
                {
                    WalletStatus status;
                    if (_daemon.wallets.Where(x => x.Label == wallet.Label).FirstOrDefault() == null)
                    {
                        status = WalletStatus.UNLOADED;
                    }
                    else
                    {
                        status = _daemon.wallets.Where(x => x.Label == wallet.Label).FirstOrDefault().IsEncrypted ? WalletStatus.LOCKED : WalletStatus.UNLOCKED;
                    }

                    statuses.Add(new WalletStatusRV { Label = wallet.Label, Status = (int)status });
                }

                return statuses;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletStatuses failed: {ex.Message}");

                return new RPCError($"Could not get wallet statuses");
            }
        }

        public class GetWalletHeightRV
        {
            public long Height { get; set; }
            public bool Synced { get; set; }
        }

        [RPCEndpoint("get_wallet_height", APISet.WALLET)]
        public static object GetWalletHeight(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");

                Wallet wallet = _daemon.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (wallet == null) return new RPCError($"no wallet found with label {label}");

                return new GetWalletHeightRV { Height = wallet.LastSeenHeight, Synced = wallet.Synced };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletHeight failed: {ex.Message}");

                return new RPCError($"Could not get wallet height");
            }
        }

        public class GetAddressHeightRV
        {
            public long Height { get; set; }
            public bool Synced { get; set; }
            public bool Syncer { get; set; }
        }

        [RPCEndpoint("get_address_height", APISet.WALLET)]
        public static object GetAddressHeight(string address)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                var wallet = _daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find address {address}");

                var addr = wallet.Addresses.Where(x => x.Address == address).FirstOrDefault();

                return new GetAddressHeightRV { Height = addr.LastSeenHeight, Synced = addr.Synced, Syncer = addr.Syncer };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletHeight failed: {ex.Message}");

                return new RPCError($"Could not get wallet height");
            }
        }

        public class GetMnemonicRV
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Mnemonic { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Entropy { get; set; }
        }

        [RPCEndpoint("get_mnemonic", APISet.WALLET)]
        public static object GetMnemonic()
        {
            try
            {
                Mnemonic mnemonic = new Mnemonic(Randomness.Random(32));

                return new GetMnemonicRV
                {
                    Mnemonic = mnemonic.GetMnemonic(),
                    Entropy = Printable.Hexify(mnemonic.GetEntropy())
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetMnemonic failed: {ex.Message}");

                return new RPCError($"Could not get mnemonic");
            }
        }

        public class TransactionHistoryOutput
        {
            public ulong Amount { get; set; }
            public string Address { get; set; }
        }

        public class TransactionHistoryRV
        {
            public string TxID { get; set; }
            public long Timestamp { get; set; }

            public ulong SentAmount { get; set; } // only nonzero if this address net sent coins
            public ulong ReceivedAmount { get; set; } // only nonzero if this address net received coins

            public List<TransactionHistoryOutput> Inputs { get; set; }
            public List<TransactionHistoryOutput> Outputs { get; set; }
        }

        [RPCEndpoint("get_transaction_history", APISet.WALLET)]
        public static object GetTransactionHistory(string address)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                var wallet = _daemon.wallets.Where(x => x.Addresses.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();

                if (wallet == null) return new RPCError($"could not find address {address}");

                var addr = wallet.Addresses.Where(x => x.Address == address).FirstOrDefault();

                if (addr.Encrypted) return new RPCError($"address {address} is encrypted");

                List<TransactionHistoryRV> history = new();
                foreach (var wtx in addr.TxHistory)
                {
                    var htx = new TransactionHistoryRV
                    {
                        TxID = wtx.TxID.ToHex(),
                        Timestamp = wtx.Timestamp,
                        Inputs = wtx.Inputs.Select(x => new TransactionHistoryOutput { Address = x.Address, Amount = x.Amount }).ToList(),
                        Outputs = wtx.Outputs.Select(x => new TransactionHistoryOutput { Address = x.Address, Amount = x.Amount }).ToList(),
                        ReceivedAmount = 0,
                        SentAmount = 0,
                    };

                    long total = 0;
                    foreach (var input in wtx.Inputs)
                    {
                        if (input.Address == address)
                        {
                            total -= (long)input.Amount;
                        }
                    }

                    foreach (var output in wtx.Outputs)
                    {
                        if (output.Address == address)
                        {
                            total += (long)output.Amount;
                        }
                    }

                    if (total > 0)
                    {
                        htx.ReceivedAmount = (ulong)total;
                    }
                    else
                    {
                        htx.SentAmount = (ulong)(-total);
                    }

                    history.Add(htx);
                }

                return history;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetTransactionHistory failed: {ex.Message}");

                return new RPCError($"Could not get transaction history");
            }
        }
    }
}
