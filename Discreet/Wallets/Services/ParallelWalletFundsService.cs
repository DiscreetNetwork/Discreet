using Discreet.Coin.Models;
using Discreet.DB;
using Discreet.Wallets.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public class ParallelWalletFundsService : WalletFundsService
    {
        public ParallelWalletFundsService(SQLiteWallet wallet, IView provider, bool checkCoinbase = true) : base(wallet, provider, checkCoinbase)
        {
        }

        protected virtual IEnumerable<Block> GetNextChunk(IEnumerator<Block> blockEnumerator, int chunkSize, bool valid = true)
        {
            // assume we've already enumerated one block
            int blocksRemaining = chunkSize;
            do
            {
                bool moveNext = valid;
                Block block = blockEnumerator.Current;

                while (moveNext && !checkCoinbase && block.Header.NumTXs == 1 && block.Header.Version == 2)
                {
                    lastSeenHeight = block.Header.Height;
                    moveNext = blockEnumerator.MoveNext();
                    block = blockEnumerator.Current;
                }

                if (moveNext && blocksRemaining > 1)
                {
                    if (block == null) throw new Exception("block was null");
                    if (lastSeenHeight >= block.Header.Height) throw new Exception($"already seen block");
                    if (lastSeenHeight + 1 != block.Header.Height) throw new Exception("block out of order");

                    lastSeenHeight = block.Header.Height;

                    blocksRemaining--;
                    yield return block;
                    valid = blockEnumerator.MoveNext();
                    continue;
                }
                else if (moveNext && blocksRemaining == 1)
                {
                    if (block == null) throw new Exception("block was null");
                    if (lastSeenHeight >= block.Header.Height) throw new Exception("already seen block");
                    if (lastSeenHeight + 1 != block.Header.Height) throw new Exception("block out of order");

                    lastSeenHeight = block.Header.Height;

                    blocksRemaining--;
                    yield return block;
                    // skip MoveNext()
                }
                else if (!moveNext)
                {
                    yield break;
                }
            }
            while (valid && blocksRemaining > 0);
        }

        public override void ProcessBlocks(IEnumerable<Block> blocks)
        {
            var sw = Stopwatch.StartNew();
            var blockEnumerator = blocks.GetEnumerator();
            while (blockEnumerator.MoveNext())
            {
                if (this.token.IsCancellationRequested)
                {
                    wallet.SetLastSeenHeight(lastSeenHeight);
                    State = ServiceState.COMPLETED;
                    return;
                }
                if (requestPause)
                {
                    State = ServiceState.PAUSED;
                    requestPause = false;
                }
                if (Paused)
                {
                    wallet.SetLastSeenHeight(lastSeenHeight);
                    return;
                }

                // grab chunk of blocks; not sure if Enumerable.Chunk() is lazily evaluated, so we do our own
                // we also force evaluation since we enumerate multiple times
                int chunkSize = 20*1024;
                var blockChunk = GetNextChunk(blockEnumerator, chunkSize).ToList();

                foreach (var acc in wallet.Accounts.Where(x => !x.Syncing).ToList())
                {
                    // trigger enumeration
                    try
                    {
                        (var spents, var utxos, var txs) = acc.ProcessBlocks(blockChunk);
                        if (spents != null || utxos != null || txs != null) wallet.SaveAccountFundData(acc, spents, utxos, txs);
                    }
                    catch (Exception ex)
                    {
                        Daemon.Logger.Error($"Exception thrown during syncing: {ex.Message}", ex);
                    }
                }
            }

            sw.Stop();

            Daemon.Logger.Debug($"SQLiteWallet synchronized with blockchain ({(checkCoinbase ? "with" : "without")} checking coinbase blocks) in {sw.ElapsedMilliseconds}ms");

            wallet.SetLastSeenHeight(lastSeenHeight);
        }
    }
}
