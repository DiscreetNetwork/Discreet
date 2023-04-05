using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Discreet.Common.Exceptions;
using Discreet.Common;
using Discreet.RPC.Common;

namespace Discreet.Readable
{
    public class Triptych: IReadable
    {
        public string K { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }

        public List<string> X { get; set; }
        public List<string> Y { get; set; }
        public List<string> f { get; set; }

        public string zA { get; set; }
        public string zC { get; set; }
        public string z { get; set; }

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
            Triptych triptych = JsonSerializer.Deserialize<Triptych>(json);
            K = triptych.K;
            A = triptych.A;
            B = triptych.B;
            C = triptych.C;
            D = triptych.D;

            X = triptych.X;
            Y = triptych.Y;
            f = triptych.f;

            zA = triptych.zA;
            zC = triptych.zC;
            z = triptych.z;
        }

        public Triptych(Coin.Models.Triptych obj)
        {
            FromObject(obj);
        }

        public Triptych(string json)
        {
            FromJSON(json);
        }

        public Triptych() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.Models.Triptych))
            {
                FromObject((Coin.Models.Triptych)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Triptych).FullName);
            }
        }

        public void FromObject(Coin.Models.Triptych obj)
        {
            if (obj.K.bytes != null) K = obj.K.ToHex();
            if (obj.A.bytes != null) A = obj.A.ToHex();
            if (obj.B.bytes != null) B = obj.B.ToHex();
            if (obj.C.bytes != null) C = obj.C.ToHex();
            if (obj.D.bytes != null) D = obj.D.ToHex();

            if (obj.X != null)
            {
                X = new List<string>(obj.X.Length);

                for (int i = 0; i < obj.X.Length; i++)
                {
                    X.Add(obj.X[i].ToHex());
                }
            }
            if (obj.Y != null)
            {
                Y = new List<string>(obj.Y.Length);

                for (int i = 0; i < obj.Y.Length; i++)
                {
                    Y.Add(obj.Y[i].ToHex());
                }
            }
            if (obj.f != null)
            {
                f = new List<string>(obj.f.Length);

                for (int i = 0; i < obj.f.Length; i++)
                {
                    f.Add(obj.f[i].ToHex());
                }
            }

            if (obj.zA.bytes != null) zA = obj.zA.ToHex();
            if (obj.zC.bytes != null) zC = obj.zC.ToHex();
            if (obj.z.bytes != null) z = obj.z.ToHex();
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Models.Triptych))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(Triptych).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            Coin.Models.Triptych obj = new();

            if (K != null && K != "") obj.K = new Cipher.Key(Printable.Byteify(K));
            if (A != null && A != "") obj.A = new Cipher.Key(Printable.Byteify(A));
            if (B != null && B != "") obj.B = new Cipher.Key(Printable.Byteify(B));
            if (C != null && C != "") obj.C = new Cipher.Key(Printable.Byteify(C));
            if (D != null && D != "") obj.D = new Cipher.Key(Printable.Byteify(D));

            if (X != null)
            {
                obj.X = new Cipher.Key[X.Count];

                for (int i = 0; i < X.Count; i++)
                {
                    obj.X[i] = new Cipher.Key(Printable.Byteify(X[i]));
                }
            }
            if (Y != null)
            {
                obj.Y = new Cipher.Key[Y.Count];

                for (int i = 0; i < Y.Count; i++)
                {
                    obj.Y[i] = new Cipher.Key(Printable.Byteify(Y[i]));
                }
            }
            if (f != null)
            {
                obj.f = new Cipher.Key[f.Count];

                for (int i = 0; i < f.Count; i++)
                {
                    obj.f[i] = new Cipher.Key(Printable.Byteify(f[i]));
                }
            }

            if (zA != null && zA != "") obj.zA = new Cipher.Key(Printable.Byteify(zA));
            if (zC != null && zC != "") obj.zC = new Cipher.Key(Printable.Byteify(zC));
            if (z != null && z != "") obj.z = new Cipher.Key(Printable.Byteify(z));

            return obj;
        }

        public static Coin.Models.Triptych FromReadable(string json)
        {
            return (Coin.Models.Triptych)new Triptych(json).ToObject();
        }

        public static string ToReadable(Coin.Models.Triptych obj)
        {
            return new Triptych(obj).JSON();
        }
    }
}
