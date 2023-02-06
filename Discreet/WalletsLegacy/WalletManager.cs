using Discreet.Cipher;
using Discreet.DB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.WalletsLegacy
{
    /**
     * Task MasterSyncer thread
     * - AddressSyncer if needed ON STARTUP
     * MasterSyncer will read from Handler to get its state
     * Then periodically check DataView for new blocks, process
     */
    public class WalletManager
    {
        public List<Wallet> Wallets;
        public static WalletManager Instance;
        // every wallet process MUST be interruptable.

        public WalletManager()
        {
            Wallets = new List<Wallet>();
        }

        static WalletManager()
        {
            Instance = new WalletManager();
        }

        public async Task Start(CancellationToken token)
        {
            DataView dataView = DataView.GetView();

            while (!token.IsCancellationRequested)
            {
                var height = dataView.GetChainHeight();

                lock (Wallets)
                {
                    for (int i = 0; i < Wallets.Count; i++)
                    {
                        if (Wallets[i].IsUnloadRequested)
                        {
                            Wallets[i].IsUnloadRequested = false;
                            Wallets[i].Save();
                            Wallets[i].MustEncrypt();
                            Wallets.Remove(Wallets[i]);
                            i--;
                        }
                    }
                }

                foreach (var wallet in Wallets)
                {
                    if (wallet.IsEncrypted) continue;

                    if (wallet.IsLockRequested)
                    {
                        wallet.Save();
                        wallet.MustEncrypt();
                        wallet.IsLockRequested = false;
                    }

                    if (!wallet.IsSyncing && wallet.LastSeenHeight < height)
                    {
                        _ = Task.Run(async () => await WalletSync(wallet, height), token).ConfigureAwait(false);
                    }
                }

                await Task.Delay(1000);
            }
        }

        public async Task WalletSync(Wallet wallet, long height)
        {
            wallet.IsSyncing = true;
            while (wallet.LastSeenHeight < height)
            {
                if (wallet.IsLockRequested)
                {
                    wallet.IsSyncing = false;
                    return;
                }

                var block = DataView.GetView().GetBlock(wallet.LastSeenHeight + 1);
                
                wallet.ProcessBlock(block);
            }

            wallet.IsSyncing = false;
            wallet.Synced = true;
        }

        public async Task AddressSync(WalletAddress address, long height)
        {
            while (address.LastSeenHeight < height)
            {
                if (address.wallet.IsLockRequested)
                {
                    return;
                }

                var block = DataView.GetView().GetBlock(address.LastSeenHeight + 1);

                address.ProcessBlock(block);
            }

            address.Synced = true;
            address.Syncer = false;
        }
    }
}
