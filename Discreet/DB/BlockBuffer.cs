using Discreet.Coin.Comparers;
using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Discreet.DB
{
    /// <summary>
    /// Provides a multiple-writer, single-reader instance for writing blocks to the blockchain. Can be configured to write periodically or on each new block.
    /// </summary>
    public class BlockBuffer
    {
        private Channel<Block> _buffer;

        public ChannelWriter<Block> Writer { get => _buffer.Writer; }

        private static BlockBuffer _instance;

        public static BlockBuffer Instance
        {
            get
            {
                if (_instance == null) _instance = new BlockBuffer();
                return _instance;
            }
        }

        private static TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
        public static TimeSpan FlushInterval { set { _flushInterval = TimeSpan.FromTicks(Math.Max(value.Ticks, TimeSpan.FromSeconds(1).Ticks)); } }

        private static bool _flushEveryBlock = false;
        public static bool FlushEveryBlock { set { _flushEveryBlock = value; } }

        public BlockBuffer()
        {
            _buffer = Channel.CreateUnbounded<Block>();
        }

        /// <summary>
        /// Starts the block buffer's flusher.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            List<Block> buf = new List<Block>();
            DateTime lastFlush = DateTime.MinValue;

            await foreach(var block in _buffer.Reader.ReadAllAsync())
            {
                if (_flushEveryBlock)
                {
                    Flush(new List<Block> { block });
                }
                else
                {
                    buf.Add(block);
                    // check received
                    if (DateTime.Now.Subtract(lastFlush) > _flushInterval)
                    {
                        // flush
                        Flush(buf);
                        buf.Clear();
                        lastFlush = DateTime.Now;
                    }
                }
            }
        }

        public void Flush(List<Block> blocks)
        {
            List<UpdateEntry> updates = new List<UpdateEntry>();

            // get previous indexers
            var tIndex = DataView.GetView().GetTransactionIndexer();
            var pIndex = DataView.GetView().GetOutputIndex();
            var bIndex = blocks.Select(x => x.Header.Height).Max();

            Dictionary<TTXInput, TTXOutput> pubUpdates = new Dictionary<TTXInput, TTXOutput>(new TTXInputEqualityComparer());

            foreach (var block in blocks)
            {
                // add block info
                updates.Add(new UpdateEntry { key = block.Header.BlockHash.Bytes, value = Serialization.Int64(block.Header.Height), rule = UpdateRule.ADD, type = UpdateType.BLOCKHEIGHT });
                updates.Add(new UpdateEntry { key = Serialization.Int64(block.Header.Height), value = block.Serialize(), rule = UpdateRule.ADD, type = UpdateType.BLOCK });
                updates.Add(new UpdateEntry { key = Serialization.Int64(block.Header.Height), value = block.Header.Serialize(), rule = UpdateRule.ADD, type = UpdateType.BLOCKHEADER });

                // add transactions
                foreach (var tx in block.Transactions)
                {
                    tIndex++;
                    updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt64(tIndex), rule = UpdateRule.ADD, type = UpdateType.TXINDEX });
                    updates.Add(new UpdateEntry { key = Serialization.UInt64(tIndex), value = tx.Serialize(), rule = UpdateRule.ADD, type = UpdateType.TX });

                    // pouts
                    uint[] uarr = new uint[tx.NumPOutputs];
                    for (int i = 0; i < tx.NumPOutputs; i++)
                    {
                        pIndex++;
                        updates.Add(new UpdateEntry { key = Serialization.UInt32(pIndex), value = tx.POutputs[i].Serialize(), rule = UpdateRule.ADD, type = UpdateType.OUTPUT });
                        uarr[i] = pIndex;
                    }

                    // pout indices
                    updates.Add(new UpdateEntry { key = tx.TxID.Bytes, value = Serialization.UInt32Array(uarr), rule = UpdateRule.ADD, type = UpdateType.OUTPUTINDICES });

                    // spent keys
                    for (int i = 0; i < tx.NumPInputs; i++)
                    {
                        updates.Add(new UpdateEntry { key = tx.PInputs[i].KeyImage.bytes, value = ChainDB.ZEROKEY, rule = UpdateRule.ADD, type = UpdateType.SPENTKEY });
                    }

                    // tinputs
                    for (int i = 0; i < tx.NumTInputs; i++)
                    {
                        if (pubUpdates.ContainsKey(tx.TInputs[i])) pubUpdates.Remove(tx.TInputs[i]);
                        else updates.Add(new UpdateEntry { key = tx.TInputs[i].Serialize(), value = ChainDB.ZEROKEY, rule = UpdateRule.DEL, type = UpdateType.PUBOUTPUT });
                    }

                    // touts
                    for (int i = 0; i < tx.NumTOutputs; i++)
                    {
                        pubUpdates[new TTXInput { TxSrc = tx.TxID, Offset = (byte)i }] = tx.TOutputs[i];
                    }
                }
            }

            // new touts
            foreach ((var txi, var txo) in pubUpdates)
            {
                txo.TransactionSrc = txi.TxSrc;
                updates.Add(new UpdateEntry { key = txi.Serialize(), value = txo.Serialize(), rule = UpdateRule.ADD, type = UpdateType.PUBOUTPUT });
            }

            // update indexers
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("indexer_tx"), value = Serialization.UInt64(tIndex), rule = UpdateRule.UPDATE, type = UpdateType.TXINDEXER });
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("indexer_output"), value = Serialization.UInt32(pIndex), rule = UpdateRule.UPDATE, type = UpdateType.OUTPUTINDEXER });
            updates.Add(new UpdateEntry { key = Encoding.ASCII.GetBytes("height"), value = Serialization.Int64(bIndex), rule = UpdateRule.UPDATE, type = UpdateType.HEIGHT });

            // perform flush
            DataView.GetView().Flush(updates);
        }
    }
}
