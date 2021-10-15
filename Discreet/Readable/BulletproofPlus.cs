using Discreet.Common;
using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Readable
{
    public class BulletproofPlus: IReadable
    {
        public uint size { get; set; }

        public string A { get; set; }
        public string A1 { get; set; }
        public string B { get; set; }

        public string r1 { get; set; }
        public string s1 { get; set; }
        public string d1 { get; set; }

        public List<string> L { get; set; }
        public List<string> R { get; set; }

        public string JSON()
        {
            return JsonSerializer.Serialize(this, ReadableOptions.Options);
        }

        public override string ToString()
        {
            return JSON();
        }

        public void FromJSON(string json)
        {
            BulletproofPlus bp = JsonSerializer.Deserialize<BulletproofPlus>(json);
            size = bp.size;

            A = bp.A;
            A1 = bp.A1;
            B = bp.B;

            r1 = bp.r1;
            s1 = bp.s1;
            d1 = bp.d1;

            L = bp.L;
            R = bp.R;
        }

        public BulletproofPlus(Coin.BulletproofPlus obj)
        {
            FromObject(obj);
        }

        public BulletproofPlus(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.BulletproofPlus))
            {
                dynamic t = obj;
                FromObject((Coin.BulletproofPlus)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(BulletproofPlus).FullName);
            }
        }

        public void FromObject(Coin.BulletproofPlus obj)
        {
            size = obj.size;

            if (obj.A.bytes != null) A = obj.A.ToHex();
            if (obj.A1.bytes != null) A1 = obj.A1.ToHex();
            if (obj.B.bytes != null) B = obj.B.ToHex();

            if (obj.r1.bytes != null) r1 = obj.r1.ToHex();
            if (obj.s1.bytes != null) s1 = obj.s1.ToHex();
            if (obj.d1.bytes != null) d1 = obj.d1.ToHex();

            if (obj.L != null)
            {
                L = new List<string>(obj.L.Length);

                for (int i = 0; i < obj.L.Length; i++)
                {
                    L.Add(obj.L[i].ToHex());
                }
            }
            if (obj.R != null)
            {
                R = new List<string>(obj.R.Length);

                for (int i = 0; i < obj.R.Length; i++)
                {
                    R.Add(obj.R[i].ToHex());
                }
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.BulletproofPlus))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(BulletproofPlus).FullName, typeof(T).FullName);
            }

        }

        public Coin.BulletproofPlus ToObject()
        {
            Coin.BulletproofPlus obj = new();

            obj.size = size;

            if (A != null && A != "") obj.A = new Cipher.Key(Printable.Byteify(A));
            if (A1 != null && A1 != "") obj.A1 = new Cipher.Key(Printable.Byteify(A1));
            if (B != null && B != "") obj.B = new Cipher.Key(Printable.Byteify(B));

            if (r1 != null && r1 != "") obj.r1 = new Cipher.Key(Printable.Byteify(r1));
            if (s1 != null && s1 != "") obj.s1 = new Cipher.Key(Printable.Byteify(s1));
            if (d1 != null && d1 != "") obj.d1 = new Cipher.Key(Printable.Byteify(d1));

            if (L != null)
            {
                obj.L = new Cipher.Key[L.Count];

                for (int i = 0; i < L.Count; i++)
                {
                    obj.L[i] = new Cipher.Key(Printable.Byteify(L[i]));
                }
            }
            if (R != null)
            {
                obj.R = new Cipher.Key[R.Count];

                for (int i = 0; i < R.Count; i++)
                {
                    obj.R[i] = new Cipher.Key(Printable.Byteify(R[i]));
                }
            }

            return obj;
        }

        public static Coin.BulletproofPlus FromReadable(string json)
        {
            return new BulletproofPlus(json).ToObject();
        }

        public static string ToReadable(Coin.BulletproofPlus obj)
        {
            return new BulletproofPlus(obj).JSON();
        }
    }
}
