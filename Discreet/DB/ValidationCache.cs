using Discreet.Coin;
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
        private Block block;
        
        /* validation and update data */
        public bool valid;
        public List<UpdateEntry> updates;
        private uint pIndex;

        /* ephemeral validation data structures */
        private Dictionary<Coin.Transparent.TXInput, Coin.Transparent.TXOutput> spentPubs;
        private SortedSet<Cipher.Key> spentKeys;

        public ValidationCache(Block blk)
        {
            dataView = DataView.GetView();
            block = blk;
            pIndex = dataView.GetOutputIndex();
            updates = new List<UpdateEntry>();
        }

        public Exception Validate()
        {
            /* validate basic data */
            if (block.Header.Version != 1 && block.Header.Version != 2) return new VerifyException("Block", $"Unsupported version (blocks are either version 1 or 2); got version {block.Header.Version}");
            if (!block.Hash().Equals(block.Header.BlockHash)) return new VerifyException("Block", $"Block hash in header does not match calculated block hash");
            if (dataView.BlockExists(block.Header.BlockHash)) return new VerifyException("Block", $"Block already present");
            if (block.Transactions == null || block.Transactions.Length == 0) return new VerifyException("Block", "Block contains no transactions");
            if (block.Header.NumTXs != block.Transactions.Length) return new VerifyException("Block", $"Block tx mismatch: expected {block.Header.NumTXs}; got {block.Transactions.Length}");
            if (block.Size() != block.Header.BlockSize) return new VerifyException("Block", $"Block size mismatch: expected {block.Header.BlockSize}; got {block.Size()}");
            if ((long)block.Header.Timestamp > DateTime.UtcNow.AddMinutes(120).Ticks) return new VerifyException("Block", $"Block timestamp is more than two hours in the future");
            if (!block.GetMerkleRoot().Equals(block.Header.MerkleRoot)) return new VerifyException("Block", $"Block merkle hash does not match calculated block merkle hash");
            if (block.Header.ExtraLen != (block.Header.Extra?.Length ?? 0)) return new VerifyException("Block", $"Block extra mismatch: expected length {block.Header.ExtraLen}, but got {block.Header.Extra?.Length ?? 0}");

            /* check fee and private output count */
            ulong fee = 0;
            uint numOutputs = 0;

            for (int i = 0; i < block.Transactions.Length; i++)
            {
                fee += block.Transactions[i].Fee;
                numOutputs += block.Transactions[i].NumPOutputs;
            }

            if (fee != block.Header.Fee) return new VerifyException("Block", $"Block fee mismatch: expected {block.Header.Fee}; got {fee} from calculations");
            if (numOutputs != block.Header.NumOutputs) return new VerifyException("Block", $"block private output count mismatch: expected {block.Header.NumOutputs}; got {numOutputs} from calculations");

            /* check coinbase */
            if (block.Header.Version == 2)
            {
                var coinbaseTx = block.Transactions[0];

                if (coinbaseTx.Version != 0)
                {
                    return new VerifyException("Block", "Miner tx not present or invalid");
                }

                var coinbase = coinbaseTx.ToPrivate();

                if (coinbase.Outputs == null || coinbase.Outputs.Length != 1)
                {
                    return new VerifyException("Block", "Miner tx has invalid outputs");
                }

                /* coinbase tx has special verification logic */
                var minerexc = coinbase.Verify();

                if (minerexc != null)
                {
                    return minerexc;
                }

                /* now verify output amount matches commitment */
                Cipher.Key feeComm = new(new byte[32]);
                Cipher.Key _I = Cipher.Key.Copy(Cipher.Key.I);
                Cipher.KeyOps.GenCommitment(ref feeComm, ref _I, block.Header.Fee);

                if (!feeComm.Equals(coinbase.Outputs[0].Commitment))
                {
                    return new VerifyException("Block", "Coinbase transaction in block does not balance with fee commitment");
                }
            }

            /* pre-check transaction data */
            for (int i = (block.Header.Version == 2) ? 1 : 0; i < block.Transactions.Length; i++)
            {
                var precheckExc = block.Transactions[i].Precheck();
                if (precheckExc != null) return precheckExc;
            }

            /* check signature data */
            if ((block.Header.Version == 1 || block.Header.Version == 2) && !block.CheckSignature())
            {
                return new VerifyException("Block", "Block signature is invalid and/or does not come from a masternode");
            }

            /* check orphan data */
            if (!dataView.BlockExists(block.Header.PreviousBlock))
            {
                return new OrphanBlockException("Orphan block detected", dataView.GetChainHeight(), block.Header.Height, block);
            }
            else
            {
                var prevHeader = dataView.GetBlockHeader(block.Header.PreviousBlock);
                if (prevHeader.Height + 1 != block.Header.Height) return new VerifyException("Block", "Previous block height + 1 does not equal block height");
                if (dataView.GetChainHeight() + 1 != block.Header.Height) return new VerifyException("Block", "Chain height + 1 does not equal block height");
            }

            for (int i = block.Header.Version == 1 ? 0 : 1; i < block.Transactions.Length; i++)
            {
                // perform tx-based checks and populate updates list
            }

            return null;
        }
    }
}
