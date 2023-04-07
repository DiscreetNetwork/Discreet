using Discreet.Cipher;
using Discreet.Cipher.Mnemonics;
using Discreet.Coin;
using Discreet.Common;
using Discreet.RPC.Common;
using Discreet.Wallets;
using Discreet.WalletsLegacy;
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
        public static async Task<object> GetWallet(string label)
        {
            try
            {
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);

                if (!success)
                {
                    return new RPCError($"could not get wallet with label {label}");
                }

                return await wallet.DoGetWallet();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWallet failed: {ex.Message}", ex);

                return new RPCError($"Could not get wallet {label}");
            }
        }

        [RPCEndpoint("get_wallets_from_db", APISet.WALLET)]
        public static object GetWalletsFromDb()
        {
            try
            {
                return SQLiteWallet.DoGetWalletsFromDir();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletsFromDb failed: {ex.Message}", ex);

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
        public static async Task<object> CreateWallet(CreateWalletParams _params)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (_params == null) return new RPCError("create wallet parameters was null");

                if (_params.Label == null || _params.Label == "") return new RPCError("label was null or empty");

                bool _encrypted = _params.Encrypted.HasValue ? _params.Encrypted.Value : true;

                if (_encrypted && (_params.Passphrase == null || _params.Passphrase == "")) return new RPCError("passphrase was null or empty and encrypted was true");

                if (SQLiteWallet.DoGetWalletsFromDir().Concat(SQLiteWallet.Wallets.Keys).Any(x => x == _params.Label)) return new RPCError("wallet with specified label already exists");

                Wallets.Models.CreateWalletParameters cwParams = new(_params.Label, _params.Passphrase ?? "");
                if (_encrypted) cwParams = cwParams.SetEncrypted();

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

                if (mnemonic != null) cwParams = cwParams.SetMnemonic(mnemonic.GetMnemonic());

                if (_params.NumStealthAddresses.HasValue && _params.StealthAddressNames != null && _params.NumStealthAddresses.Value != _params.StealthAddressNames.Count)
                {
                    return new RPCError($"number of stealth addresses does not match the number of provided stealth address names: {_params.NumStealthAddresses.Value} != {_params.StealthAddressNames.Count}");
                }

                if (_params.NumTransparentAddresses.HasValue && _params.TransparentAddressNames != null && _params.NumTransparentAddresses.Value != _params.TransparentAddressNames.Count)
                {
                    return new RPCError($"number of transparent addresses does not match the number of provided transparent address names: {_params.NumTransparentAddresses.Value} != {_params.TransparentAddressNames.Count}");
                }

                if (_params.StealthAddressNames != null && _params.StealthAddressNames.Count > 0)
                {
                    _params.StealthAddressNames.ForEach(x => cwParams = cwParams.AddStealthAddress(x));
                }
                else
                {
                    cwParams = cwParams.SetNumStealthAddresses(_params.NumStealthAddresses.Value);
                }

                if (_params.TransparentAddressNames != null && _params.TransparentAddressNames.Count > 0)
                {
                    _params.TransparentAddressNames.ForEach(x => cwParams = cwParams.AddTransparentAddress(x));
                }
                else
                {
                    cwParams = cwParams.SetNumTransparentAddresses(_params.NumTransparentAddresses.Value);
                }

                bool _save = _params.Save.HasValue ? _params.Save.Value : false;
                bool _scan = _params.ScanForBalance.HasValue ? _params.ScanForBalance.Value : false;

                if (_params.ScanForBalance.HasValue && _params.ScanForBalance.Value == true) cwParams = cwParams.Scan();
                else cwParams = cwParams.SkipScan();

                if (_scan && !_save) return new RPCError("Save must be true if scan is true");

                SQLiteWallet wallet = SQLiteWallet.CreateWallet(cwParams);
                return await wallet.DoGetWallet();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CreateWallet failed: {ex.Message}", ex);

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

            try
            {
                if (_params.Path != null && _params.Path != "")
                {
                    if (!Path.IsPathRooted(_params.Path))
                        return new RPCError("Path must be absolute");

                    SQLiteWallet.OpenWallet(_params.Path, _params.Passphrase);
                }
                else
                {
                    SQLiteWallet.OpenWallet(_params.Label, _params.Passphrase);
                }

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LoadWallet failed: {ex.Message}", ex);

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
        public static async Task<object> CheckIntegrity(CheckIntegrityParams _params)
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
                    var success = SQLiteWallet.Wallets.TryGetValue(_params.Label, out var wallet);
                    if (!success) return new RPCError($"could not find wallet with label {_params.Label}");

                    return await wallet.DoCheckIntegrity() ? "OK" : "wallet integrity check failed.";
                }
                else
                {
                    var wallet = SQLiteWallet.Wallets.Values.Where(x => x.Accounts.Any(y => y.Address == _params.Address)).FirstOrDefault();
                    if (wallet == null) return new RPCError($"could not find address {_params.Address}");

                    return await wallet.DoCheckAccountIntegrity(_params.Address)
                        ? "OK" : $"address integrity check failed. consider using restore_wallet on wallet {wallet.Label} to attempt recovery.";
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CheckIntegrity failed: {ex.Message}", ex);

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
        public static async Task<object> LoadWallets(List<LoadWalletParams> _params)
        {
            var _daemon = Network.Handler.GetHandler().daemon;

            if (_params == null || _params.Count == 0) return new RPCError("load wallets params was null");
            bool dup = _params.Aggregate(new List<string>(), (lst, elem) => { lst.Add(elem.Label); return lst; }).Distinct().Count() < _params.Count;
            if (dup) return new RPCError($"load wallet params contains duplicate wallets to load; cannot load wallets");
            List<SQLiteWallet> wallets = new();
            try
            {
                foreach (var param in _params)
                {
                    if (param == null) return new RPCError("one of the load wallets params was null");
                    if (param.Label == null || param.Label == "") return new RPCError("one of the labels was null");

                    var wallet = SQLiteWallet.OpenWallet(param.Label, param.Passphrase);
                    wallets.Add(wallet);
                }

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LoadWallets failed: {ex.Message}", ex);

                return new RPCError($"could not load wallets");
            }
        }

        [RPCEndpoint("lock_wallet", APISet.WALLET)]
        public static async Task<object> LockWallet(string label)
        {
            try
            {
                if (label == null || label == "") return new RPCError("label was null");
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"could not find wallet with label {label}");

                await wallet.DoLockWallet();

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LockWallet failed: {ex.Message}", ex);

                return new RPCError($"could not lock wallet");
            }
        }

        [RPCEndpoint("lock_wallets", APISet.WALLET)]
        public static async Task<object> LockWallets()
        {
            try
            {
                await Task.WhenAll(SQLiteWallet.Wallets.Values.ToList().Select(async (x) => await x.DoLockWallet()));

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to LockWallets failed: {ex.Message}", ex);

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
        public static async Task<object> UnlockWallet(UnlockWalletParams _params)
        {
            var _daemon = Network.Handler.GetHandler().daemon;

            if (_params == null) return new RPCError("unlock wallet params was null");
            if (_params.Label == null || _params.Label == "") return new RPCError("label was null");

            try
            {
                bool success = SQLiteWallet.Wallets.TryGetValue(_params.Label, out var wallet);
                if (!success) return new RPCError($"no wallet found with label {_params.Label}");

                success = await wallet.Unlock(_params.Passphrase);
                if (!success) return new RPCError($"wrong passphrase!");
                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to UnlockWallet failed: {ex.Message}", ex);

                return new RPCError($"Could not unlock wallet");
            }
        }

        [RPCEndpoint("get_wallet_balance", APISet.WALLET)]
        public static async Task<object> GetWalletBalance(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");
                bool success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"no wallet found with label {label}");

                return await wallet.DoGetBalance();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletBalance failed: {ex.Message}", ex);

                return new RPCError($"Could not get wallet balance");
            }
        }

        [RPCEndpoint("get_balance", APISet.WALLET)]
        public static async Task<object> GetBalance(string address)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (address == null || address == "") return new RPCError("parameter was null");
                var wallet = SQLiteWallet.Wallets.Values.Where(x => x.Accounts.Any(y => y.Address == address)).FirstOrDefault();
                if (wallet == null) return new RPCError($"could not find address {address}");

                return await wallet.DoGetAccountBalance(address);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetBalance failed: {ex.Message}", ex);

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
        public static async Task<object> CreateAddress(CreateAddressParams _params)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (_params == null) return new RPCError("unlock wallet params was null");
                if (_params.Label == null || _params.Label == "") return new RPCError("label was null");

                var success = SQLiteWallet.Wallets.TryGetValue(_params.Label, out var wallet);
                if (!success) return new RPCError($"no wallet found with label {_params.Label}");

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
                var caParams = new Wallets.Models.CreateAccountParameters(_params.Name);
                if (addrType.Value == AddressType.STEALTH) caParams = caParams.Stealth();
                else if (addrType.Value == AddressType.TRANSPARENT) caParams = caParams.Transparent();

                if (!_params.Deterministic.HasValue) return new RPCError("deterministic was null");
                bool deterministic = _params.Deterministic.Value;
                if (deterministic) caParams = caParams.SetDeterministic();
                else caParams = caParams.SetNondeterministic();

                if (deterministic && ((_params.Secret != null && _params.Secret != "") ||
                                      (_params.Spend != null && _params.Spend != "") ||
                                      (_params.View != null && _params.View != "")))
                    return new RPCError("Secret, Spend and View cannot be set if deterministic is true");

                if (_params.Secret != null)
                {
                    if (Printable.IsHex(_params.Secret) && _params.Secret.Length == 64) caParams = caParams.SetTransparentSeed(_params.Secret);
                    else caParams = caParams.SetTransparentMnemonic(_params.Secret);
                }
                else if (_params.Spend != null && _params.View != null)
                {
                    Mnemonic spend, view;
                    if (Printable.IsHex(_params.Spend) && _params.Spend.Length == 64) spend = new(Printable.Byteify(_params.Spend));
                    else spend = new(_params.Spend);

                    if (Printable.IsHex(_params.View) && _params.View.Length == 64) view = new(Printable.Byteify(_params.View));
                    else view = new(_params.View);

                    caParams = caParams.SetStealthMnemonics(spend.GetMnemonic(), view.GetMnemonic());
                }

                bool scan = _params.ScanForBalance.HasValue ? _params.ScanForBalance.Value : false;
                caParams = caParams.SetScan(scan);

                return await wallet.DoCreateAccount(caParams);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to CreateAddress failed: {ex.Message}", ex);

                return new RPCError($"Could not create address");
            }
        }

        [RPCEndpoint("get_addresses", APISet.WALLET)]
        public static async Task<object> GetAddresses(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("parameter was null");
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"no wallet found with label {label}");

                return await wallet.DoGetAccounts();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetAddresses failed: {ex.Message}", ex);

                return new RPCError($"Could not get addresses");
            }
        }

        [RPCEndpoint("get_wallets", APISet.WALLET)]
        public static async Task<object> GetWallets()
        {
            try
            {
                return await Task.WhenAll(SQLiteWallet.Wallets.Values.Select(async (wallet) => await wallet.DoGetWallet()));
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWallets failed: {ex.Message}", ex);

                return new RPCError($"Could not get wallets");
            }
        }

        [RPCEndpoint("stop_wallet", APISet.WALLET)]
        public static async Task<object> StopWallet(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("label was null");
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"could not find wallet with label {label}");
                
                success = await wallet.DoUnload();

                return success ? "OK" : throw new Exception("unknown failure");
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to StopWallet failed: {ex.Message}", ex);

                return new RPCError($"could not stop wallet");
            }
        }

        [RPCEndpoint("get_wallet_version", APISet.WALLET)]
        public static object GetWalletVersion(string label)
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;

                if (label == null || label == "") return new RPCError("label was null");
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"could not find wallet with label {label}");

                return wallet.Version;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletVersion failed: {ex.Message}", ex);

                return new RPCError($"Could not get wallet version");
            }
        }

        [RPCEndpoint("change_wallet_label", APISet.WALLET)]
        public static async Task<object> ChangeWalletLabel(string label, string newLabel)
        {
            try
            {
                if (label == null || label == "") return new RPCError("label was null");
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"could not find wallet with label {label}");

                return await wallet.DoChangeLabel(label) ? "OK" : "failed to change wallet label";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to ChangeWalletLabel failed: {ex.Message}", ex);

                return new RPCError($"Could not change wallet label");
            }
        }

        [RPCEndpoint("change_address_name", APISet.WALLET)]
        public static async Task<object> ChangeAddressName(string address, string name)
        {
            try
            {
                var wallet = SQLiteWallet.Wallets.Values.Where(x => x.Accounts.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();
                if (wallet == null) return new RPCError($"could not find address {address}");

                await wallet.DoChangeAccountName(address, name);
                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to ChangeAddressName failed: {ex.Message}", ex);

                return new RPCError($"Could not change address name");
            }
        }

        [RPCEndpoint("get_wallet_status", APISet.WALLET)]
        public static object GetWalletStatus(string label)
        {
            try
            {
                return SQLiteWallet.GetWalletStatus(label);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletStatus failed: {ex.Message}", ex);

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
                var statuses = SQLiteWallet.DoGetWalletStatuses();
                return statuses.Select(x => new WalletStatusRV { Label = x.Item1, Status = x.Item2}).ToList();
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletStatuses failed: {ex.Message}", ex);

                return new RPCError($"Could not get wallet statuses");
            }
        }

        public class GetWalletHeightRV
        {
            public long Height { get; set; }
            public bool Synced { get; set; }
        }

        [RPCEndpoint("get_wallet_height", APISet.WALLET)]
        public static async Task<object> GetWalletHeight(string label)
        {
            try
            {
                if (label == null || label == "") return new RPCError("label was null");
                var success = SQLiteWallet.Wallets.TryGetValue(label, out var wallet);
                if (!success) return new RPCError($"could not find wallet with label {label}");

                (bool synced, long height) = await wallet.DoGetHeight();
                return new GetWalletHeightRV { Height = height, Synced = synced };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletHeight failed: {ex.Message}", ex);

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
        public static async Task<object> GetAddressHeight(string address)
        {
            try
            {
                var wallet = SQLiteWallet.Wallets.Values.Where(x => x.Accounts.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();
                if (wallet == null) return new RPCError($"could not find address {address}");

                (bool synced, bool syncer, long height) = await wallet.DoGetAccountHeight(address);
                return new GetAddressHeightRV { Height = height, Synced = synced, Syncer = syncer };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetWalletHeight failed: {ex.Message}", ex);

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
                Daemon.Logger.Error($"RPC call to GetMnemonic failed: {ex.Message}", ex);

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
        public static async Task<object> GetTransactionHistory(string address)
        {
            try
            {
                var wallet = SQLiteWallet.Wallets.Values.Where(x => x.Accounts.Where(y => y.Address == address).FirstOrDefault() != null).FirstOrDefault();
                if (wallet == null) return new RPCError($"could not find address {address}");

                return await wallet.DoGetTransactionHistory(address);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetTransactionHistory failed: {ex.Message}", ex);

                return new RPCError($"Could not get transaction history");
            }
        }

        [RPCEndpoint("migrate_legacy_wallet", APISet.WALLET)]
        public static object MigrateLegacyWallet(string label, string passphrase)
        {
            try
            {
                bool success = SQLiteWallet.WalletMigration.Migrate(label, passphrase);
                if (!success) return new RPCError("could not migrate legacy wallet");

                return "OK";
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to MigrateLegacyWallet failed: {ex.Message}", ex);

                return new RPCError($"Could not migrate legacy wallet");
            }
        }
    }
}
