using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.Network;

namespace Discreet.Wallets
{
    public class WalletTxOutput
    {
        public ulong Amount { get; set; }   
        public string Address { get; set; }

        public void Serialize(Stream s)
        {
            Coin.Serialization.CopyData(s, Amount);
            Coin.Serialization.CopyData(s, Address);
        }

        public void Deserialize(Stream s)
        {
            Amount = Coin.Serialization.GetUInt64(s);
            Address = Coin.Serialization.GetString(s);
        }
    }

    public class WalletTx
    {
        public Cipher.SHA256 TxID { get; set; }
        public long Timestamp { get; set; }
        public int Index { get; set; }

        public WalletTxOutput[] Inputs { get; set; }
        public WalletTxOutput[] Outputs { get; set; }

        private WalletAddress _walletAddress;

        private byte[] _encryptedString;

        public byte[] EncryptedString { get { if (_encryptedString == null) Encrypt(); return _encryptedString; } }

        public WalletTx(WalletAddress address, Cipher.SHA256 tx, ulong[] inputs, string[] senders, ulong[] outputs, string[] receivers, bool sent = false, long timestamp = 0)
        {
            _walletAddress = address;
            TxID = tx;
            Timestamp = timestamp;
            Inputs = new WalletTxOutput[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                Inputs[i] = new WalletTxOutput { Amount = inputs[i], Address = senders[i] };
            }

            Outputs = new WalletTxOutput[outputs.Length];
            for (int i = 0; i < outputs.Length; i++)
            {
                Outputs[i] = new WalletTxOutput { Amount = outputs[i], Address = receivers[i] };
            }

            if (sent) Handler.GetHandler().OnTransactionReceived += OnSuccess;
        }

        public WalletTx() { }

        public WalletTx(WalletAddress address, byte[] encryptedString)
        {
            _walletAddress = address;
            _encryptedString = encryptedString;
        }

        /// <summary>
        /// Commits this transaction to the associated wallet address' history if the tx was successful.
        /// </summary>
        /// <param name="e"></param>
        public void OnSuccess(TransactionReceivedEventArgs e)
        {
            if (e.Tx.Hash() == TxID)
            {
                Handler.GetHandler().OnTransactionReceived -= OnSuccess;
                Timestamp = DateTime.Now.Ticks;

                if (e.Success)
                {
                    // commit to walletaddress' tx history
                    _walletAddress.AddTransactionToHistory(this);
                }
            }
        }

        public byte[] Serialize()
        {
            var _ms = new MemoryStream();

            Serialize(_ms);

            return _ms.ToArray();
        }

        

        public void Encrypt(bool clear = false)
        {
            var key = _walletAddress.GetEncryptionKey();
            Cipher.CipherObject cipherObject = Cipher.AESCBC.GenerateCipherObject(key);

            var unencryptedData = Serialize();
            var data = Cipher.AESCBC.Encrypt(unencryptedData, cipherObject);
            _encryptedString = cipherObject.PrependIV(data);
            // clear data in memory
            if (clear)
            {
                TxID = default;

                foreach (var input in Inputs)
                {
                    input.Amount = 0;
                    input.Address = null;
                }

                foreach (var output in Outputs)
                {
                    output.Amount = 0;
                    output.Address = null;
                }
            }

            Array.Clear(unencryptedData);
            Array.Clear(key);
        }

        public void Decrypt(byte[] data)
        {
            _encryptedString = data;
            Decrypt();
        }

        public void Decrypt()
        {
            var key = _walletAddress.GetEncryptionKey();
            (var cipherObject, var data) = Cipher.CipherObject.GetFromPrependedArray(key, _encryptedString);

            var unencryptedData = Cipher.AESCBC.Decrypt(data, cipherObject);
            Deserialize(new MemoryStream(unencryptedData));

            Array.Clear(unencryptedData);
            Array.Clear(key);
        }

        public void Serialize(Stream s)
        {
            s.Write(TxID.Bytes);

            Coin.Serialization.CopyData(s, Timestamp);
            Coin.Serialization.CopyData(s, Index);

            Coin.Serialization.CopyData(s, Inputs.Length);
            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].Serialize(s);
            }

            Coin.Serialization.CopyData(s, Outputs.Length);
            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].Serialize(s);
            }
        }

        public void Deserialize(Stream s)
        {
            TxID = new Cipher.SHA256(s);

            Timestamp = Coin.Serialization.GetInt64(s);
            Index = Coin.Serialization.GetInt32(s);

            var inputsLength = Coin.Serialization.GetInt32(s);
            Inputs = new WalletTxOutput[inputsLength];
            for (int i = 0; i < inputsLength; i++)
            {
                Inputs[i] = new WalletTxOutput();
                Inputs[i].Deserialize(s);
            }

            var outputsLength = Coin.Serialization.GetInt32(s);
            Outputs = new WalletTxOutput[outputsLength];
            for (int i = 0; i < outputsLength; i++)
            {
                Outputs[i] = new WalletTxOutput();
                Outputs[i].Deserialize(s);
            }
        }
    }
}
