using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Common;
using System.IO;

namespace Discreet.Coin
{
    /// <summary>
    /// AddressType defines types of addresses, and thus, types of transactions
    /// that are handled by those addresses.
    /// </summary>
    public enum AddressType: byte
    {
        STEALTH = 0,
        TRANSPARENT = 1,
    }

    /// <summary>
    /// IAddress defines methods that are common to all the addresses defined in
    /// AddressType.
    /// </summary>
    public interface IAddress
    {
        public byte[] Bytes();
        public string ToString();
        public byte[] Checksum();
        public byte Version();
        public uint Size();

        public byte Type();
    }

    /// <summary>
    /// AddressVersion defines the standard versioner for addresses.
    /// </summary>
    public static class AddressVersion
    {
        public static byte VERSION = 1;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// TAddress is the Discreet transparent address class.
    /// </summary>
    public class TAddress: IAddress
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte version;

        [MarshalAs(UnmanagedType.Struct)]
        public Cipher.RIPEMD160 hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] checksum;

        /// <summary>
        /// TAddress initializes the version, hash and checksum of the
        /// transparent address.
        /// </summary>
        public TAddress()
        {
            version = 0;
            hash = new Cipher.RIPEMD160(new byte[20], false);
            checksum = new byte[4];
        }

        /// <summary>
        /// TAddress initialize the version, hash and checksum of the
        /// transparent address. Uses a public key to generate the hash.
        /// </summary>
        /// <param name="pk">A public key.</param>
        public TAddress(Cipher.Key pk)
        {
            version = AddressVersion.VERSION;

            hash = Cipher.RIPEMD160.HashData(Cipher.SHA256.HashData(pk.bytes).Bytes);

            byte[] chk = new byte[21];
            chk[0] = version;
            Array.Copy(hash.Bytes, 0, chk, 1, 20);

            checksum = Cipher.Base58.GetCheckSum(chk);
        }

