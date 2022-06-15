using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WebSocketLib
{
    //GET / HTTP/1.1
    //Host: localhost:9999
    //Connection: Upgrade
    //Pragma: no-cache
    //Cache-Control: no-cache
    //Upgrade: websocket
    //Origin: file://
    //Sec-WebSocket-Version: 13
    //User-Agent: Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36
    //Accept-Encoding: gzip, deflate, br
    //Accept-Language: zh-CN,zh;q=0.9,ja;q=0.8,nl;q=0.7
    //Cookie: _ga=GA1.1.601168430.1540570802
    //Sec-WebSocket-Key: TV0AyXfLhtkIDU2OBa7cmw==
    //Sec-WebSocket-Extensions: permessage-deflate; client_max_window_bits

    /// <summary>
    /// 创建websocket所需要的头部握手协议
    /// </summary>
    public class WebSocketHeader
    {
        /// <summary>
        /// 协议头部信息
        /// </summary>
        private string Head = "GET {urladdress} HTTP/1.1";
        /// <summary>
        /// 头部请求信息
        /// </summary>
        private string HeadStr = string.Empty;
        /// <summary>
        /// 内置换行
        /// </summary>
        private string NewLine = "\r\n";
        /// <summary>
        /// 数值之间的分隔符
        /// </summary>
        private string splitStr = ": ";
        /// <summary>
        /// 魔数
        /// </summary>
        private const string MagicKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        /// <summary>
        /// 所有可选的头部
        /// </summary>
        private Dictionary<string, string> Heads = new Dictionary<string, string>();
        /// <summary>
        /// 内置随机
        /// </summary>
        private static RandomNumberGenerator random = new RNGCryptoServiceProvider();
        /// <summary>
        /// websocketkey信息
        /// </summary>
        private string SecWebSocketKey { get; set; }
        /// <summary>
        /// 默认链接
        /// </summary>
        /// <param name="Host">localhost:9999</param>
        /// <param name=""></param>
        public WebSocketHeader(string url)
        {
            SecWebSocketKey = CreateBase64Key();
            var URL = ToUri(url);
            HeadStr = Head.Replace("{urladdress}", URL.AbsolutePath);
            var port = URL.Port;
            var schm = URL.Scheme;
            string Host = (port == 80 && schm == "ws") ||
                (port == 443 && schm == "wss") ? URL.DnsSafeHost : URL.Authority;
            string Origin = URL.OriginalString;
            if (!string.IsNullOrEmpty(Host)) { Add("Host", Host); }
            if (!string.IsNullOrEmpty(Origin)) { Add("Origin", Origin); }
            Add("Sec-WebSocket-Key", SecWebSocketKey);
            Add("Connection", "Upgrade");
            Add("Upgrade", "websocket");
            Add("Sec-WebSocket-Version", "13");
            Add("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
        }
        /// <summary>
        /// 默认链接
        /// </summary>
        /// <param name="Host">localhost:9999</param>
        /// <param name=""></param>
        public WebSocketHeader(Uri URL)
        {
            HeadStr = Head.Replace("{urladdress}", URL.AbsolutePath);
            SecWebSocketKey = CreateBase64Key();
            var port = URL.Port;
            var schm = URL.Scheme;
            string Host = (port == 80 && schm == "ws") ||
                (port == 443 && schm == "wss") ? URL.DnsSafeHost : URL.Authority;
            string Origin = URL.OriginalString;
            if (!string.IsNullOrEmpty(Host)) { Add("Host", Host); }
            if (!string.IsNullOrEmpty(Origin)) { Add("Origin", Origin); }
            Add("Sec-WebSocket-Key", SecWebSocketKey);
            Add("Connection", "Upgrade");
            Add("Upgrade", "websocket");
            Add("Sec-WebSocket-Version", "13");
            Add("Sec-WebSocket-Protocol", "chat, superchat");
        }
        /// <summary>
        /// 增加数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value)
        {
            if (Heads.ContainsKey(name))
            {
                Heads[name] = value;
            }
            else
            {
                Heads.Add(name, value);
            }
        }
        /// <summary>
        /// 获取需要发送的数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetRequestByte()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(HeadStr + NewLine);
            foreach (var item in Heads)
            {
                sb.Append(item.Key + splitStr + item.Value + NewLine);
            }
            sb.Append(NewLine);
            string html = sb.ToString();
            return Encoding.UTF8.GetBytes(html);
        }
        /// <summary>
        /// 直接生成websocket所需要的key
        /// </summary>
        /// <returns></returns>
        public static string CreateBase64Key()
        {
            var src = new byte[16];
            random.GetBytes(src);
            return Convert.ToBase64String(src);
        }
        /// <summary>
        /// 验证签名是否正确
        /// </summary>
        /// <returns></returns>
        public bool CheckSecWebSocketKey(string key)
        {
            string temp = key;
            if (!string.IsNullOrEmpty(key))
            {
                if (key.IndexOf("Sec-WebSocket-Accept") > -1)
                {
                    string info = key;
                    string[] list = info.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in list.Reverse())
                    {
                        if (item.IndexOf("Sec-WebSocket-Accept") > -1)
                        {
                            key = item.Split(new string[] { splitStr }, StringSplitOptions.None)[1];
                            break;
                        }
                    }

                }
                var Akey = CreateResponseKey(SecWebSocketKey);
                if (key != Akey)
                {
                    return false;
                }
                if (temp.IndexOf("101") < 0)
                {
                    return false;
                }
                return true;

            }
            return false;
        }
        /// <summary>
        /// 与服务器进行验签
        /// </summary>
        /// <param name="base64Key"></param>
        /// <returns></returns>
        private static string CreateResponseKey(string base64Key)
        {
            var buff = new StringBuilder(base64Key, 64);
            buff.Append(MagicKey);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            var src = sha1.ComputeHash(Encoding.ASCII.GetBytes(buff.ToString()));

            return Convert.ToBase64String(src);
        }
        /// <summary>
        /// 获取URL地址
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Uri ToUri(string value)
        {
            Uri ret;
            Uri.TryCreate(value, MaybeUri(value) ? UriKind.Absolute : UriKind.Relative, out ret);
            return ret;
        }
        /// <summary>
        /// 查看是否是一个URL地址
        /// </summary>
        private static bool MaybeUri(string value)
        {
            if (value == null)
                return false;

            if (value.Length == 0)
                return false;

            var idx = value.IndexOf(':');
            if (idx == -1)
                return false;

            if (idx >= 10)
                return false;

            var schm = value.Substring(0, idx);
            return IsPredefinedScheme(schm);
        }
        /// <summary>
        /// 对URL地址进行预处理
        /// </summary>
        private static bool IsPredefinedScheme(string value)
        {
            if (value == null || value.Length < 2)
                return false;

            var c = value[0];
            if (c == 'h')
                return value == "http" || value == "https";

            if (c == 'w')
                return value == "ws" || value == "wss";

            if (c == 'f')
                return value == "file" || value == "ftp";

            if (c == 'g')
                return value == "gopher";

            if (c == 'm')
                return value == "mailto";

            if (c == 'n')
            {
                c = value[1];
                return c == 'e'
                       ? value == "news" || value == "net.pipe" || value == "net.tcp"
                       : value == "nntp";
            }

            return false;
        }
    }
}
