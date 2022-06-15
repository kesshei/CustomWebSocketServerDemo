using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "WebSocket Client Demo 蓝总创精英团队!";
            var webSocket = await CreateAsync("ws://localhost:5000");
            if (webSocket != null)
            {
                Console.WriteLine("服务开始执行!");
                _ = Task.Run(async () =>
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(1024);
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription);
                            }
                            var text = Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count));
                            Console.WriteLine("来自服务端:" + text);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                });
                Console.WriteLine("开始输入：");
                Thread.Sleep(1000);
                var text = string.Empty;
                while (text != "exit")
                {
                    text = Console.ReadLine();
                    var sendStr = Encoding.UTF8.GetBytes(text);
                    if (webSocket.State != WebSocketState.Open)
                    {
                        Console.WriteLine("服务端自己关闭了连接!");
                    }
                    await webSocket.SendAsync(sendStr, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else
            {
                Console.WriteLine("服务连接失败!");
            }
            Console.WriteLine("服务执行完毕!");
            Console.ReadLine();
        }
        /// <summary>
        /// 创建客户端实例
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ClientWebSocket> CreateAsync(string ServerUri)
        {
            var webSocket = new ClientWebSocket();
            webSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };

            await webSocket.ConnectAsync(new Uri(ServerUri), CancellationToken.None);
            if (webSocket.State == WebSocketState.Open)
            {
                return webSocket;
            }
            return null;
        }
    }
}
