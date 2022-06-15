using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketLib
{
    /// <summary>
    /// WebSocketServer
    /// </summary>
    public class WebSocketServer
    {
        /// <summary>
        /// 核心监听方法
        /// </summary>
        TcpListener listener;
        /// <summary>
        /// 服务端监听的端口  作为服务端口
        /// </summary>
        public int ListenPort;
        /// <summary>
        /// 监听的端口
        /// </summary>
        /// <param name="port"></param>
        public WebSocketServer(int port)
        {
            this.ListenPort = port;
        }
        /// <summary>
        /// websocket 事件
        /// </summary>
        /// <param name="UserToken"></param>
        public delegate void WebSocketHandler(Socket socket, string data);
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event WebSocketHandler OnOpen;
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event WebSocketHandler OnClose;
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event WebSocketHandler OnMessage;
        /// <summary>
        /// 开始监听
        /// </summary>
        /// <returns></returns>
        public WebSocketServer Listen()
        {
            listener = new TcpListener(IPAddress.Any, this.ListenPort);
            listener.Start();
            ServerStart();
            Task.Run(() =>
            {
                while (true)
                {
                    TcpClient s = listener.AcceptTcpClient();
                    //来一个新的链接
                    ThreadPool.QueueUserWorkItem(r => { Accept(s); });
                }
            });
            return this;
        }
        /// <summary>
        /// 一个新的连接
        /// </summary>
        /// <param name="s"></param>
        public void Accept(TcpClient s)
        {
            BinaryReader rs = new BinaryReader(s.GetStream());
            var ReceiveBuffer = new byte[1024];
            List<byte> ReceiveList = new List<byte>();
            UserToken userToken = new UserToken();
            userToken.ConnectSocket = s.Client;
            userToken.ConnectTime = DateTime.Now;
            userToken.RemoteAddress = s.Client.RemoteEndPoint;
            userToken.IPAddress = ((IPEndPoint)(userToken.RemoteAddress)).Address;

            try
            {
                newAcceptHandler(userToken);
                while (s.Connected)
                {
                    int length = 0;
                    try
                    {
                        length = rs.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);
                        //如果没有读完，就一直读
                        for (int i = 0; i < length; i++)
                        {
                            ReceiveList.Add(ReceiveBuffer[i]);
                        }
                        if (s.Client.Available == 0)
                        {
                            var data = ReceiveList.ToArray();
                            //接收完毕
                            Task.Run(() => ReceiveHandler(userToken, data));
                            ReceiveList.Clear();
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    if (length == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                s.Close();//客户端连接关闭
                newQuitHandler(userToken);
            }
        }
        /// <summary>
        /// 接收信息的处理
        /// </summary>
        /// <param name="UserToken"></param>
        public void ReceiveHandler(UserToken userToken, byte[] Receivedata)
        {
            //说明第一次链接，先进行握手
            if (!userToken.temp.ContainsKey("WebSocket"))
            {
                string info = Encoding.UTF8.GetString(Receivedata);
                if (info.IndexOf("websocket") > -1)
                {
                    var send = userToken.ConnectSocket.Send(WebSocketProtocol.HandshakeMessage(info));
                    if (send > 0)
                    {
                        userToken.temp.Add("WebSocket", true);
                    }
                }
            }
            else
            {
                var data = WebSocketProtocol.Decode(Receivedata);
                if (data != null)
                {
                    if (data.head.Opcode == OperType.Close)
                    {
                        userToken.ConnectSocket.Close();
                    }
                    else
                    {
                        if (OnMessage != null)
                        {
                            OnMessage(userToken.ConnectSocket, data.Data);
                        }
                        else
                        {
                            //接管数据处理
                            Console.WriteLine("收到数据");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 新的链接
        /// </summary>
        public void newAcceptHandler(UserToken userToken)
        {
            if (OnOpen != null)
            {
                OnOpen(userToken.ConnectSocket, null);
            }
            Console.WriteLine("一个新的用户:" + userToken.RemoteAddress.ToString());
        }
        /// <summary>
        /// 服务开始
        /// </summary>
        public void ServerStart()
        {
            Console.WriteLine("服务开启:local:" + this.ListenPort);
        }
        /// <summary>
        /// 用户退出
        /// </summary>
        public void newQuitHandler(UserToken userToken)
        {
            if (OnClose != null)
            {
                OnClose(userToken.ConnectSocket, null);
            }
            Console.WriteLine("用户退出:" + userToken.RemoteAddress.ToString());
        }
        /// <summary>
        /// 对客户发送数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public int SendMessage(Socket socket, string data)
        {
            int length = -1;
            try
            {
                var bytes = WebSocketProtocol.PackageServerData(Encoding.UTF8.GetBytes(data), false);
                if (socket != null && socket.Connected)
                {
                    length = socket.Send(bytes);
                }
            }
            catch (Exception ex)
            { }
            return length;
        }
    }
}
