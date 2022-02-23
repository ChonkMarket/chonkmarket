namespace ChonkyWeb.Services
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class SSEClientManager
    {
        private object _locker = new object();
        private readonly Dictionary<string, List<SSEClient>> SSEClients = new();
        private readonly Task pingLoop;

        public SSEClientManager()
        {
            pingLoop = Task.Run(async () =>
            {
                while (true)
                {
                    await PingClients();
                    await Task.Delay(10000);
                }
            });
        }

        public SSEClient RegisterClient(string symbol, HttpContext context)
        {
            symbol = symbol.ToUpper();

            List<SSEClient> clientList;
            lock (_locker)
            {
                if (!SSEClients.TryGetValue(symbol, out clientList))
                {
                    clientList = new List<SSEClient>();
                    SSEClients[symbol] = clientList;
                }
            }
            var client = new SSEClient(context, (client, e) => { clientList.Remove((SSEClient)client); });
            clientList.Add(client);
            return client;
        }

        public Task PingClients()
        {
            var tasks = new List<Task>();
            foreach (var key in SSEClients.Keys)
            {
                if (SSEClients.TryGetValue(key, out var clientList))
                {
                    for (var i = 0; i < clientList.Count; i++)
                    {
                        var client = clientList[i];
                        if (client != null)
                        {
                            tasks.Add(client.Ping());
                        }
                    }
                }
            }
            return Task.WhenAll(tasks);
        }

        public Task PushData(string symbol, string data)
        {
            if (SSEClients.TryGetValue(symbol, out var clientList))
            {
                var tasks = new List<Task>();
                for (var i = 0; i < clientList.Count; i++)
                {
                    var client = clientList[i];
                    if (client != null)
                    {
                        tasks.Add(client.SendDataAsync(data));
                    }
                }
                return Task.WhenAll(tasks);
            }
            return Task.CompletedTask;
        }
    }

    public class SSEClient : IDisposable
    {
        private HttpContext _context;
        private readonly Task initialized;
        private TaskCompletionSource _tcs = new TaskCompletionSource();
        public event EventHandler ConnectionClosed;

        public Task Task;

        public SSEClient(HttpContext context, EventHandler onClose = null)
        {
            _context = context;
            ConnectionClosed = onClose;

            initialized = Task.Run(async () =>
            {
                await SSEInitAsync();
            });
            Task = _tcs.Task;
        }

        private async Task SSEInitAsync()
        {
            _context.Response.Headers.Add("Cache-Control", "no-cache");
            _context.Response.Headers.Add("Content-Type", "text/event-stream");
            await _context.Response.Body.FlushAsync();
        }
        
        public async Task SendEventAsync(string _event, string data)
        {
            if (_context.RequestAborted.IsCancellationRequested)
            {
                this.Dispose();
            }
            else
            {
                await initialized;
                await _context.Response.WriteAsync($"event: {_event}\ndata: {data}\n\n");
            }
        }

        public async Task Ping()
        {
            if (_context.RequestAborted.IsCancellationRequested)
            {
                this.Dispose();
            }
            else
            {
                await initialized;
                await _context.Response.WriteAsync("event: ping\ndata: ping\n\n");
            }
        }

        public async Task SendDataAsync(string _event)
        {
            if (_context.RequestAborted.IsCancellationRequested)
            {
                this.Dispose();
            }
            else
            {
                await initialized;
                await _context.Response.WriteAsync($"data: {_event}\n\n");
            }
        }

        public void Dispose()
        {
            _tcs.SetResult();
            ConnectionClosed?.Invoke(this, new EventArgs());
        }
    }
}
