using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public class ValidationCache
    {
        /* raw data and db access */
        private DataView dataView;
        private Coin.Block block;
        
        /* validation and update data */
        public bool valid;
        public List<UpdateEntry> updates;
        private uint pIndex;

        /* ephemeral validation data structures */
        private Dictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> spentPubs;
        private SortedSet<Cipher.Key> spentKeys;

        public ValidationCache(Coin.Block blk)
        {
            dataView = DataView.GetView();
            block = blk;
            pIndex = dataView.GetOutputIndex();
            updates = new List<UpdateEntry>();
        }

        public bool Validate()
        {

        }
    }
}
