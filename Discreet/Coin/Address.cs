using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Common;
using System.IO;
using Discreet.Common.Exceptions;
using Discreet.Coin.Script;
using Discreet.Common.Serialize;
using Discreet.Cipher;
using Discreet.Cipher.Extensions;

namespace Discreet.Coin
{
    public enum AddressType: byte
    {
        STEALTH = 0,
        TRANSPARENT = 1,
    }

    public interface IAddress
    {
        public byte[] Bytes();
        public string ToString();
        public byte[] Checksum();
        public byte Version();
        public uint Size();

        public byte Type();
    }

    /* standard versioner for addresses */
    public static class AddressVersion
    {
        public static byte VERSION = 1;
        public static byte SCRIPT_VERSION = 2;
    }
    
    /**
     * TAddress is the Discreet Transparent address class.
     */
    public class TAddress: IAddress
    {
        public byte version;

        public Cipher.RIPEMD160 hash;

        public byte[] checksum;

        public TAddress(Cipher.Key pk)
        {
            version = AddressVersion.VERSION;

            hash = Cipher.RIPEMD160.HashData(Cipher.SHA256.HashData(pk.bytes).Bytes);

            byte[] chk = new byte[21];
            chk[0] = version;
            Array.Copy(hash.Bytes, 0, chk, 1, 20);

            checksum = Cipher.Base58.GetCheckSum(chk);
        }

        public TAddress()
        {
            version = 0;
            hash = new Cipher.RIPEMD160(new byte[20], false);
            checksum = new byte[4];
        }

