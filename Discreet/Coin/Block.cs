using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Coin
{
    /**
     * This block class is intended for use in testnet.
     * Blocks will, in the future, also specify position in the DAG.
     * Global indices will be deterministically generated for the sake of DAG consistency.
     * The current consensus among developers for amount_output_index and tx_index is generation during the addition of the head block in the DAG.
     * All transactions in blocks in a round of consensus are added in order of timestamp, ensuring all blocks are processed.
     * For a blockchain this simplifies to a single block addition.
     */
    public class Block
    {
        public byte Version;
        public uint NumOutputs;

        public ulong Timestamp;
        public ulong Height;
        public ulong Fee;

        public Cipher.SHA256 PreviousBlock;
        public Cipher.SHA256 Hash;

        public uint NumTXs;
        public uint NumUTXOs;
        public uint BlockSize;
        public Cipher.SHA256[] Transactions;
        public uint[] Indices;
    }
}
