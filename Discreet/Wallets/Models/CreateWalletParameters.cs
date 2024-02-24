using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Wallets.Extensions;

namespace Discreet.Wallets.Models
{
    public class CreateWalletParameters
    {
        public string Label { get; set; }
        public string Passphrase { get; set; }

        public uint BIP39 { get; set; } = 24;
        public string? Mnemonic { get; set; }

        public bool Encrypted { get; set; } = true;
        public bool Deterministic { get; set; } = true;

        public uint NumStealthAddresses { get; set; } = 1;
        public uint NumTransparentAddresses { get; set; } = 0;
        public List<string> StealthAddressNames { get; set; } = new();
        public List<string> TransparentAddressNames { get; set; } = new();

        public bool ScanForBalance { get; set; } = false;

        public CreateWalletParameters(string label, string passphrase) { Label = label; Passphrase = passphrase; }

        public CreateWalletParameters SetEncrypted() { Encrypted = true; return this; }
        public CreateWalletParameters SetUnencrypted() { Encrypted = false; return this; }

        public CreateWalletParameters SetDeterministic() { Deterministic = true; return this; }
        public CreateWalletParameters SetNondeterministic() { Deterministic = false; return this; }

        public CreateWalletParameters SetBIP39(uint bip39)
        {
            if (bip39 != 12 && bip39 != 24)
            {
                throw new Exception($"Cannot make wallet with seed phrase length {bip39} (only 12 and 24)");
            }

            Mnemonic = null;
            return this;
        }

        public CreateWalletParameters SetSeed(string seed)
        {
            if (seed == null || !seed.IsHex() || (seed.Length != 32 && seed.Length != 64))
            {
                throw new ArgumentException(nameof(seed));
            }

            Mnemonic = new Discreet.Cipher.Mnemonics.Mnemonic(seed.HexToBytes()).GetMnemonic();
            return this;
        }

        public CreateWalletParameters SetMnemonic(string mnemonic)
        {
            var _mnem = new Discreet.Cipher.Mnemonics.Mnemonic(mnemonic);
            if (_mnem.GetEntropy().Length != 16 && _mnem.GetEntropy().Length != 32)
            {
                throw new ArgumentException(nameof(mnemonic));
            }

            Mnemonic = _mnem.GetMnemonic();
            return this;
        }

        public CreateWalletParameters SetNumStealthAddresses(uint numStealthAddresses)
        {
            if (NumStealthAddresses == StealthAddressNames.Count && StealthAddressNames.Count > 0)
            {
                throw new Exception($"field {nameof(NumStealthAddresses)} must equal {nameof(StealthAddressNames)}.Count when greater than 0");
            }

            NumStealthAddresses = numStealthAddresses;
            return this;
        }

        public CreateWalletParameters SetNumTransparentAddresses(uint numTransparentAddresses)
        {
            if (NumTransparentAddresses == TransparentAddressNames.Count && TransparentAddressNames.Count > 0)
            {
                throw new Exception($"field {nameof(NumTransparentAddresses)} must equal {nameof(TransparentAddressNames)}.Count when greater than 0");
            }

            NumTransparentAddresses = numTransparentAddresses;
            return this;
        }

        public CreateWalletParameters AddStealthAddress(string name)
        {
            if (NumStealthAddresses != StealthAddressNames.Count)
            {
                NumStealthAddresses = (uint)StealthAddressNames.Count;
            }

            StealthAddressNames.Add(name);
            NumStealthAddresses++;
            return this;
        }

        public CreateWalletParameters AddTransparentAddress(string name)
        {
            if (NumTransparentAddresses != TransparentAddressNames.Count)
            {
                NumTransparentAddresses = (uint)TransparentAddressNames.Count;
            }

            TransparentAddressNames.Add(name);
            NumTransparentAddresses++;
            return this;
        }

        public CreateWalletParameters SkipScan() { ScanForBalance = false; return this; }
        public CreateWalletParameters Scan() { ScanForBalance = true; return this; }
    }
}
