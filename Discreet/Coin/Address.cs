using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Coin
{
    public interface IAddress
    {
        public byte[] Bytes();
        public string String();
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

        public string String()
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
        public Discreet.Cipher.Key view;

        [MarshalAs(UnmanagedType.Struct)]
        public Discreet.Cipher.Key spend;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] checksum;

        public StealthAddress(Discreet.Cipher.Key vk, Discreet.Cipher.Key sk)
        {
            version = AddressVersion.VERSION;

            view = vk;
            spend = sk;

            byte[] chk = new byte[65];
            chk[0] = version;
            Array.Copy(view.bytes, 0, chk, 1, 32);
            Array.Copy(view.bytes, 0, chk, 33, 32);

            checksum = Discreet.Cipher.Base58.GetCheckSum(chk);
        }

        public uint Size()
        {
            return 69;
        }

        public byte[] Bytes()
        {
            byte[] rv = new byte[Size()];

            rv[0] = version;
            Array.Copy(view.bytes, 0, rv, 1, 32);
            Array.Copy(spend.bytes, 0, rv, 33, 32);
            Array.Copy(checksum, 0, rv, 65, 4);

            return rv;
        }

        public string String()
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
}
