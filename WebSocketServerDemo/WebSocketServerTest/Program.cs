using WebSocketLib;
using System;

namespace WebSocketServerTest
{
    class Program
    {
        static WebSocketServer smartWebSocketServer;
        static void Main(string[] args)
        {
            Console.Title = "WebSocket Server Demo 蓝总创精英团队!";
            smartWebSocketServer = new WebSocketServer(5000);
            smartWebSocketServer.OnMessage += WebSocketServer_OnMessage;
            smartWebSocketServer.Listen();
            Console.WriteLine("开始监听!");
            Console.ReadLine(); 
        }

        private static void WebSocketServer_OnMessage(System.Net.Sockets.Socket socket, string data)
        {
            Console.WriteLine("收到客户端的数据:" + data);
            smartWebSocketServer.SendMessage(socket, data);
        }
    }
}