        /// <summary>
        /// TAddress initializes the version, hash and checksum of the
        /// transparent address. Uses a byte array to initialize these fields.
        /// </summary>
        /// <param name="bytes">Byte array containing the version, hash and
        /// checksum of a transparent address.</param>
        public TAddress(byte[] bytes)
        {
            version = bytes[0];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        /// <summary>
        /// TAddress initializes the version, hash and checksum of the
        /// transparent address. Uses a byte array to initialize these fields,
        /// retrieving bytes starting from an offset.
        /// </summary>
        /// <param name="bytes">Byte array containing the version, hash and
        /// checksum.</param>
        /// <param name="offset">Offset which indicates from
        /// what index we can start reading to find the relevant data.</param>
        public TAddress(byte[] bytes, uint offset)
        {
            version = bytes[offset];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, offset + 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            Array.Copy(bytes, offset + 21, checksum, 0, 4);
        }

        /// <summary>
        /// TAddress initializes the version, hash and checksum of the
        /// transparent address. Uses a stream of bytes to initialize these
        /// fields.
        /// </summary>
        /// <param name="s">A stream of bytes.</param>
        public TAddress(Stream s)
        {
            version = (byte)s.ReadByte();
            byte[] _hash = new byte[20];
            s.Read(_hash);
            hash = new Cipher.RIPEMD160(_hash, false);
            checksum = new byte[4];
            s.Read(checksum);
        }

        /// <summary>
        /// TAddress initializes the version, hash and checksum of the
        /// transparent address. Uses an address to initialize the version,
        /// hash and checksum.
        /// </summary>
        /// <param name="addr">A string representing an address.</param>
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

        /// <summary>
        /// Size returns the size of the transparent address.
        /// </summary>
        public uint Size()
        {
            return 25;
        }

        /// <summary>
        /// Bytes creates a byte array representation of the transparent
        /// address, where the first byte is the version, the following 20 bytes
        /// are the hash, and the remaining 4 are the checksum.
        /// </summary>
        /// <returns>The byte array representation of the transparent address.</returns>
        public byte[] Bytes()
        {
            byte[] rv = new byte[Size()];

            rv[0] = version;
            Array.Copy(hash.Bytes, 0, rv, 1, 20);
            Array.Copy(checksum, 0, rv, 21, 4);

            return rv;
        }

        /// <summary>
        /// FromBytes initializes the transparent address (version, hash and
        /// checksum) from an array of bytes.
        /// </summary>
        /// <param name="bytes">The byte array representation of the transparent address.</param>
        public void FromBytes(byte[] bytes)
        {
            version = bytes[0];
            byte[] _hash = new byte[20];
            Array.Copy(bytes, 1, _hash, 0, 20);
            hash = new Cipher.RIPEMD160(_hash, false);
            Array.Copy(bytes, 21, checksum, 0, 4);
        }

        /// <summary>
        /// ToString generates a string representation of the transparent address.
        /// </summary>
        /// <returns>A string representation of the transparent address.</returns>
        public override string ToString()
        {
            return Cipher.Base58.EncodeWhole(Bytes());
        }

        /// <summary>
        /// Checksum returns the checksum of the transparent address.
        /// </summary>
        /// <returns>The checksum.</returns>
        public byte[] Checksum()
        {
            return checksum;
        }

        /// <summary>
        /// Version returns the version of the transparent address.
        /// </summary>
        /// <returns>The version.</returns>
        public byte Version()
        {
            return version;
        }

        /// <summary>
        /// Type returns the type of this address, which is transparent.
        /// </summary>
        /// <returns>The type of the address.</returns>
        public byte Type()
        {
            return (byte)AddressType.TRANSPARENT;
        }

        /// <summary>
        /// Verify verifies that the version and checksum of a transparent address are valid.
        /// </summary>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify()
        {
            if (version != 1)
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

        /// <summary>
        /// CheckAddressBytes determines if the given public key matches the
        /// address of this transparent address.
        /// </summary>
        /// <param name="pk">A public key.</param>
        /// <returns>True if the addresses match, false otherwise.</returns>
        public bool CheckAddressBytes(Cipher.Key pk)
        {
            var pkh = Cipher.RIPEMD160.HashData(Cipher.SHA256.HashData(pk.bytes).Bytes);
            return hash.Equals(pkh);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// StealthAddress is the Discreet shielded/private address class (i.e. dual key wallet).
    /// </summary>
    public class StealthAddress : IAddress
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte version;

        [MarshalAs(UnmanagedType.Struct)]
        public Cipher.Key spend;

        [MarshalAs(UnmanagedType.Struct)]
        public Cipher.Key view;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] checksum;

        /// <summary>
        /// StealthAddress initializes the view and spend keys, as well as the
        /// version and checksum of the stealth address.
        /// </summary>
        /// <param name="vk">A view key.</param>
        /// <param name="pk">A spend key.</param>
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

        /// <summary>
        /// StealthAddress initializes the view and spend keys, as well as the
        /// version and checksum of the stealth address. Uses a byte array to
        /// initialize these fields.
        /// </summary>
        /// <param name="bytes">Byte array containing the view and spend keys,
        /// version, hash and checksum of a stealth address.</param>
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

        /// <summary>
        /// StealthAddress initializes the view and spend keys, as well as the
        /// version and checksum of the stealth address. Uses a byte array to
        /// initialize these fields, retrieving bytes starting from an offset.
        /// </summary>
        /// <param name="bytes">Byte array containing the view and spend keys,
        /// version, hash and checksum of a stealth address.</param>
        /// <param name="offset">Offset which indicates from
        /// what index we can start reading to find the relevant data.</param>
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

        /// <summary>
        /// StealthAddress initializes the view and spend keys, as
        /// well as the version and checksum of the stealth address. Uses a
        /// stream of bytes to initialize these fields.
        /// </summary>
        /// <param name="bytes">A stream of bytes.</param>
        public StealthAddress(Stream s)
        {
            version = (byte) s.ReadByte();

            spend = new Cipher.Key(s);
            view = new Cipher.Key(s);
            checksum = new byte[4];

            s.Read(checksum);
        }

        /// <summary>
        /// StealthAddress initializes the view and spend keys, as
        /// well as the version and checksum of the stealth address. Uses an
        /// address to initialize the version, hash and checksum.
        /// </summary>
        /// <param name="bytes">A string representing an address.</param>
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

        /// <summary>
        /// Size returns the size of the stealth address.
        /// </summary>
        public uint Size()
        {
            return 69;
        }

        /// <summary>
        /// Bytes creates a byte array representation of the stealth address,
        /// where the first byte is the version, the following 32 bytes are the
        /// spend key, the next 32 bytes are the view key, and the remaining 4
        /// are the checksum.
        /// </summary>
        /// <returns>The byte array representation of the stealth address.</returns>
        public byte[] Bytes()
        {
            byte[] rv = new byte[Size()];

            rv[0] = version;
            Array.Copy(spend.bytes, 0, rv, 1, 32);
            Array.Copy(view.bytes, 0, rv, 33, 32);
            Array.Copy(checksum, 0, rv, 65, 4);

            return rv;
        }

        /// <summary>
        /// ToString generates a string representation of the stealth address.
        /// </summary>
        /// <returns>A string representation of the stealth address.</returns>
        public override string ToString()
        {
            return Cipher.Base58.Encode(Bytes());
        }

        /// <summary>
        /// Checksum returns the checksum of the stealth address.
        /// </summary>
        /// <returns>The checksum.</returns>
        public byte[] Checksum()
        {
            return checksum;
        }

        /// <summary>
        /// Version returns the version of the stealth address.
        /// </summary>
        /// <returns>The version.</returns>
        public byte Version()
        {
            return version;
        }

        /// <summary>
        /// Type returns the type of this address, which is stealth.
        /// </summary>
        /// <returns>The type of the address.</returns>
        public byte Type()
        {
            return (byte)AddressType.STEALTH;
        }

        /// <summary>
        /// Verify verifies that the following properties are correct in the stealth address:
        /// - The spend and view keys are in the main subgroup.
        /// - The address has a valid length.
        /// - The version is valid.
        /// - The checksum is valid.
        /// </summary>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
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
    }
}
