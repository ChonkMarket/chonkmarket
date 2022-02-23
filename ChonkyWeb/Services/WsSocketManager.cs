namespace ChonkyWeb.Services
{
    using StockDataLibrary.Protos;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using Google.Protobuf;
    using System.Threading.Tasks;
    using System.Threading;
    using System;

    public class WsSocketManager
    {
        private readonly Dictionary<string, List<WsSocket>> sockets = new();
        public CancellationToken CancellationToken { get; set; } = new();
        private readonly object _locker = new();

        public WsSocket RegisterSocket(WebSocket socket, string symbol)
        {
            symbol = symbol.ToUpper();

            List<WsSocket> socketList;
            lock(_locker)
            {
                if (!sockets.TryGetValue(symbol, out socketList))
                {
                    socketList = new List<WsSocket>();
                    sockets[symbol] = socketList;
                }
            }
            var wsSocket = new WsSocket(socket, CancellationToken);
            wsSocket.ConnectionClosed = (socket, e) => { socketList.Remove(wsSocket); };
            socketList.Add(wsSocket);
            return wsSocket;
        }

        public async Task PushTrade(string symbol, Trade trade)
        {
            if (sockets.TryGetValue(symbol, out var list))
            { 
                foreach (var socket in list)
                {
                    await socket.PushData(trade);
                }
            }
        }
    }

    public class WsSocket : IDisposable
    {
        private readonly TaskCompletionSource state = new();
        public Task Task { get => state.Task; }
        private readonly WebSocket _socket;
        private readonly CancellationToken _token;
        public EventHandler ConnectionClosed { get; set; }

        public WsSocket(WebSocket socket, CancellationToken token)
        {
            _socket = socket;
            _token = token;
        }

        public async Task PushData(Trade trade)
        {
            var bytesToPush = trade.ToByteArray();
            if (_socket.State == WebSocketState.Open)
                await _socket.SendAsync(bytesToPush, WebSocketMessageType.Binary, true, _token);
            else
                this.Dispose();
        }

        public void Dispose()
        {
            ConnectionClosed?.Invoke(this, new EventArgs());
            state.SetResult();
            GC.SuppressFinalize(this);
        }
    }

}
