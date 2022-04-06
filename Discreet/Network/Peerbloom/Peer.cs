using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Peer
    {
        public IPEndPoint Endpoint { get; set; }
        public IPEndPoint Source { get; set; }
        public long LastSeen { get; set; }
        public long FirstSeen { get; set; }

        public bool InTried { get; set; }

        public int NumFailedConnectionAttempts { get; set; }

        public long LastSuccess { get; set; }
        public long LastAttempt { get; set; }

        // how many times this occurs in the NEW set
        public int RefCount { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[77];

            Core.Utils.SerializeEndpoint(Endpoint, data, 0);
            Core.Utils.SerializeEndpoint(Source, data, 18);
            Coin.Serialization.CopyData(data, 36, LastSeen);
            Coin.Serialization.CopyData(data, 44, FirstSeen);
            Coin.Serialization.CopyData(data, 52, InTried);
            Coin.Serialization.CopyData(data, 53, NumFailedConnectionAttempts);
            Coin.Serialization.CopyData(data, 57, LastSuccess);
            Coin.Serialization.CopyData(data, 65, LastAttempt);
            Coin.Serialization.CopyData(data, 73, RefCount);
            return data;
        }

        public Peer() { }

        public Peer(byte[] data)
        {
            Deserialize(data);
        }

        public void Deserialize(byte[] data)
        {
            Endpoint = Core.Utils.DeserializeEndpoint(data, 0);
            Source = Core.Utils.DeserializeEndpoint(data, 18);
            LastSeen = Coin.Serialization.GetInt64(data, 36);
            FirstSeen = Coin.Serialization.GetInt64(data, 44);
            InTried = Coin.Serialization.GetBool(data, 52);
            NumFailedConnectionAttempts = Coin.Serialization.GetInt32(data, 53);
            LastSuccess = Coin.Serialization.GetInt64(data, 57);
            LastAttempt = Coin.Serialization.GetInt64(data, 65);
            RefCount = Coin.Serialization.GetInt32(data, 73);
        }

        public Peer(IPEndPoint endpoint, IPEndPoint source, long lastSeen, long firstSeen)
        {
            Endpoint = endpoint;
            Source = source;
            LastSeen = lastSeen;
            FirstSeen = firstSeen;
            InTried = false;
            LastSuccess = 0;
            NumFailedConnectionAttempts = 0;
            LastAttempt = 0;
            RefCount = 0;
        }

        public Peer(IPEndPoint endpoint, IPEndPoint source, long lastSeen)
        {
            Endpoint = endpoint;
            Source = source;
            LastSeen = lastSeen;
            FirstSeen = DateTime.UtcNow.Ticks;
            InTried = false;
            LastSuccess = 0;
            NumFailedConnectionAttempts = 0;
            LastAttempt = 0;
            RefCount = 0;
        }

        public Peer(IPEndPoint endpoint, IPEndPoint source)
        {
            Endpoint = endpoint;
            Source = source;
            LastSeen = DateTime.UtcNow.Ticks - (24L * 3600L * 10_000_000L);
            FirstSeen = DateTime.UtcNow.Ticks;
            InTried = false;
            LastSuccess = 0;
            NumFailedConnectionAttempts = 0;
            LastAttempt = 0;
            RefCount = 0;
        }

        public bool IsTerrible()
        {
            // likely a private peer; should evict if failed to connect.
            if (LastAttempt > 0 && NumFailedConnectionAttempts > 0 && Endpoint.Port > 49151)
            {
                return true;
            }

            // don't remove things attempted in the last 10 minutes
            if (LastAttempt > 0 && LastAttempt >= DateTime.UtcNow.Ticks - 600L * 10_000_000L)
            {
                return false;
            }

            // has timestamp from too far in the future
            if (LastSeen > DateTime.UtcNow.Ticks + 600L * 10_000_000L)
            {
                return true;
            }

            // not seen in recent history, or never seen
            if (LastSeen == 0 || DateTime.UtcNow.Ticks - LastSeen > Constants.PEERLIST_HORIZON_DAYS * 24L * 3600L * 10_000_000L)
            {
                return true;
            }

            // no successes after maximum retries
            if (LastSuccess == 0 && NumFailedConnectionAttempts >= Constants.PEERLIST_MAX_RETRIES)
            {
                return true;
            }

            // too many falures over the last week
            if (DateTime.UtcNow.Ticks - LastSuccess > Constants.PEERLIST_MIN_FAIL_DAYS * 24L * 3600L * 10_000_000L && NumFailedConnectionAttempts >= Constants.PEERLIST_MAX_FAILURES)
            {
                return true;
            }

            return false;
        }

        public double GetChance()
        {
            double chance = 1.0;

            long lastTry = Math.Min(DateTime.UtcNow.Ticks - LastAttempt, 0);

            if (lastTry < Constants.PEERLIST_RECENT_TRY * 10_000_000L)
            {
                chance *= 0.01;
            }

            chance *= Math.Pow(0.66, Math.Min(NumFailedConnectionAttempts, 8));

            return chance;
        }
    }
}
