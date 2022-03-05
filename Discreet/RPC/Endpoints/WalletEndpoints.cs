using Discreet.Cipher;
using Discreet.Coin;
using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace Discreet.RPC.Endpoints
{
    public static class WalletEndpoints
    {
        [RPCEndpoint("get_wallet", APISet.WALLET)]
        public static object GetWallet(string label)
        {
            try
            {
                var _wallet = Network.Handler.GetHandler().visor.wallets.Where(x => x.Label == label).FirstOrDefault();

                if (_wallet == null)
                {
                    return new RPCError($"could not get wallet with label {label}");
                }

                return new Readable.Wallet(_wallet);
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to GetWallet failed: {ex}");

                return new RPCError($"Could not get wallet {label}");
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
                var _visor = Network.Handler.GetHandler().visor;

                if (_params == null) return new RPCError("create wallet parameters was null");

                if (_params.Label == null || _params.Label == "") return new RPCError("label was null or empty");

                if (_visor.wallets.Any(x => x.Label == _params.Label)) return new RPCError("label already exists");

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


                uint numStealthAddresses = _params.NumStealthAddresses.HasValue ? _params.NumStealthAddresses.Value : 1;
                uint numTransparentAddresses = _params.NumTransparentAddresses.HasValue ? _params.NumTransparentAddresses.Value : 0;

                bool _save = _params.Save.HasValue ? _params.Save.Value : false;
                bool _scan = _params.ScanForBalance.HasValue ? _params.ScanForBalance.Value : false;

                if (_scan && !_save) return new RPCError("Save must be true if scan is true");

                Wallets.Wallet wallet;

                if (mnemonic == null)
                {
                    wallet = new Wallets.Wallet(_params.Label, _params.Passphrase, _params.Bip39.HasValue ? _params.Bip39.Value : 24, _encrypted, true, numStealthAddresses, numTransparentAddresses);
                }
                else
                {
                    wallet = new Wallets.Wallet(_params.Label, _params.Passphrase, mnemonic.GetMnemonic(), _encrypted, true, (uint)mnemonic.Words.Length, numStealthAddresses, numTransparentAddresses);
                }

                if (_save)
                {
                    _visor.wallets.Add(wallet);

                    wallet.ToFile(Path.Join(Visor.VisorConfig.GetConfig().WalletPath, $"{wallet.Label}.dis"));

                    _ = _visor.WalletSyncer(wallet, _scan);
                }

                return new Readable.Wallet(wallet);
            }
            catch (Exception ex)
            {
                Visor.Logger.Error($"RPC call to CreateWallet failed: {ex}");

                return new RPCError($"Could not create wallet");
            }
        }
    }
}
