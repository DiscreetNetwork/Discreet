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
        TXPool txpool;

        public Visor(Wallet wallet)
        {
            if (wallet.Addresses == null || wallet.Addresses.Length == 0 || wallet.Addresses[0].Type == 1)
            {
                throw new Exception("Discreet.Visor.Visor: Improper wallet for visor!");
            }

            this.wallet = wallet;

            txpool = TXPool.GetTXPool();
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

        public void ProcessTransaction(FullTransaction tx)
        {
            var err = txpool.ProcessIncoming(tx);

            if (err != null)
            {
                Logger.Log(err.Message);
            }
        }

        public void Mint()
        {
            if (wallet.Addresses[0].Type != 0) throw new Exception("Discreet.Visor.Visor.Mint: Cannot mint a block with transparent wallet!");

            Block blk = Block.Build(txpool.GetTransactionsForBlock(), (StealthAddress)wallet.Addresses[0].GetAddress());

            //propagate

            ProcessBlock(blk);
        }
    }
}
