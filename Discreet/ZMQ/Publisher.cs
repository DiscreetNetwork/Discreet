using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.ZMQ
{
    public class Publisher
    {
        private static Publisher _instance;
        public static Publisher Instance { get { if (_instance == null) { _instance = new Publisher(); } return _instance; } }
        
        private CancellationTokenSource _cancellationTokenSource;
        private readonly PublisherSocket _publisherSocket = new PublisherSocket();
        private readonly ConcurrentQueue<Tuple<string, byte[]>> _messageQueue = new ConcurrentQueue<Tuple<string, byte[]>>();

        private Publisher() { }

        public void Start(int port)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _publisherSocket.Bind($"tcp://*:{port}");
            Daemon.Logger.Info($"ZMQ.Publisher: Starting on port {port}");

            while(!_cancellationTokenSource.IsCancellationRequested)
            {
                if (!_messageQueue.TryDequeue(out var message)) continue;

                try
                {
                    _publisherSocket.SendMoreFrame(message.Item1).SendFrame(message.Item2);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        public void Stop()
        {
            Daemon.Logger.Info($"ZMQ.Publisher: Stopping the publisher");
            _publisherSocket.Close();
            _cancellationTokenSource.Cancel();
        }

        public void Publish(string topic, byte[] messageData)
        {
            Daemon.Logger.Info($"ZMQ.Publisher: Publishing message to topic [{topic}]");
            _messageQueue.Enqueue(new Tuple<string, byte[]>(topic, messageData));
        }

        public void Publish(string topic, string message)
        {
            byte[] messageData = Encoding.UTF8.GetBytes(message);
            Publish(topic, messageData);
        }
    }
}
