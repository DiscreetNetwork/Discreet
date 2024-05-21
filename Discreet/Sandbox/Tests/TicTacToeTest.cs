using Discreet.Coin.Models;
using Discreet.Coin;
using Discreet.Daemon.BlockAuth;
using Discreet.Daemon;
using Discreet.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Discreet.Sandbox.Tests
{
    internal class TicTacToeTest
    {
        public static void Simulate()
        {
            var daemon = SandboxWallet._daemon;
            var pwt = new SandboxWallet("Private Wallet", true);
            var blk = Block.BuildGenesis([new StealthAddress(pwt.Address)], [SandboxWallet.CoinsToDroplets(100)], 4096, DefaultBlockAuth.Instance.Keyring.SigningKeys.First());
            daemon.SandboxMint(blk);

            var w1 = new SandboxWallet("Wallet 1", false);
            var w2 = new SandboxWallet("Wallet 2", false);

            var tx = pwt.CreateTransaction([(SandboxWallet.CoinsToDroplets(10), new TAddress(w1.Address)), (SandboxWallet.CoinsToDroplets(10), new TAddress(w2.Address))]);
            daemon.SandboxTransaction(tx);
            daemon.SandboxMint();

            var txMakeGame = SandboxTicTacToe.CreateTTTGame(new TAddress(w1.Address), new TAddress(w2.Address), w1, 0);
            daemon.SandboxTransaction(txMakeGame);
            daemon.SandboxMint();

            var txOTurn1 = SandboxTicTacToe.TakeOTurn(txMakeGame.TOutputs[1], 1, w2, 1);
            daemon.SandboxTransaction(txOTurn1);
            daemon.SandboxMint();

            var txXTurn2 = SandboxTicTacToe.TakeXTurn(txOTurn1.TOutputs[1], 1, w1, 2);
            daemon.SandboxTransaction(txXTurn2);
            daemon.SandboxMint();

            var txOTurn2 = SandboxTicTacToe.TakeOTurn(txXTurn2.TOutputs[1], 1, w2, 5);
            daemon.SandboxTransaction(txOTurn2);
            daemon.SandboxMint();

            var txXTurn3 = SandboxTicTacToe.TakeXTurn(txOTurn2.TOutputs[1], 1, w1, 3);
            daemon.SandboxTransaction(txXTurn3);
            daemon.SandboxMint();

            var txOTurn3 = SandboxTicTacToe.TakeOTurn(txXTurn3.TOutputs[1], 1, w2, 6);
            daemon.SandboxTransaction(txOTurn3);
            daemon.SandboxMint();

            var txXTurn4 = SandboxTicTacToe.TakeXTurn(txOTurn3.TOutputs[1], 1, w1, 4);
            daemon.SandboxTransaction(txXTurn4);
            daemon.SandboxMint();

            var txOTurn4 = SandboxTicTacToe.TakeOTurn(txXTurn4.TOutputs[1], 1, w2, 8);
            daemon.SandboxTransaction(txOTurn4);
            daemon.SandboxMint();
            
            var txXTurn5 = SandboxTicTacToe.TakeXTurn(txOTurn4.TOutputs[1], 1, w1, 7);
            daemon.SandboxTransaction(txXTurn5);
            daemon.SandboxMint();

            for (int i = 0; i < 10; i++)
            {
                daemon.SandboxMint();
            }

            // redeem due to tie
            var redeem = SandboxTicTacToe.RedeemBet(txXTurn5.TOutputs[1], 1, w1);
            daemon.SandboxTransaction(redeem);
            daemon.SandboxMint();

            // board should be won
            //var txClaimWin = SandboxTicTacToe.ClaimWin(txOTurn3.TOutputs[1], 1, w2);
            //daemon.SandboxTransaction(txClaimWin);
            //daemon.SandboxMint();

            // wait until slot
            //for (int i = 0; i < 10; i++)
            //{
            //    daemon.SandboxMint();
            //}

            //var txRedeem = SandboxTicTacToe.RedeemBet(txMakeGame.TOutputs[1], 1, w1);
            //daemon.SandboxTransaction(txRedeem);
            //daemon.SandboxMint();
        }
    }
}
