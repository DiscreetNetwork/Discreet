using Discreet.Coin.Models;
using Discreet.Common.Serialize;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Discreet.Daemon.BlockAuth
{
    public class DefaultBlockAuth
    {
        private static DefaultBlockAuth instance;
        public static DefaultBlockAuth Instance { get { if (instance == null) {  instance = new DefaultBlockAuth(DaemonConfig.GetConfig()); return instance; } else { return instance; } } }

        public AuthKeys Keyring { get; private set; }

        private CancellationTokenSource _cts;
        private readonly PublisherSocket _data = new PublisherSocket();
        private readonly SubscriberSocket _finalize = new SubscriberSocket();
        private readonly Channel<Block> _finalizedBlocks = Channel.CreateUnbounded<Block>();

        public ChannelReader<Block> Finalized { get { return _finalizedBlocks.Reader; } }

        private DefaultBlockAuth(DaemonConfig conf)
        {
            Keyring = new AuthKeys(conf);
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Binds the data publisher socker and finalize socket, and listens
        /// </summary>
        /// <returns></returns>
        public async Task Start(int dataPort, int finalizePort, ChannelWriter<bool> pause)
        {
            _data.Bind($"tcp://*:{dataPort}");
            _finalize.Connect($"tcp://localhost:{finalizePort}");

            _finalize.Subscribe("final");
            _finalize.Subscribe("pause");

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var topic = _finalize.ReceiveFrameBytes();
                    var data = _finalize.ReceiveFrameBytes();

                    Logger.Debug("DefaultBlockAuth: received frame");

                    if (topic == null)
                    {
                        Logger.Error($"DefaultBlockAuth.Start: failed to receive a finalize topic from Aurem");
                    }

                    if (data == null)
                    {
                        Logger.Error("DefaultBlockAuth.Start: failed to received finalized data from Aurem");
                    }

                    var topicS = Encoding.UTF8.GetString(topic);

                    if (topicS == "pause")
                    {
                        if (data.Length > 0 && data[0] == 0)
                        {
                            Logger.Info("DefaultBlockAuth: received signal from Aurem to pause minting", verbose: 3);
                            await pause.WriteAsync(true);
                        }
                        else if (data.Length > 0)
                        {
                            Logger.Info("DefaultBlockAuth: received signal from Aurem to resume minting", verbose: 3);
                            await pause.WriteAsync(false);
                        }
                    }
                    else if (topicS == "final")
                    {
                        var b = new Block();
                        b.Deserialize(data);
                        Logger.Info("DefaultBlockAuth: received finalized data from Aurem", verbose: 3);
                        await _finalizedBlocks.Writer.WriteAsync(b);
                    }
                    else
                    {
                        Logger.Error($"DefaultBlockAuth.Start: failed to receive correct finalize topic from Aurem");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"DefaultBlockAuth: {ex.Message}", ex);
                }
            }
        }

        public void Stop()
        {
            _data.Close();
            _finalize.Close();
            _cts.Cancel();
            _finalizedBlocks.Writer.Complete();
        }

        public void PublishBlockToAurem(Block b)
        {
            Logger.Info($"DefaultBlockAuth: sending block data for height {b.Header.Height} to Aurem", verbose: 3);
            _data.SendMoreFrame("data").SendFrame(b.Serialize());
        }
    }
}
