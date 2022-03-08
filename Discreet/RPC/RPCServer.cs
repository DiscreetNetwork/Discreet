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

        public RPCServer(int portNumber)
        {
            Visor.Logger.Log("Starting RPC server");
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
            
        }

        public async Task Start()
        {
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {

                Visor.Logger.Log($"Discreet.RPC: {ex.Message}");

                if(ex.ErrorCode == 5)
                    Visor.Logger.Info($"Discreet.RPC: RPC was unable to start due to insufficient privileges. Please start as administrator and open port {_port}. Continuing without RPC.");
            }
          

            while (true)
            {
                var ctx = await _listener.GetContextAsync();
                var ss = ctx.Request.InputStream;

                StreamReader reader = new(ss);

                RPCProcess processor = new();
                object result = processor.ProcessRemoteCall(reader.ReadToEnd());


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
