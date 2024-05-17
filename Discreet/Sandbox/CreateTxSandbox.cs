using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Coin.Models;
using Discreet.Coin.Script;
using Discreet.Common;
using Discreet.Common.Serialize;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Sandbox
{
    /// <summary>
    /// Debug code for testing default debug app (Tic-Tac-Toe)
    /// </summary>
    public class CreateTxSandbox
    {
        public static ulong CoinsToDroplets(int c)
        {
            return (ulong)c * 1_000_000_000_0ul;
        }

        public static bool CheckWin(byte[] boardState)
        {
            for (int i = 0; i < 3; i++)
            {
                if (boardState[1 + i] != 0 && ((boardState[i + 1] == boardState[i + 4]) && (boardState[i + 4] == boardState[i + 7])))
                {
                    return true;
                }

                if (boardState[3*i + 1] != 0 && ((boardState[3*i + 1] == boardState[3*i + 2]) && (boardState[3*i + 2] == boardState[3*i + 3])))
                {
                    return true;
                }
            }

            if (boardState[1] != 0 && ((boardState[1] == boardState[5]) && (boardState[5] == boardState[9])))
            {
                return true;
            }

            if (boardState[3] != 0 && ((boardState[3] == boardState[5]) && (boardState[5] == boardState[7])))
            {
                return true;
            }

            return false;
        }

        public static bool CheckWin(byte[] boardState, byte who)
        {
            for (int i = 0; i < 3; i++)
            {
                if (boardState[1 + i] == who && ((boardState[i + 1] == boardState[i + 4]) && (boardState[i + 4] == boardState[i + 7])))
                {
                    return true;
                }

                if (boardState[3 * i + 1] == who && ((boardState[3 * i + 1] == boardState[3 * i + 2]) && (boardState[3 * i + 2] == boardState[3 * i + 3])))
                {
                    return true;
                }
            }

            if (boardState[1] == who && ((boardState[1] == boardState[5]) && (boardState[5] == boardState[9])))
            {
                return true;
            }

            if (boardState[3] == who && ((boardState[3] == boardState[5]) && (boardState[5] == boardState[7])))
            {
                return true;
            }

            return false;
        }

        public FullTransaction CreateTTTGame(TAddress x, TAddress o, SandboxWallet w, int initialMove = -1, ulong earliestRedeem = 10)
        {
            const string bytecode = "3080285050501133000001852430011033000002ab2430021033000003d12430031033000000832433000004be23300930013101172c310100202f2f5252163058100151300810330000005924513001016180330000003c23618080602731010030a02f522e300a2f3101a02c3101b060d031020030602f522960d333000004be2427330000002e25502f1060302020a2a830021033000004be24300114ab102f201260300110121330011433000004be308020a50c33000004be24330000005e25fca0bc30016002502f1033000000e6245151bd1033000000ec243001600233000000c92333000004be2350c3300a1030011433000004be246080300a2f3104162f63c13104002061608027582f103001145957101256541012582f10300114595710125654101213572f10300114585610125553101213592f103001145a5a10125959101213562f10300114575710125656101213532f103001145454101253531213592f103001145a5710125653101213572f103001145857101256551012136a27330000005e25302020a2a830021033000004be24300114ab1033000004be243060282f510d2f5210133009520c1230011433000004be24300a2f3103162c310300202f513009165030581033000004be2452300816533007165430061655300516563004165730031658300216593001165a2f1633000002066a80330000010d238080808080808080808030011433000004be8033000000c3252f5130091652300816533007165430061655300516563004165730031658300216593001165a2f162433000002556a80330000010d2380808080808080808051512f10125230011452304f10121330011433000004be24f8808038ffffffffffffffffff123103002038ffffffffffffffffff12306028300802300303305817011030011433000004be24fc330000005e25304020a2a830021033000004be24300114ab1033000004be243060282f510d2f5210133009520c1230011433000004be24300a2f3103162c310300202f5130091650304f1033000004be2452300816533007165430061655300516563004165730031658300216593001165a2f16330000032c6a80330000010d238080808080808080808030011433000004be8033000000c3252f5130091652300816533007165430061655300516563004165730031658300216593001165a2f1624330000037b6a80330000010d2380808080808080808051512f10125230011452305810121330011433000004be24f8808038ffffffffffffffffff123103002038ffffffffffffffffff12306028300802300303304f17011030011433000004be24fca2a830021033000004be24300114ab302020511033000003f724304020511033000003ff24fb3058330000040123304f300a2f3102162c3102002050300916502f1030011433000004be2451300816523007165330061654300516553004165630031657300216583001165916585c103001145957101256541012585d10300114595710125654101213575d10300114585610125553101213595d103001145a5a10125959101213565d10300114575710125656101213535d103001145454101253531213595d103001145a5710125653101213575d1030011458571012565510121330011433000004be24fcfb";
            if (w.Utxos.Count == 0)
            {
                throw new Exception("Missing input!");
            }

            byte[] scriptData = new byte[160];
            scriptData[31] = (byte)((x.ToString() == w.Address) ? 1 : 0);
            Array.Copy(x.Bytes(), 0, scriptData, 39, 25);
            Array.Copy(o.Bytes(), 0, scriptData, 71, 25);

            if (w.Type == 0)
            {
                throw new Exception("Expected a transparent wallet");
            }

            var utxo = w.Utxos.Where(x => x.Type == 1 && x.DecodedAmount >= CoinsToDroplets(2)).FirstOrDefault();
            if (utxo == null)
            {
                throw new Exception("Could not find utxo with enough coins");
            }

            var input = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };
            Array.Copy(utxo.TxSrc.Bytes, 0, scriptData, 96, 32);
            Array.Copy(Common.Serialization.UInt64(earliestRedeem), 0, scriptData, 152, 8);

            ChainScript script = new ChainScript { Version = 1, Code = Printable.Byteify(bytecode), Data = scriptData };

            var scriptOut = new ScriptTXOutput { Address = new ScriptAddress(script), Amount = CoinsToDroplets(1), ReferenceScript = script };

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

        public FullTransaction TakeXTurn(ScriptTXOutput scriptOut, int index, SandboxWallet w, int move)
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

            var xaddr = new TAddress(scriptOut.Datum.Data[39..64]);
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
                NumScriptInputs = 0,
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

        public FullTransaction TakeOTurn(ScriptTXOutput scriptOut, int index, SandboxWallet w, int move)
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

            var oaddr = new TAddress(scriptOut.Datum.Data[71..96]);
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
                NumScriptInputs = 0,
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

        public FullTransaction RedeemBet(ScriptTXOutput scriptOut, int index, SandboxWallet w)
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

            var xaddr = new TAddress(scriptOut.Datum.Data[39..64]);
            if (xaddr.ToString() != w.Address)
            {
                throw new Exception("wallet address does not match script x address");
            }

            var lowestValidityInterval = Common.Serialization.GetInt64(scriptOut.ReferenceScript.Data, 152);
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
            var redeemerData = new byte[96];
            Array.Copy(rdmSig.Serialize(), redeemerData, 96);

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
                NumTOutputs = 2,
                NumRefInputs = 0,
                NumScriptInputs = 0,
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

        public FullTransaction ClaimWin(ScriptTXOutput scriptOut, int index, SandboxWallet w)
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

            var xaddr = new TAddress(scriptOut.Datum.Data[39..64]);
            var oaddr = new TAddress(scriptOut.Datum.Data[71..96]);
            if (xaddr.ToString() != w.Address && oaddr.ToString() != w.Address)
            {
                throw new Exception("wallet address does not match script x or o address");
            }

            var who = (xaddr.ToString() == w.Address) ? 0x58 : 0x4f;

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
            var redeemerData = new byte[96];
            Array.Copy(rdmSig.Serialize(), redeemerData, 96);

            var redeemer = new Datum { Version = 0, Data = redeemerData };

            var scriptInput = new TTXInput { TxSrc = scriptOut.TransactionSrc, Offset = (byte)index };
            var intput = new TTXInput { TxSrc = utxo.TxSrc, Offset = (byte)utxo.OutputIndex };

            var coinOut = new ScriptTXOutput(default, new TAddress(w.Address), utxo.DecodedAmount);

            var tx = new FullTransaction
            {
                Version = 4,
                NumInputs = 2,
                NumOutputs = 1,
                NumSigs = 1,
                NumPInputs = 0,
                NumPOutputs = 0,
                NumTInputs = 2,
                NumTOutputs = 2,
                NumRefInputs = 0,
                NumScriptInputs = 0,
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
