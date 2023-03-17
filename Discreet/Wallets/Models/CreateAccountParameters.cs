using Discreet.Cipher.Mnemonics;
using Discreet.Wallets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Models
{
    public class CreateAccountParameters
    {
        public byte Type { get; set; } = 0;
        public bool Deterministic { get; set; } = false;
        public string Name { get; set; }
        public byte[]? Secret { get; set; } = null; 
        public byte[]? Spend { get; set; } = null;
        public byte[]? View { get; set; } = null;
        public bool ScanForBalance { get; set; } = false;
        public bool Save { get; set; } = false;

        public CreateAccountParameters(string name) { Name = name; }

        public CreateAccountParameters SetDeterministic() { Deterministic = true; return this; }
        public CreateAccountParameters SetNondeterministic() { Deterministic = false; return this; }
        public CreateAccountParameters Stealth() 
        { 
            Type = 0;
            if (Secret != null) Secret = null;

            return this;
        }

        public CreateAccountParameters Transparent()
        {
            Type = 1;
            if (Spend != null) Spend = null;
            if (View != null) View = null;

            return this;
        }

        public CreateAccountParameters SetTransparentMnemonic(string mnemonic)
        {
            var _mnem = new Mnemonic(mnemonic);
            if (_mnem.GetEntropy().Length != 32)
            {
                throw new ArgumentException(nameof(mnemonic));
            }

            Deterministic = false;
            Type = 1;
            if (Spend != null) Spend = null;
            if (View != null) View = null;
            Secret = _mnem.GetEntropy();

            return this;
        }

        public CreateAccountParameters SetStealthMnemonics(string mnemonicSpend, string mnemonicView)
        {
            var _mnemSpend = new Mnemonic(mnemonicSpend);
            if (_mnemSpend.GetEntropy().Length != 32)
            {
                throw new ArgumentException(nameof(mnemonicSpend));
            }

            var _mnemView = new Mnemonic(mnemonicView);
            if (_mnemView.GetEntropy().Length != 32)
            {
                throw new ArgumentException(nameof(mnemonicView));
            }

            Deterministic = false;
            Type = 0;
            if (Secret != null) Secret = null;
            Spend = _mnemSpend.GetEntropy();
            View = _mnemView.GetEntropy();

            return this;
        }

        public CreateAccountParameters SetTransparentSeed(string seed)
        {
            if (seed == null || !seed.IsHex() || seed.Length != 64)
            {
                throw new ArgumentException(nameof(seed));
            }

            Deterministic = false;
            Type = 1;
            if (Spend != null) Spend = null;
            if (View != null) View = null;
            Secret = new Mnemonic(seed.HexToBytes()).GetEntropy();

            return this;
        }

        public CreateAccountParameters SetStealthSeeds(string seedSpend, string seedView)
        {
            if (seedSpend == null || !seedSpend.IsHex() || seedSpend.Length != 64)
            {
                throw new ArgumentException(nameof(seedSpend));
            }

            if (seedView == null || !seedView.IsHex() || seedView.Length != 64)
            {
                throw new ArgumentException(nameof(seedView));
            }

            Deterministic = false;
            Type = 0;
            if (Secret != null) Secret = null;
            Spend = new Mnemonic(seedSpend.HexToBytes()).GetEntropy();
            View = new Mnemonic(seedView.HexToBytes()).GetEntropy();

            return this;
        }

        public CreateAccountParameters SkipScan() { ScanForBalance = false; return this; }
        public CreateAccountParameters Scan() { ScanForBalance = true; return this; }

        public CreateAccountParameters SetScan(bool scan) { ScanForBalance = scan; return this; }
        public CreateAccountParameters SetSave() { Save = true; return this; }
        public CreateAccountParameters NoSave() { Save = false; return this; }
    }
}
