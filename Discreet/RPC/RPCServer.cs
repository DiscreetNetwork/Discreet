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

        public RPCServer(int portNumber)
        {
            Console.WriteLine("Server running...");
            RPCEndpointResolver.ReflectEndpoints();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{portNumber}/");

        }

        public async Task Start()
        {
            _listener.Start();

            while (true)
            {
                var ctx = await _listener.GetContextAsync();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () => HandleRequest(ctx));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public async Task HandleRequest(HttpListenerContext ctx)
        {
            var ss = ctx.Request.InputStream;

            StreamReader reader = new StreamReader(ss);

            RPCProcess processor = new RPCProcess();
            object result = processor.ProcessRemoteCall(reader.ReadToEnd());


            using (var sw = new StreamWriter(ctx.Response.OutputStream))
            {
                await sw.WriteAsync((string)result);
                await sw.FlushAsync();
            }
        }

        public async Task Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }
}
