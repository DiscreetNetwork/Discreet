using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Readable
{
    public interface IReadable
    {
        public string JSON();
        public void FromJSON(string json);

        public T ToObject<T>();
        public void FromObject<T>(T obj);
    }
}