        public TAddress(byte[] bytes)
        {
            version = bytes[0];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        public TAddress(byte[] bytes, uint offset)
        {
            version = bytes[offset];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, offset + 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            Array.Copy(bytes, offset + 21, checksum, 0, 4);
        }

        public TAddress(Stream s)
        {
            version = (byte)s.ReadByte();
            byte[] _hash = new byte[20];
            s.Read(_hash);
            hash = new Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            s.Read(checksum);
        }

        public TAddress(string addr)
        {
            byte[] bytes = Cipher.Base58.DecodeWhole(addr);

            if (bytes.Length != Size())
            {
                throw new Exception($"Coin.TAddress: Cannot make address from byte array of size {bytes.Length}, requires byte array of size {Size()} (attempted to make from string: \"{addr}\")");
            }

            version = bytes[0];

            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);

            checksum = new byte[4];
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        public uint Size()
        {
            return 25;
        }

        public byte[] Bytes()
        {
            byte[] rv = new byte[Size()];

            rv[0] = version;
            Array.Copy(hash.Bytes, 0, rv, 1, 20);
            Array.Copy(checksum, 0, rv, 21, 4);

            return rv;
        }

        public void FromBytes(byte[] bytes)
        {
            version = bytes[0];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        public override string ToString()
        {
            return Cipher.Base58.EncodeWhole(Bytes());
        }

        public byte[] Checksum()
        {
            return checksum;
        }

        public byte Version()
        {
            return version;
        }

        public byte Type()
        {
            return (byte)AddressType.TRANSPARENT;
        }

        public VerifyException Verify()
        {
            if (version != 1 && version != 2)
            {
                return new VerifyException("TAddress", $"transparent address version not supported! (supports {AddressVersion.VERSION}; got {version})");
            }

            byte[] chk = new byte[21];
            chk[0] = version;
            Array.Copy(hash.Bytes, 0, chk, 1, 20);

            var chksum = Cipher.Base58.GetCheckSum(chk);

            var chksumStr = Printable.Hexify(chksum);
            var checksumStr = Printable.Hexify(checksum);

            if (chksumStr != checksumStr)
            {
                return new VerifyException("TAddress", $"transparent address checksums not equal! (calculated as {chksumStr}; but got {checksumStr})");
            }

            return null;
        }

        public bool CheckAddressBytes(Cipher.Key pk)
        {
            var pkh = Cipher.RIPEMD160.HashData(Cipher.SHA256.HashData(pk.bytes).Bytes);
            return hash.Equals(pkh);
        }

        public bool CheckAddressBytes(RIPEMD160 pkh)
        {
            return hash.Equals(pkh);
        }

        public (bool, int) CheckAddressBytes(RIPEMD160[] pkh)
        {
            for (int i = 0; i < pkh.Length; i++)
            {
                if (CheckAddressBytes(pkh[i]))
                {
                    return (true, i);
                }
            }

            return (false, -1);
        }

        public static RIPEMD160 CreateAddressBytes(Cipher.Key pk)
        {
            return RIPEMD160.HashData(Cipher.SHA256.HashData(pk.bytes).Bytes);
        }

        public static bool operator ==(TAddress a, TAddress b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.version == b.version && a.hash.Equals(b.hash) && a.checksum.BEquals(b.checksum);
        }

        public static bool operator !=(TAddress a, TAddress b) => !(a == b);

        public ScriptAddress ToScriptAddress()
        {
            return new ScriptAddress { checksum = checksum, version = version, hash = hash };
        }
    }

    public class ScriptAddress : TAddress
    {
        public ScriptAddress() : base()
        {
            version = AddressVersion.SCRIPT_VERSION;
            hash = new Cipher.RIPEMD160(new byte[20], false);
            checksum = new byte[4];
        }

        public ScriptAddress(ChainScript script)
        {
            version = AddressVersion.SCRIPT_VERSION;

            var _hash = Cipher.SHA256.HashData(script.Serialize());
            hash = Cipher.RIPEMD160.HashData(_hash.Bytes);

            byte[] chk = new byte[21];
            chk[0] = version;
            Array.Copy(hash.Bytes, 0, chk, 1, 20);

            checksum = Cipher.Base58.GetCheckSum(chk);
        }

        public ScriptAddress(byte[] bytes) : base(bytes)
        {

        }

        public ScriptAddress(byte[] bytes, uint offset) : base(bytes, offset)
        {

        }

        public ScriptAddress(Stream s) : base(s)
        {

        }

        public ScriptAddress(string addr) : base(addr)
        {

        }

        public new VerifyException Verify()
        {
            if (version != AddressVersion.SCRIPT_VERSION)
            {
                return new VerifyException("ScriptAddress", $"script address version not supported! (supports {AddressVersion.SCRIPT_VERSION}; got {version})");
            }

            byte[] chk = new byte[21];
            chk[0] = version;
            Array.Copy(hash.Bytes, 0, chk, 1, 20);

            var chksum = Cipher.Base58.GetCheckSum(chk);

            var chksumStr = Printable.Hexify(chksum);
            var checksumStr = Printable.Hexify(checksum);

            if (chksumStr != checksumStr)
            {
                return new VerifyException("ScriptAddress", $"script address checksums not equal! (calculated as {chksumStr}; but got {checksumStr})");
            }

            return null;
        }
    }

    /**
     * StealthAddress is the Discreet shielded/private address class (i.e. dual key wallet).
     */
    public class StealthAddress : IAddress
    {
        public byte version;

        public Cipher.Key spend;

        public Cipher.Key view;

        public byte[] checksum;

        public StealthAddress(Cipher.Key vk, Cipher.Key sk)
        {
            version = AddressVersion.VERSION;

            view = vk;
            spend = sk;

            byte[] chk = new byte[65];
            chk[0] = version;
            Array.Copy(spend.bytes, 0, chk, 1, 32);
            Array.Copy(view.bytes, 0, chk, 33, 32);

            checksum = Cipher.Base58.GetCheckSum(chk);
        }

        public StealthAddress(byte[] bytes)
        {
            if (bytes.Length != Size())
            {
                throw new Exception($"Coin.StealthAddress: Cannot make stealth address from byte array of size {bytes.Length}, requires byte array of size {Size()}");
            }
            version = bytes[0];

            spend = new Cipher.Key(new byte[32]);
            view = new Cipher.Key(new byte[32]);
            checksum = new byte[4];

            Array.Copy(bytes, 1, spend.bytes, 0, 32);
            Array.Copy(bytes, 33, view.bytes, 0, 32);
            Array.Copy(bytes, 65, checksum, 0, 4);
        }

        public StealthAddress(byte[] bytes, uint offset)
        {
            version = bytes[offset];

            spend = new Cipher.Key(new byte[32]);
            view = new Cipher.Key(new byte[32]);
            checksum = new byte[4];

            Array.Copy(bytes, offset + 1, spend.bytes, 0, 32);
            Array.Copy(bytes, offset + 33, view.bytes, 0, 32);
            Array.Copy(bytes, offset + 65, checksum, 0, 4);
        }

        public StealthAddress(Stream s)
        {
            version = (byte) s.ReadByte();

            spend = new Cipher.Key(s);
            view = new Cipher.Key(s);
            checksum = new byte[4];

            s.Read(checksum);
        }

        public StealthAddress(string data)
        {
            byte[] bytes = Cipher.Base58.Decode(data);

            if (bytes.Length != Size())
            {
                throw new Exception($"Coin.StealthAddress: Cannot make stealth address from byte array of size {bytes.Length}, requires byte array of size {Size()} (attempted to make from string: \"{data}\")");
            }

            version = bytes[0];

            spend = new Cipher.Key(new byte[32]);
            view = new Cipher.Key(new byte[32]);
            checksum = new byte[4];

            Array.Copy(bytes, 1, spend.bytes, 0, 32);
            Array.Copy(bytes, 33, view.bytes, 0, 32);
            Array.Copy(bytes, 65, checksum, 0, 4);
        }

        public uint Size()
        {
            return 69;
        }

        public byte[] Bytes()
        {
            byte[] rv = new byte[Size()];

            rv[0] = version;
            Array.Copy(spend.bytes, 0, rv, 1, 32);
            Array.Copy(view.bytes, 0, rv, 33, 32);
            Array.Copy(checksum, 0, rv, 65, 4);

            return rv;
        }

        public override string ToString()
        {
            return Cipher.Base58.Encode(Bytes());
        }

        public byte[] Checksum()
        {
            return checksum;
        }

        public byte Version()
        {
            return version;
        }

        public VerifyException Verify()
        {
            if (!Cipher.KeyOps.InMainSubgroup(ref spend))
            {
                return new VerifyException("StealthAddress", "spend key " + spend.ToHexShort() + " not on main subgroup!");
            }

            if (!Cipher.KeyOps.InMainSubgroup(ref view))
            {
                return new VerifyException("StealthAddress", "view key " + view.ToHexShort() + " not on main subgroup!");
            }

            var charLength = Cipher.Base58.Encode(Bytes()).Length;

            if (charLength != 95)
            {
                return new VerifyException("StealthAddress", $"stealth address does not encode to correct length string! (expected 95 characters; got {charLength})");
            }

            if (version != 1)
            {
                return new VerifyException("StealthAddress", $"stealth address version not supported! (supports {AddressVersion.VERSION}; got {version})");
            }

            byte[] chk = new byte[65];
            chk[0] = version;
            Array.Copy(spend.bytes, 0, chk, 1, 32);
            Array.Copy(view.bytes, 0, chk, 33, 32);

            var chksum = Cipher.Base58.GetCheckSum(chk);

            var chksumStr = Printable.Hexify(chksum);
            var checksumStr = Printable.Hexify(checksum);

            if (chksumStr != checksumStr)
            {
                return new VerifyException("StealthAddress", $"stealth address checksums not equal! (calculated as {chksumStr}; but got {checksumStr})");
            }

            return null;
        }

        public byte Type()
        {
            return (byte)AddressType.STEALTH;
        }
    }
}
