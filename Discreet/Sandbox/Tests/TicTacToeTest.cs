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

namespace Discreet.Sandbox.Tests
{
    internal class TicTacToeTest
    {
        public static void SimulateOWin()
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

            var txMakeGame = SandboxTicTacToe.CreateTTTGame(new TAddress(w1.Address), new TAddress(w2.Address), w1, 4);
            daemon.SandboxTransaction(txMakeGame);
            daemon.SandboxMint();

            var txOTurn1 = SandboxTicTacToe.TakeOTurn(txMakeGame.TOutputs[1], 1, w2, 3);
            daemon.SandboxTransaction(txOTurn1);
            daemon.SandboxMint();

            var txXTurn2 = SandboxTicTacToe.TakeXTurn(txOTurn1.TOutputs[1], 1, w1, 1);
            daemon.SandboxTransaction(txXTurn2);
            daemon.SandboxMint();

            var txOTurn2 = SandboxTicTacToe.TakeOTurn(txXTurn2.TOutputs[1], 1, w2, 0);
            daemon.SandboxTransaction(txOTurn2);
            daemon.SandboxMint();

            var txXTurn3 = SandboxTicTacToe.TakeXTurn(txOTurn2.TOutputs[1], 1, w1, 8);
            daemon.SandboxTransaction(txXTurn3);
            daemon.SandboxMint();

            var txOTurn3 = SandboxTicTacToe.TakeOTurn(txXTurn3.TOutputs[1], 1, w2, 6);
            daemon.SandboxTransaction(txOTurn3);
            daemon.SandboxMint();

            // board should be won
            var txClaimWin = SandboxTicTacToe.ClaimWin(txOTurn3.TOutputs[1], 1, w2);
            daemon.SandboxTransaction(txClaimWin);
            daemon.SandboxMint();

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
