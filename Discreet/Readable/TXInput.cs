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
    public class TXInput: IReadable
    {
        public List<uint> Offsets { get; set; }
        public string KeyImage { get; set; }

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
            TXInput tXInput = JsonSerializer.Deserialize<TXInput>(json);
            Offsets = tXInput.Offsets;
            KeyImage = tXInput.KeyImage;
        }

        public TXInput(Coin.TXInput obj)
        {
            FromObject(obj);
        }

        public TXInput(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.TXInput))
            {
                dynamic t = obj;
                FromObject((Coin.TXInput)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(TXInput).FullName);
            }
        }

        public void FromObject(Coin.TXInput obj)
        {
            if (obj.Offsets != null)
            {
                Offsets = new List<uint>(obj.Offsets.Length);

                for (int i = 0; i < obj.Offsets.Length; i++)
                {
                    Offsets.Add(obj.Offsets[i]);
                }
            }
            if (obj.KeyImage.bytes != null) KeyImage = obj.KeyImage.ToHex();
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.TXInput))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(TXInput).FullName, typeof(T).FullName);
            }

        }

        public Coin.TXInput ToObject()
        {
            Coin.TXInput obj = new();

            if (Offsets != null)
            {
                obj.Offsets = new uint[Offsets.Count];

                for (int i = 0; i < Offsets.Count; i++)
                {
                    obj.Offsets[i] = Offsets[i];
                }
            }
            if (KeyImage != null && KeyImage != "") obj.KeyImage = new Cipher.Key(Printable.Byteify(KeyImage));

            return obj;
        }

        public static Coin.TXInput FromReadable(string json)
        {
            return new TXInput(json).ToObject();
        }

        public static string ToReadable(Coin.TXInput obj)
        {
            return new TXInput(obj).JSON();
        }
    }
}
