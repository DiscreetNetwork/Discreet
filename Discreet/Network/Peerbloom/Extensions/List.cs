using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Extensions
{
    public static class List
    {
        public static List<T> TakeRandomPercent<T>(this List<T> list, int low, int high)
        {
            List<T> toReturn = new List<T>();
            Random random = new Random();

            foreach (var item in list)
            {
                if(random.Next(low, high + 1) == low)
                {
                    toReturn.Add(item);
                }
            }

            return toReturn;
        }
    }
}
