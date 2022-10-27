using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Discreet.RPC.Common.APISetExtensions;

namespace Discreet.RPC
{
    public class RPCServer
    {   
        private readonly HttpListener _listener;
        private int _port;

        private bool _indented = false;
        private int _indentSize = 4;
        private bool _useTabs = false;

        private CancellationTokenSource _cancellationTokenSource;

        public bool Indented { get { return _indented; } set { _indented = value; } }

        public int IndentSize { get { return _indentSize; } set { if (value >=  0) _indentSize = value; } }

        public bool UseTabs { get { return _useTabs; } set { if (Indented) _useTabs = value;} }

        private Daemon.Daemon _daemon;

        private Common.APISet _set;

        public Common.APISet Set { get { return _set;  } private set { _set = value; } }

        public RPCServer(int portNumber, Daemon.Daemon daemon)
        {
            RPCEndpointResolver.ReflectEndpoints();
            _port = portNumber;
            _listener = new HttpListener();
            if(Discreet.Network.Peerbloom.Network.GetNetwork().LocalNode.IsPublic)
            {
                _listener.Prefixes.Add($"http://*:{portNumber}/");
            } else
            {
                _listener.Prefixes.Add($"http://localhost:{portNumber}/");
            }

            _indented = Daemon.DaemonConfig.GetConfig().RPCIndented.HasValue ? Daemon.DaemonConfig.GetConfig().RPCIndented.Value : false;
            _indentSize = Daemon.DaemonConfig.GetConfig().RPCIndentSize.HasValue ? Daemon.DaemonConfig.GetConfig().RPCIndentSize.Value : 4;
            _useTabs = Daemon.DaemonConfig.GetConfig().RPCUseTabs.HasValue ? Daemon.DaemonConfig.GetConfig().RPCUseTabs.Value : false;

            _daemon = daemon;
        }

        public async Task Start()
        {

            _cancellationTokenSource = new CancellationTokenSource();

            Set = CreateSet(Daemon.DaemonConfig.GetConfig().APISets);

            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {

                Daemon.Logger.Error($"Discreet.RPC: {ex.Message}", ex);

                if(ex.ErrorCode == 5)
                    Daemon.Logger.Info($"Discreet.RPC: RPC was unable to start due to insufficient privileges. Please start as administrator and open port {_port}. Continuing without RPC.");
            }

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var ctx = await _listener.GetContextAsync();

                _ = Task.Factory.StartNew(async () =>
                {
                    var ss = ctx.Request.InputStream;

                    StreamReader reader = new(ss);

                    RPCProcess processor = new();
                    object result = processor.ProcessRemoteCall(this, reader.ReadToEnd(), _daemon.RPCLive);

                    using var sw = new StreamWriter(ctx.Response.OutputStream);
                    await sw.WriteAsync((string)result);
                    await sw.FlushAsync();
                });
            }
        }

        public void Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
}
