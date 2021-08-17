using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Coin
{
    /**
     * WIP
     * 
     * Transaction outputs come in both transparent and spent varieties.
     * 
     * 
     * 
     * 
     * 
     */

    class TxOutputHeader
    {
        UInt64 Time;
        UInt64 BlockNumber;
    }

    class TxOutput
    {
        Discreet.Cipher.Hash TransactionSrc;
    }

}
