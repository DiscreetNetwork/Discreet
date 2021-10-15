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
    public class Bulletproof: IReadable
    {
        public ulong size { get; set; }

        public string A { get; set; }
        public string S { get; set; }
        public string T1 { get; set; }
        public string T2 { get; set; }
        public string taux { get; set; }
        public string mu { get; set; }

        public List<string> L { get; set; }
        public List<string> R { get; set; }

        public string a { get; set; }
        public string b { get; set; }
        public string t { get; set; }

        public string JSON()
        {
            return JsonSerializer.Serialize(this);
        }

        public override string ToString()
        {
            return JSON();
        }

        public void FromJSON(string json)
        {
            Bulletproof bp = JsonSerializer.Deserialize<Bulletproof>(json);
            size = bp.size;
            
            A = bp.A;
            S = bp.S;
            T1 = bp.T1;
            T2 = bp.T2;
            taux = bp.taux;
            mu = bp.mu;

            L = bp.L;
            R = bp.R;

            a = bp.a;
            b = bp.b;
            t = bp.t;
        }

        public Bulletproof(Coin.Bulletproof obj)
        {
            FromObject(obj);
        }

        public Bulletproof(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.Bulletproof))
            {
                dynamic t = obj;
                FromObject((Coin.Bulletproof)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Bulletproof).FullName);
            }
        }

        public void FromObject(Coin.Bulletproof obj)
        {
            size = obj.size;

            if (obj.A.bytes != null) A = obj.A.ToHex();
            if (obj.S.bytes != null) S = obj.S.ToHex();
            if (obj.T1.bytes != null) T1 = obj.T1.ToHex();
            if (obj.T2.bytes != null) T2 = obj.T2.ToHex();
            if (obj.taux.bytes != null) taux = obj.taux.ToHex();
            if (obj.mu.bytes != null) mu = obj.mu.ToHex();

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

            if (obj.a.bytes != null) a = obj.a.ToHex();
            if (obj.b.bytes != null) b = obj.b.ToHex();
            if (obj.t.bytes != null) t = obj.t.ToHex();
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Bulletproof))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(Bulletproof).FullName, typeof(T).FullName);
            }

        }

        public Coin.Bulletproof ToObject()
        {
            Coin.Bulletproof obj = new();

            if (A != null && A != "") obj.A = new Cipher.Key(Printable.Byteify(A));
            if (S != null && S != "") obj.S = new Cipher.Key(Printable.Byteify(S));
            if (T1 != null && T1 != "") obj.T1 = new Cipher.Key(Printable.Byteify(T1));
            if (T2 != null && T2 != "") obj.T2 = new Cipher.Key(Printable.Byteify(T2));
            if (taux != null && taux != "") obj.taux = new Cipher.Key(Printable.Byteify(taux));
            if (mu != null && mu != "") obj.mu = new Cipher.Key(Printable.Byteify(mu));

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

            if (a != null && a != "") obj.a = new Cipher.Key(Printable.Byteify(a));
            if (b != null && b != "") obj.b = new Cipher.Key(Printable.Byteify(b));
            if (t != null && t != "") obj.t = new Cipher.Key(Printable.Byteify(t));

            return obj;
        }

        public static Coin.Bulletproof FromReadable(string json)
        {
            return new Bulletproof(json).ToObject();
        }

        public static string ToReadable(Coin.Bulletproof obj)
        {
            return new Bulletproof(obj).JSON();
        }
    }
}
