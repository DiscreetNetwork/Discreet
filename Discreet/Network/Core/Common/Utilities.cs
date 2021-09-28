using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Discreet.Network.Core.Common
{
  public static class Utilities
    {
        public static IPEndPoint IPEndPointFromString(string ipEndPointString)
        {
            var endpoint = ipEndPointString.Split(':');
            return new IPEndPoint(IPAddress.Parse(endpoint[0]), int.Parse(endpoint[1]));
        }

    }
}
