using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;

namespace Discreet.DB
{
    public class CurView
    {
        private ArchiveDB archiveDB;
        private StateDB stateDB;

        private static CurView instance;

        static CurView()
        {
            instance = new CurView();
        }

        public CurView()
        {
            archiveDB = new ArchiveDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "archive"));
            stateDB = new StateDB(Path.Join(Daemon.DaemonConfig.GetConfig().DBPath, "state"));
        }

        public void AddBlockToCache(Block blk) => archiveDB.AddBlockToCache(blk);

        public void BlockCacheHas(Cipher.SHA256 block) => archiveDB.BlockCacheHas(block);

        public void AddBlock(Block blk)
        {
            archiveDB.AddBlock(blk);
            stateDB.AddBlock(blk);
        }

        public Dictionary<long, Block> GetBlockCache() => archiveDB.GetBlockCache();

        public void ClearBlockCache() => archiveDB.ClearBlockCache();

        public bool CheckSpentKey(Cipher.Key j) => stateDB.CheckSpentKey(j);

        public bool CheckSpentKeyBlock(Cipher.Key j) => stateDB.CheckSpentKeyBlock(j);
    }
}