using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.RPC.Common
{
    [Flags]
    public enum APISet: int
    {
        DEFAULT = 0,
        READ = 1 << 0,
        TXN = 1 << 1,
        WALLET = 1 << 2,
        STORAGE = 1 << 3,
        SEED_RECOVERY = 1 << 4,
        STATUS = 1 << 5,
    }

    public static class APISetExtensions
    {
        public static APISet CreateSet(List<string> apis)
        {
            APISet apiSet = new APISet();

            foreach (string apisItem in apis)
            {
                apiSet |= GetSet(apisItem);
            }

            return apiSet;
        }

        public static APISet GetSet(string api)
        {
            if (api == null) return APISet.DEFAULT;

            if (api.ToLower().Trim() == "read") return APISet.READ;
            if (api.ToLower().Trim() == "txn" || api.ToLower().Trim() == "transaction") return APISet.TXN;
            if (api.ToLower().Trim() == "wallet") return APISet.WALLET;
            if (api.ToLower().Trim() == "storage") return APISet.STORAGE;
            if (api.ToLower().Trim() == "seed_recovery") return APISet.SEED_RECOVERY;
            if (api.ToLower().Trim() == "status") return APISet.STATUS;
            if (api.ToLower().Trim() == "all") return APISet.READ | APISet.TXN | APISet.WALLET | APISet.STORAGE | APISet.SEED_RECOVERY | APISet.STATUS;
            if (api.ToLower().Trim() == "explorer") return APISet.READ | APISet.STATUS;

            return APISet.DEFAULT;
        }

        /// <summary>
        /// Returns the APISet as a string.
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public static string Descriptor(this APISet set)
        {
            if (set == default) return "default";
            if (set == APISet.DEFAULT) return "default";

            List<string> rvs = new();

            if (set.HasFlag(APISet.READ)) rvs.Add("read");
            if (set.HasFlag(APISet.TXN)) rvs.Add("txn");
            if (set.HasFlag(APISet.WALLET)) rvs.Add("wallet");
            if (set.HasFlag(APISet.STORAGE)) rvs.Add("storage");
            if (set.HasFlag(APISet.SEED_RECOVERY)) rvs.Add("seed_recovery");
            if (set.HasFlag(APISet.STATUS)) rvs.Add("status");

            return string.Join(", ", rvs.ToArray());
        }
    }
}
