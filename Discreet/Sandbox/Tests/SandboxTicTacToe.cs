﻿using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Coin.Models;
using Discreet.Coin.Script;
using Discreet.Common;
using Discreet.Common.Serialize;
using Discreet.Scripting;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox.Tests
{
    /// <summary>
    /// Debug code for testing default debug app (Tic-Tac-Toe)
    /// </summary>
    public class SandboxTicTacToe
    {
        public static bool CheckWin(byte[] boardState)
        {
            for (int i = 0; i < 3; i++)
            {
                if (boardState[1 + i] != 0 && boardState[i + 1] == boardState[i + 4] && boardState[i + 4] == boardState[i + 7])
                {
                    return true;
                }

                if (boardState[3 * i + 1] != 0 && boardState[3 * i + 1] == boardState[3 * i + 2] && boardState[3 * i + 2] == boardState[3 * i + 3])
                {
                    return true;
                }
            }

            if (boardState[1] != 0 && boardState[1] == boardState[5] && boardState[5] == boardState[9])
            {
                return true;
            }

            if (boardState[3] != 0 && boardState[3] == boardState[5] && boardState[5] == boardState[7])
            {
                return true;
            }

            return false;
        }

        public static bool CheckWin(byte[] boardState, byte who)
        {
            for (int i = 0; i < 3; i++)
            {
                if (boardState[1 + i] == who && boardState[i + 1] == boardState[i + 4] && boardState[i + 4] == boardState[i + 7])
                {
                    return true;
                }

                if (boardState[3 * i + 1] == who && boardState[3 * i + 1] == boardState[3 * i + 2] && boardState[3 * i + 2] == boardState[3 * i + 3])
                {
                    return true;
                }
            }

            if (boardState[1] == who && boardState[1] == boardState[5] && boardState[5] == boardState[9])
            {
                return true;
            }

            if (boardState[3] == who && boardState[3] == boardState[5] && boardState[5] == boardState[7])
            {
                return true;
            }

            return false;
        }

        public static FullTransaction CreateTTTGame(TAddress x, TAddress o, SandboxWallet w, int initialMove = -1, ulong earliestRedeem = 10)
        {
            var cod = DVMAParserV1.ParseString(TicTacToeAsm.Program);
            if (w.Utxos.Count == 0)
            {
                throw new Exception("Missing input!");
            }

            byte[] scriptData = new byte[160];
            scriptData[31] = (byte)(x.ToString() == w.Address ? 1 : 0);
            Array.Copy(x.Bytes(), 0, scriptData, 39, 25);
            Array.Copy(o.Bytes(), 0, scriptData, 71, 25);

            if (w.Type == 0)
            {
                throw new Exception("Expected a transparent wallet");
            }

            var utxo = w.Utxos.Where(x => x.Type == 1 && x.DecodedAmount >= SandboxWallet.CoinsToDroplets(2)).FirstOrDefault();
            if (utxo == null)
            {
                throw new Exception("Could not find utxo with enough coins");
            }

            var input = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };
            Array.Copy(utxo.TxSrc.Bytes, 0, scriptData, 96, 32);
            Array.Copy(Serialization.UInt64(earliestRedeem), 0, scriptData, 152, 8);

            ChainScript script = new ChainScript { Version = 1, Code = cod, Data = scriptData };

            var scriptOut = new ScriptTXOutput { Address = new ScriptAddress(script), Amount = SandboxWallet.CoinsToDroplets(1), ReferenceScript = script };

            byte[] boardState = new byte[10];
            boardState[0] = 0x58;
            if (x.ToString() == w.Address)
            {
                if (initialMove > -1)
                {
                    if (initialMove >= 9)
                    {
                        throw new Exception("initial move is out of range");
                    }

                    boardState[0] = 0x4f;
                    boardState[1 + initialMove] = 0x58;
                }
            }

            scriptOut.Datum = new Datum { Version = 0, Data = boardState };
            scriptOut.DatumHash = scriptOut.Datum.Hash();

            var coinOut = new ScriptTXOutput(default, new TAddress(w.Address), utxo.DecodedAmount - scriptOut.Amount);

            var tx = new FullTransaction
            {
                Version = 4,
                NumInputs = 1,
                NumOutputs = 2,
                NumSigs = 1,
                NumPInputs = 0,
                NumPOutputs = 0,
                NumTInputs = 1,
                NumTOutputs = 2,
                NumRefInputs = 0,
                NumScriptInputs = 0,
                TInputs = [input],
                RefInputs = [],
                TOutputs = [coinOut, scriptOut],
                _datums = [],
                _redeemers = [],
                _scripts = [],
                TransactionKey = default,
                PInputs = [],
                POutputs = [],
                PSignatures = [],
                PseudoOutputs = [],
                ValidityInterval = (-1, long.MaxValue),
                Fee = 0,
            };

            tx.SigningHash = tx.GetSigningHash();
            byte[] signData = new byte[64];
            Array.Copy(tx.SigningHash.Bytes, signData, 32);
            Array.Copy(input.Hash(new TTXOutput { TransactionSrc = utxo.TxSrc, Address = new TAddress(w.Address), Amount = utxo.DecodedAmount }).Bytes, 0, signData, 32, 32);
            var signHash = SHA256.HashData(signData);
            tx.TSignatures = [(0, new Signature(w.SecKey.Value, w.PubKey.Value, signHash))];

            // produces the side-effect of having the wallet 
            return tx;
        }

        public static FullTransaction TakeXTurn(ScriptTXOutput scriptOut, int index, SandboxWallet w, int move)
        {
            if (w.Type == 0)
            {
                throw new Exception("Expected a transparent wallet");
            }

            if (move < 0 || move > 8)
            {
                throw new Exception("Move out of range");
            }

            var utxo = w.Utxos.Where(x => x.Type == 1).FirstOrDefault();
            if (utxo == null)
            {
                throw new Exception("Could not find utxo with enough coins");
            }

            if (scriptOut.Datum == null || scriptOut.Datum.Data == null || scriptOut.Datum.Data.Length != 10)
            {
                throw new Exception("Script datum invalid");
            }

            var xaddr = new TAddress(scriptOut.ReferenceScript.Data[39..64]);
            if (xaddr.ToString() != w.Address)
            {
                throw new Exception("wallet address does not match script x address");
            }

            var boardState = scriptOut.Datum.Data;
            if (boardState[0] != 0x58)
            {
                throw new Exception("Board state indicates it is not X's turn");
            }

            if (boardState[1 + move] != 0)
            {
                throw new Exception($"Board state already contains an {(boardState[1 + move] == 0x58 ? "X" : "O")} at position {move}");
            }

            if (CheckWin(boardState))
            {
                throw new Exception("Board state indicates a win has already occurred");
            }

            var newBoardState = new byte[10];
            Array.Copy(boardState, newBoardState, 10);
            newBoardState[1 + move] = 0x58;
            newBoardState[0] = (byte)(CheckWin(newBoardState) ? 0 : 0x4f);

            var rdmSigData = new byte[0xB0];
            scriptOut.ReferenceScript.Data.CopyTo(rdmSigData, 0);
            Array.Copy(scriptOut.Datum.Data, 0, rdmSigData, 0xa0, 10);
            var rdmSigHash = SHA256.HashData(rdmSigData);
            var rdmSig = new Signature(w.SecKey.Value, w.PubKey.Value, rdmSigHash);
            var redeemerData = new byte[160];
            Array.Copy(rdmSig.Serialize(), redeemerData, 96);
            redeemerData[127] = (byte)move;
            redeemerData[159] = 0;

            var redeemer = new Datum { Version = 0, Data = redeemerData };

            var scriptInput = new TTXInput { TxSrc = scriptOut.TransactionSrc, Offset = (byte)index };
            var intput = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };

            var coinOut = new ScriptTXOutput(default, new TAddress(w.Address), utxo.DecodedAmount);
            var newScriptOut = new ScriptTXOutput { Address = scriptOut.Address, Amount = scriptOut.Amount, ReferenceScript = scriptOut.ReferenceScript, Datum = new Datum { Version = 0, Data = newBoardState } };
            newScriptOut.DatumHash = newScriptOut.Datum.Hash();

            var tx = new FullTransaction
            {
                Version = 4,
                NumInputs = 2,
                NumOutputs = 2,
                NumSigs = 1,
                NumPInputs = 0,
                NumPOutputs = 0,
                NumTInputs = 2,
                NumTOutputs = 2,
                NumRefInputs = 0,
                NumScriptInputs = 1,
                TInputs = [intput, scriptInput],
                RefInputs = [],
                TOutputs = [coinOut, newScriptOut],
                _datums = [],
                _redeemers = [(1, redeemer)],
                _scripts = [],
                TransactionKey = default,
                PInputs = [],
                POutputs = [],
                PSignatures = [],
                PseudoOutputs = [],
                ValidityInterval = (-1, long.MaxValue),
                Fee = 0,
            };

            tx.SigningHash = tx.GetSigningHash();
            byte[] signData = new byte[64];
            Array.Copy(tx.SigningHash.Bytes, signData, 32);
            Array.Copy(intput.Hash(new TTXOutput { TransactionSrc = utxo.TxSrc, Address = new TAddress(w.Address), Amount = utxo.DecodedAmount }).Bytes, 0, signData, 32, 32);
            var signHash = SHA256.HashData(signData);
            tx.TSignatures = [(0, new Signature(w.SecKey.Value, w.PubKey.Value, signHash))];

            return tx;
        }

        public static FullTransaction TakeOTurn(ScriptTXOutput scriptOut, int index, SandboxWallet w, int move)
        {
            if (w.Type == 0)
            {
                throw new Exception("Expected a transparent wallet");
            }

            if (move < 0 || move > 8)
            {
                throw new Exception("Move out of range");
            }

            var utxo = w.Utxos.Where(x => x.Type == 1).FirstOrDefault();
            if (utxo == null)
            {
                throw new Exception("Could not find utxo with enough coins");
            }

            if (scriptOut.Datum == null || scriptOut.Datum.Data == null || scriptOut.Datum.Data.Length != 10)
            {
                throw new Exception("Script datum invalid");
            }

            var oaddr = new TAddress(scriptOut.ReferenceScript.Data[71..96]);
            if (oaddr.ToString() != w.Address)
            {
                throw new Exception("wallet address does not match script o address");
            }

            var boardState = scriptOut.Datum.Data;
            if (boardState[0] != 0x4f)
            {
                throw new Exception("Board state indicates it is not X's turn");
            }

            if (boardState[1 + move] != 0)
            {
                throw new Exception($"Board state already contains an {(boardState[1 + move] == 0x58 ? "X" : "O")} at position {move}");
            }

            if (CheckWin(boardState))
            {
                throw new Exception("Board state indicates a win has already occurred");
            }

            var newBoardState = new byte[10];
            Array.Copy(boardState, newBoardState, 10);
            newBoardState[1 + move] = 0x4f;
            newBoardState[0] = (byte)(CheckWin(newBoardState) ? 0 : 0x58);

            var rdmSigData = new byte[0xB0];
            scriptOut.ReferenceScript.Data.CopyTo(rdmSigData, 0);
            Array.Copy(scriptOut.Datum.Data, 0, rdmSigData, 0xa0, 10);
            var rdmSigHash = SHA256.HashData(rdmSigData);
            var rdmSig = new Signature(w.SecKey.Value, w.PubKey.Value, rdmSigHash);
            var redeemerData = new byte[160];
            Array.Copy(rdmSig.Serialize(), redeemerData, 96);
            redeemerData[127] = (byte)move;
            redeemerData[159] = 1;

            var redeemer = new Datum { Version = 0, Data = redeemerData };

            var scriptInput = new TTXInput { TxSrc = scriptOut.TransactionSrc, Offset = (byte)index };
            var intput = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };

            var coinOut = new ScriptTXOutput(default, new TAddress(w.Address), utxo.DecodedAmount);
            var newScriptOut = new ScriptTXOutput { Address = scriptOut.Address, Amount = scriptOut.Amount, ReferenceScript = scriptOut.ReferenceScript, Datum = new Datum { Version = 0, Data = newBoardState } };
            newScriptOut.DatumHash = newScriptOut.Datum.Hash();

            var tx = new FullTransaction
            {
                Version = 4,
                NumInputs = 2,
                NumOutputs = 2,
                NumSigs = 1,
                NumPInputs = 0,
                NumPOutputs = 0,
                NumTInputs = 2,
                NumTOutputs = 2,
                NumRefInputs = 0,
                NumScriptInputs = 1,
                TInputs = [intput, scriptInput],
                RefInputs = [],
                TOutputs = [coinOut, newScriptOut],
                _datums = [],
                _redeemers = [(1, redeemer)],
                _scripts = [],
                TransactionKey = default,
                PInputs = [],
                POutputs = [],
                PSignatures = [],
                PseudoOutputs = [],
                ValidityInterval = (-1, long.MaxValue),
                Fee = 0,
            };

            tx.SigningHash = tx.GetSigningHash();
            byte[] signData = new byte[64];
            Array.Copy(tx.SigningHash.Bytes, signData, 32);
            Array.Copy(intput.Hash(new TTXOutput { TransactionSrc = utxo.TxSrc, Address = new TAddress(w.Address), Amount = utxo.DecodedAmount }).Bytes, 0, signData, 32, 32);
            var signHash = SHA256.HashData(signData);
            tx.TSignatures = [(0, new Signature(w.SecKey.Value, w.PubKey.Value, signHash))];

            return tx;
        }

        public static FullTransaction RedeemBet(ScriptTXOutput scriptOut, int index, SandboxWallet w)
        {
            if (w.Type == 0)
            {
                throw new Exception("Expected a transparent wallet");
            }

            var utxo = w.Utxos.Where(x => x.Type == 1).FirstOrDefault();
            if (utxo == null)
            {
                throw new Exception("Could not find utxo with enough coins");
            }

            if (scriptOut.Datum == null || scriptOut.Datum.Data == null || scriptOut.Datum.Data.Length != 10)
            {
                throw new Exception("Script datum invalid");
            }

            var xorOCreator = scriptOut.ReferenceScript.Data[31];
            var xaddr = new TAddress(scriptOut.ReferenceScript.Data[39..64]);
            if (xorOCreator == 1 && xaddr.ToString() != w.Address)
            {
                throw new Exception("wallet address does not match script x address");
            }

            var lowestValidityInterval = Serialization.GetInt64(scriptOut.ReferenceScript.Data, 152);
            if (lowestValidityInterval > DB.ViewProvider.GetDefaultProvider().GetChainHeight())
            {
                throw new Exception("Earliest redeem interval not met!");
            }

            var boardState = scriptOut.Datum.Data;

            var rdmSigData = new byte[0xB0];
            scriptOut.ReferenceScript.Data.CopyTo(rdmSigData, 0);
            Array.Copy(scriptOut.Datum.Data, 0, rdmSigData, 0xa0, 10);
            var rdmSigHash = SHA256.HashData(rdmSigData);
            var rdmSig = new Signature(w.SecKey.Value, w.PubKey.Value, rdmSigHash);
            var redeemerData = new byte[160];
            Array.Copy(rdmSig.Serialize(), redeemerData, 96);
            redeemerData[159] = 3;

            var redeemer = new Datum { Version = 0, Data = redeemerData };

            var scriptInput = new TTXInput { TxSrc = scriptOut.TransactionSrc, Offset = (byte)index };
            var intput = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };

            var coinOut = new ScriptTXOutput(default, new TAddress(w.Address), utxo.DecodedAmount + scriptOut.Amount);

            var tx = new FullTransaction
            {
                Version = 4,
                NumInputs = 2,
                NumOutputs = 1,
                NumSigs = 1,
                NumPInputs = 0,
                NumPOutputs = 0,
                NumTInputs = 2,
                NumTOutputs = 1,
                NumRefInputs = 0,
                NumScriptInputs = 1,
                TInputs = [intput, scriptInput],
                RefInputs = [],
                TOutputs = [coinOut],
                _datums = [],
                _redeemers = [(1, redeemer)],
                _scripts = [],
                TransactionKey = default,
                PInputs = [],
                POutputs = [],
                PSignatures = [],
                PseudoOutputs = [],
                ValidityInterval = (lowestValidityInterval, long.MaxValue),
                Fee = 0,
            };

            tx.SigningHash = tx.GetSigningHash();
            byte[] signData = new byte[64];
            Array.Copy(tx.SigningHash.Bytes, signData, 32);
            Array.Copy(intput.Hash(new TTXOutput { TransactionSrc = utxo.TxSrc, Address = new TAddress(w.Address), Amount = utxo.DecodedAmount }).Bytes, 0, signData, 32, 32);
            var signHash = SHA256.HashData(signData);
            tx.TSignatures = [(0, new Signature(w.SecKey.Value, w.PubKey.Value, signHash))];

            return tx;
        }

        public static FullTransaction ClaimWin(ScriptTXOutput scriptOut, int index, SandboxWallet w)
        {
            if (w.Type == 0)
            {
                throw new Exception("Expected a transparent wallet");
            }

            var utxo = w.Utxos.Where(x => x.Type == 1).FirstOrDefault();
            if (utxo == null)
            {
                throw new Exception("Could not find utxo with enough coins");
            }

            if (scriptOut.Datum == null || scriptOut.Datum.Data == null || scriptOut.Datum.Data.Length != 10)
            {
                throw new Exception("Script datum invalid");
            }

            var xaddr = new TAddress(scriptOut.ReferenceScript.Data[39..64]);
            var oaddr = new TAddress(scriptOut.ReferenceScript.Data[71..96]);
            if (xaddr.ToString() != w.Address && oaddr.ToString() != w.Address)
            {
                throw new Exception("wallet address does not match script x or o address");
            }

            var who = xaddr.ToString() == w.Address ? 0x58 : 0x4f;

            var boardState = scriptOut.Datum.Data;
            if (boardState[0] != 0x00)
            {
                throw new Exception("Board state indicates it is not complete");
            }

            if (!CheckWin(boardState))
            {
                throw new Exception("Board state indicates a win has not occurred");
            }

            if (!CheckWin(boardState, (byte)who))
            {
                throw new Exception($"Board was not won by {(who == 0x58 ? "X" : "O")}");
            }

            var rdmSigData = new byte[0xB0];
            scriptOut.ReferenceScript.Data.CopyTo(rdmSigData, 0);
            Array.Copy(scriptOut.Datum.Data, 0, rdmSigData, 0xa0, 10);
            var rdmSigHash = SHA256.HashData(rdmSigData);
            var rdmSig = new Signature(w.SecKey.Value, w.PubKey.Value, rdmSigHash);
            var redeemerData = new byte[160];
            Array.Copy(rdmSig.Serialize(), redeemerData, 96);
            redeemerData[159] = 2;

            var redeemer = new Datum { Version = 0, Data = redeemerData };

            var scriptInput = new TTXInput { TxSrc = scriptOut.TransactionSrc, Offset = (byte)index };
            var intput = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };

            var coinOut = new ScriptTXOutput(default, new TAddress(w.Address), scriptOut.Amount + utxo.DecodedAmount);

            var tx = new FullTransaction
            {
                Version = 4,
                NumInputs = 2,
                NumOutputs = 1,
                NumSigs = 1,
                NumPInputs = 0,
                NumPOutputs = 0,
                NumTInputs = 2,
                NumTOutputs = 1,
                NumRefInputs = 0,
                NumScriptInputs = 1,
                TInputs = [intput, scriptInput],
                RefInputs = [],
                TOutputs = [coinOut],
                _datums = [],
                _redeemers = [(1, redeemer)],
                _scripts = [],
                TransactionKey = default,
                PInputs = [],
                POutputs = [],
                PSignatures = [],
                PseudoOutputs = [],
                ValidityInterval = (-1, long.MaxValue),
                Fee = 0,
            };

            tx.SigningHash = tx.GetSigningHash();
            byte[] signData = new byte[64];
            Array.Copy(tx.SigningHash.Bytes, signData, 32);
            Array.Copy(intput.Hash(new TTXOutput { TransactionSrc = utxo.TxSrc, Address = new TAddress(w.Address), Amount = utxo.DecodedAmount }).Bytes, 0, signData, 32, 32);
            var signHash = SHA256.HashData(signData);
            tx.TSignatures = [(0, new Signature(w.SecKey.Value, w.PubKey.Value, signHash))];

            return tx;
        }
    }
}
