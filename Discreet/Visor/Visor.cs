using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.Wallets;
using Discreet.Common.Exceptions;
using Discreet.Cipher;

namespace Discreet.Visor
{
    /* Manages all blockchain operations. Contains the wallet manager, logger, Network manager, RPC manager, DB manager and TXPool manager. */
    public class Visor
    {
        // WIP
        Wallet wallet;

        public Visor(Wallet wallet)
        {
            this.wallet = wallet;
        }

        public void ProcessBlock(Block block)
        {
            var err = block.Verify();

            if (err != null)
            {
                throw err;
            }

            DB.DB db = DB.DB.GetDB();

            try
            {
                db.AddBlock(block);
            }
            catch(Exception e)
            {
                throw new DatabaseException("Discreet.Visor.Visor.ProcessBlock", e.Message);
            }

            wallet.ProcessBlock(block);
        }
    }
}
