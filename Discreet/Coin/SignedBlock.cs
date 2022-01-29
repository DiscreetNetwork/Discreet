using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin
{
    /**
     * Signed Blocks are used by the Discreet Testnet to verify a masternode minted it.
     */
    public class SignedBlock: Block
    {
        public Cipher.Signature Sig;

        public bool CheckSignature()
        {
            return Sig.Verify(BlockHash) && IsMasternode(Sig.y);
        }

        public static bool IsMasternode(Cipher.Key k)
        {
            //TODO: Implement hardcoded masternode IDs
            return true;
        }
    }
}
