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
            Console.WriteLine("Server running...");
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

                Visor.Logger.Log($"Discreet.RPC: {ex.Message}");
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


    }
}
