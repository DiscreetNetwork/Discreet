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
            Visor.Logger.Log("Starting RPC server");
            RPCEndpointResolver.ReflectEndpoints();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{portNumber}/");
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
