using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Common;

namespace Discreet.Coin
{
    public interface IAddress
    {
        public byte[] Bytes();
        public string ToString();
        public byte[] Checksum();
        public byte Version();
        public uint Size();
    }

    /* standard versioner for addresses */
    public static class AddressVersion
    {
        public static byte VERSION = 1;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    /**
     * TAddress is the Discreet Transparent address class.
     */
    public class TAddress: IAddress
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte version;

        [MarshalAs(UnmanagedType.Struct)]
        public Discreet.Cipher.RIPEMD160 hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] checksum;

        public TAddress(Discreet.Cipher.Key pk)
        {
            version = AddressVersion.VERSION;

            hash = Discreet.Cipher.RIPEMD160.HashData(Discreet.Cipher.SHA256.HashData(pk.bytes).Bytes);

            byte[] chk = new byte[21];
            chk[0] = version;
            Array.Copy(hash.Bytes, 0, chk, 1, 20);

            checksum = Discreet.Cipher.Base58.GetCheckSum(chk);
        }

        public TAddress()
        {
            version = 0;
            hash = new Discreet.Cipher.RIPEMD160(new byte[20], false);
            checksum = new byte[4];
        }

        public TAddress(byte[] bytes)
        {
            version = bytes[0];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Discreet.Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        public TAddress(string addr)
        {
            byte[] bytes = Cipher.Base58.Decode(addr);

            if (bytes.Length != Size())
            {
                throw new Exception($"Discreet.Coin.TAddress: Cannot make address from byte array of size {bytes.Length}, requires byte array of size {Size()} (attempted to make from string: \"{addr}\")");
            }

            version = bytes[0];

            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Discreet.Cipher.RIPEMD160(_hash, false);

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
            hash = new Discreet.Cipher.RIPEMD160(_hash, false);
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        public override string ToString()
        {
            return Discreet.Cipher.Base58.Encode(Bytes());
        }

        public byte[] Checksum()
        {
            return checksum;
        }

        public byte Version()
        {
            return version;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    /**
     * StealthAddress is the Discreet shielded/private address class (i.e. dual key wallet).
     */
    public class StealthAddress : IAddress
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte version;

        [MarshalAs(UnmanagedType.Struct)]
        public Discreet.Cipher.Key spend;

        [MarshalAs(UnmanagedType.Struct)]
        public Discreet.Cipher.Key view;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] checksum;

        public StealthAddress(Discreet.Cipher.Key vk, Discreet.Cipher.Key sk)
        {
            version = AddressVersion.VERSION;

            view = vk;
            spend = sk;

            byte[] chk = new byte[65];
            chk[0] = version;
            Array.Copy(spend.bytes, 0, chk, 1, 32);
            Array.Copy(view.bytes, 0, chk, 33, 32);

            checksum = Discreet.Cipher.Base58.GetCheckSum(chk);
        }

        public StealthAddress(byte[] bytes)
        {
            if (bytes.Length != Size())
            {
                throw new Exception($"Discreet.Coin.StealthAddress: Cannot make stealth address from byte array of size {bytes.Length}, requires byte array of size {Size()}");
            }
            version = bytes[0];

            spend = new Cipher.Key(new byte[32]);
            view = new Cipher.Key(new byte[32]);
            checksum = new byte[4];

            Array.Copy(bytes, 1, spend.bytes, 0, 32);
            Array.Copy(bytes, 33, view.bytes, 0, 32);
            Array.Copy(bytes, 65, checksum, 0, 4);
        }

        public StealthAddress(string data)
        {
            byte[] bytes = Cipher.Base58.Decode(data);

            if (bytes.Length != Size())
            {
                throw new Exception($"Discreet.Coin.StealthAddress: Cannot make stealth address from byte array of size {bytes.Length}, requires byte array of size {Size()} (attempted to make from string: \"{data}\")");
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
            return Discreet.Cipher.Base58.Encode(Bytes());
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
                return new VerifyException("StealthAddress", $"stealth address version not supported! (supports up to {AddressVersion.VERSION}; got {version})");
            }

            byte[] chk = new byte[65];
            chk[0] = version;
            Array.Copy(spend.bytes, 0, chk, 1, 32);
            Array.Copy(view.bytes, 0, chk, 33, 32);

            var chksum = Discreet.Cipher.Base58.GetCheckSum(chk);

            var chksumStr = Printable.Hexify(chksum);
            var checksumStr = Printable.Hexify(checksum);

            if (chksumStr != checksumStr)
            {
                return new VerifyException("StealthAddress", $"stealth address checksums not equal! (calculated as {chksumStr}; but got {checksumStr})");
            }

            return null;
        }
    }
}
