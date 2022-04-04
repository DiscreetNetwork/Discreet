using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.RPC
{
    public class RPCServer
    {   
        private readonly HttpListener _listener;
        private int _port;

        private bool _indented = false;
        private int _indentSize = 4;
        private bool _useTabs = false;

        public bool Indented { get { return _indented; } set { _indented = value; } }

        public int IndentSize { get { return _indentSize; } set { if (value >=  0) _indentSize = value; } }

        public bool UseTabs { get { return _useTabs; } set { if (Indented) _useTabs = value;} }

        private Daemon.Daemon _daemon;

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

            _daemon = daemon;
        }

        public RPCServer(int portNumber, bool indented, int indentSize, bool useTabs, Daemon.Daemon daemon) : this(portNumber, daemon)
        {
            _indented = indented;
            _indentSize = indentSize;
            _useTabs = useTabs;
        }

        public async Task Start()
        {
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {

                Daemon.Logger.Log($"Discreet.RPC: {ex.Message}");

                if(ex.ErrorCode == 5)
                    Daemon.Logger.Info($"Discreet.RPC: RPC was unable to start due to insufficient privileges. Please start as administrator and open port {_port}. Continuing without RPC.");
            }
          
            while (!_daemon.RPCLive)
            {
                await Task.Delay(250);
            }

            while (true)
            {
                var ctx = await _listener.GetContextAsync();
                var ss = ctx.Request.InputStream;

                StreamReader reader = new(ss);

                RPCProcess processor = new();
                object result = processor.ProcessRemoteCall(this, reader.ReadToEnd());


                using var sw = new StreamWriter(ctx.Response.OutputStream);
                await sw.WriteAsync((string)result);
                await sw.FlushAsync();
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
