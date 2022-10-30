using Discreet.Daemon;
using Discreet.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.HTTP
{
    public class HttpServer
    {
        private readonly HttpListener _listener;

        public HttpServer(int portNumber)
        {
            Logger.Info("Server running...");
            RPCEndpointResolver.ReflectEndpoints();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{portNumber}/");
        }

        public async Task Start()
        {
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {

                Daemon.Logger.Error($"Discreet.RPC: {ex.Message}", ex);
            }


            while (true)
            {
                var ctx = await _listener.GetContextAsync();
                var ss = ctx.Request.InputStream;

                StreamReader reader = new(ss);

                HttpProcess processor = new();
                object result = processor.ProcessRemoteCall(this, reader.ReadToEnd());


                using var sw = new StreamWriter(ctx.Response.OutputStream);
                await sw.WriteAsync((string)result);
                await sw.FlushAsync();
            }
        }
    }
}
