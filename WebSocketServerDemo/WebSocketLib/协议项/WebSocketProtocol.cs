using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketLib
{
    /// <summary>
    /// webSocket协议
    /// </summary>
    public class WebSocketProtocol
    {
        /// <summary>
        /// WebSocket 握手 key 魔数
        /// </summary>
        private const string MagicKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        /// <summary>
        /// 是否启动了掩码
        /// </summary>
        private const char charOne = '1';
        private const char charZero = '0';
        #region 协议之 握手
        /// <summary>
        /// 协议处理-http协议握手
        /// </summary>
        public static byte[] HandshakeMessage(string data)
        {
            string key = string.Empty;
            string info = data;
            //一步一步来
            string[] list = info.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in list.Reverse())
            {
                if (item.IndexOf("Sec-WebSocket-Key") > -1)
                {
                    key = item.Split(new string[] { ": " }, StringSplitOptions.None)[1];
                    break;
                }
            }
            //获取标准的key
            key = getResponseKey(key);
            //拼装返回的协议内容
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
            responseBuilder.Append("Upgrade: websocket" + "\r\n");
            responseBuilder.Append("Connection: Upgrade" + "\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + key + "\r\n\r\n");
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
        /// <summary>
        /// 获取返回验证的key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string getResponseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            else
            {
                key += MagicKey;
                key = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key.Trim())));
                return key;
            }
        }
        #endregion
        #region 协议包解析
        #region 解码
        /// <summary>
        /// 判断数据是否足够
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool DataEnough(long AllLength, MessageHeader header, byte[] data, ref long ReadLength)
        {
            if (data == null || data.Length < 3) { return false; }
            //判断数据是否足够
            Byte[] buffer = data;
            if (AllLength > (long)header.Payloadlen)
            {
                ReadLength = header.PayloadDataStartIndex + (long)header.Payloadlen;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 解码数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Message Decode(Byte[] data, MessageHeader header)
        {
            Message msg = new Message();
            msg.head = header;
            if (data == null || data.Length == 0)
            {
                msg.Data = string.Empty;
                return msg;
            }

            Byte[] payload = null;
            if (header != null)
            {
                //payload = new Byte[data.Length - header.PayloadDataStartIndex];
                if (header.Payloadlen > 0)
                {
                    payload = new Byte[(int)header.Payloadlen];
                }
                else
                {
                    payload = new Byte[data.Length - header.PayloadDataStartIndex];
                }
                Buffer.BlockCopy(data, header.PayloadDataStartIndex, payload, 0, payload.Length);
                if (header.MASK == charOne)
                {
                    for (int i = 0; i < payload.Length; i++)
                    {
                        payload[i] = (Byte)(payload[i] ^ header.Maskey[i % 4]);
                    }
                }
            }
            else
            {
                msg.Data = Encoding.UTF8.GetString(data);
                return msg;
            }
            if (header.Opcode == OperType.Text)
            {
                msg.Data = Encoding.UTF8.GetString(payload);
            }
            return msg;
        }
        /// <summary>
        /// 解码数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Message Decode(Byte[] data)
        {
            Byte[] buffer = new Byte[14];
            if (data.Length >= 14)
            {
                Buffer.BlockCopy(data, 0, buffer, 0, 14);
            }
            else
            {
                Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            }
            MessageHeader header = analyseHead(buffer);
            Message msg = new Message();
            msg.head = header;
            if (data == null || data.Length == 0)
            {
                msg.Data = string.Empty;
                return msg;
            }

            Byte[] payload = null;
            if (header != null)
            {
                //payload = new Byte[data.Length - header.PayloadDataStartIndex];
                if (header.Payloadlen > 0)
                {
                    payload = new Byte[(int)header.Payloadlen];
                }
                else
                {
                    payload = new Byte[data.Length - header.PayloadDataStartIndex];
                }
                Buffer.BlockCopy(data, header.PayloadDataStartIndex, payload, 0, payload.Length);
                if (header.MASK == charOne)
                {
                    for (int i = 0; i < payload.Length; i++)
                    {
                        payload[i] = (Byte)(payload[i] ^ header.Maskey[i % 4]);
                    }
                }
            }
            else
            {
                msg.Data = Encoding.UTF8.GetString(data);
                return msg;
            }
            if (header.Opcode == OperType.Text)
            {
                msg.Data = Encoding.UTF8.GetString(payload);
            }
            return msg;
        }
        /// <summary>
        /// 判断此数据是否是合格的头部信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsHead(byte[] data, ref MessageHeader header)
        {
            if (data != null && data.Length > 2)
            {
                var head = analyseHead(data);
                header = head;
                if (head != null)
                {
                    if (head.RSV1 == '0' && head.RSV2 == '0' && head.RSV3 == '0')
                    {
                        if (head.Opcode == OperType.Binary || head.Opcode == OperType.Close || head.Opcode == OperType.Ping || head.Opcode == OperType.Pong || head.Opcode == OperType.Row || head.Opcode == OperType.Text)
                        {
                            if (head.MASK == '0')
                            {

                                int lenSize = (int)head.Payloadlen;
                                if (lenSize > 125)
                                {
                                    if (lenSize > 0xFFFF)
                                    {
                                        lenSize = 127;
                                    }
                                    else
                                    {
                                        lenSize = 126;
                                    }
                                }
                                if (lenSize == head.Len)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 解析数据的头部信息
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static MessageHeader analyseHead(Byte[] buffer)
        {
            MessageHeader header = new MessageHeader();
            header.FIN = (buffer[0] & 0x80) == 0x80 ? charOne : charZero;
            header.RSV1 = (buffer[0] & 0x40) == 0x40 ? charOne : charZero;
            header.RSV2 = (buffer[0] & 0x20) == 0x20 ? charOne : charZero;
            header.RSV3 = (buffer[0] & 0x10) == 0x10 ? charOne : charZero;
            // 判断是否为结束针
            header.IsEof = (buffer[0] >> 7) > 0;
            if ((buffer[0] & 0xA) == 0xA)
                header.Opcode = OperType.Pong;
            else if ((buffer[0] & 0x9) == 0x9)
                header.Opcode = OperType.Ping;
            else if ((buffer[0] & 0x8) == 0x8)
                header.Opcode = OperType.Close;
            else if ((buffer[0] & 0x2) == 0x2)
                header.Opcode = OperType.Binary;
            else if ((buffer[0] & 0x1) == 0x1)
                header.Opcode = OperType.Text;
            else if ((buffer[0] & 0x0) == 0x0)
                header.Opcode = OperType.Row;

            header.MASK = (buffer[1] & 0x80) == 0x80 ? charOne : charZero;
            Int32 len = buffer[1] & 0x7F;
            header.Len = len;
            if (len == 126)
            {
                header.Payloadlen = (UInt64)(buffer[2] << 8 | buffer[3]);
                if (header.MASK == charOne)
                {
                    header.Maskey = new Byte[4];
                    Buffer.BlockCopy(buffer, 4, header.Maskey, 0, 4);
                    header.PayloadDataStartIndex = 8;
                }
                else
                    header.PayloadDataStartIndex = 4;
            }
            else if (len == 127)
            {
                Byte[] byteLen = new Byte[8];
                Buffer.BlockCopy(buffer, 2, byteLen, 0, 8);
                Array.Reverse(byteLen);
                header.Payloadlen = BitConverter.ToUInt64(byteLen, 0);
                if (header.MASK == charOne)
                {
                    header.Maskey = new Byte[4];
                    Buffer.BlockCopy(buffer, 10, header.Maskey, 0, 4);
                    header.PayloadDataStartIndex = 14;
                }
                else
                    header.PayloadDataStartIndex = 10;
            }
            else
            {
                header.Payloadlen = (ulong)len;
                if (header.MASK == charOne)
                {
                    header.Maskey = new Byte[4];
                    Buffer.BlockCopy(buffer, 2, header.Maskey, 0, 4);
                    header.PayloadDataStartIndex = 6;
                }
                else
                    header.PayloadDataStartIndex = 2;
            }
            return header;
        }
        #endregion
        #region 编码
        /// <summary>
        /// 把客户端消息打包处理
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="message">Message.</param>
        public static byte[] PackageServerData(byte[] message, bool IsMask = true)
        {
            byte[] bytData = message;
            byte[] MaskingKey = null;
            if (IsMask)
            {
                MaskingKey = createMaskingKey();
            }
            UInt64 length = (UInt64)bytData.Length;
            byte len = (byte)length, lenSize = 0;
            if (length > 125)
            {
                if (length > 0xFFFF)
                {
                    len = 127;
                    lenSize = 8;
                }
                else
                {
                    len = 126;
                    lenSize = 2;
                }
            }

            bool Mask = (MaskingKey != null);
            UInt64 offset = (UInt64)(2 + lenSize);

            byte[] bytBuffer = new byte[2 + (UInt64)(Mask ? 4 : 0) + length + (UInt64)lenSize];
            bytBuffer[0] = (byte)(0x80 | (byte)1);
            bytBuffer[1] = (byte)((Mask ? 0x80 : 0) | len);

            if (lenSize == 2)
            {
                bytBuffer[2] = (byte)(length >> 8 & 0xFF);
                bytBuffer[3] = (byte)(length & 0xFF);
            }
            else if (lenSize == 4)
            {
                bytBuffer[2] = (byte)(length >> 56 & 0xFF);
                bytBuffer[3] = (byte)(length >> 48 & 0xFF);
                bytBuffer[4] = (byte)(length >> 40 & 0xFF);
                bytBuffer[5] = (byte)(length >> 32 & 0xFF);
                bytBuffer[6] = (byte)(length >> 24 & 0xFF);
                bytBuffer[7] = (byte)(length >> 16 & 0xFF);
                bytBuffer[8] = (byte)(length >> 8 & 0xFF);
                bytBuffer[9] = (byte)(length & 0xFF);
            }

            if (Mask)
            {
                for (UInt64 i = 0; i < 4; i++)
                {
                    bytBuffer[offset + i] = MaskingKey[i];
                }
                offset += 4;
            }

            for (UInt64 i = 0; i < length; i++)
            {
                if (Mask) bytData[i] ^= MaskingKey[i % 4];
                bytBuffer[offset + i] = bytData[i];
            }

            return bytBuffer;
        }
        /// <summary>
        /// 打包消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="IsMask">是否启用虚码，服务器给客户端 不可以用，客户端给服务器，必须用</param>
        /// <returns></returns>
        public static byte[] PackageServerData(string message, bool IsMask = true)
        {
            byte[] bytData = Encoding.UTF8.GetBytes(message);
            byte[] MaskingKey = null;
            if (IsMask)
            {
                MaskingKey = createMaskingKey();
            }
            UInt64 length = (UInt64)bytData.Length;
            byte len = (byte)length, lenSize = 0;
            if (length > 125)
            {
                if (length > 0xFFFF)
                {
                    len = 127;
                    lenSize = 8;
                }
                else
                {
                    len = 126;
                    lenSize = 2;
                }
            }

            bool Mask = (MaskingKey != null);
            UInt64 offset = (UInt64)(2 + lenSize);

            byte[] bytBuffer = new byte[2 + (UInt64)(Mask ? 4 : 0) + length + (UInt64)lenSize];
            bytBuffer[0] = (byte)(0x80 | (byte)1);
            bytBuffer[1] = (byte)((Mask ? 0x80 : 0) | len);

            if (lenSize == 2)
            {
                bytBuffer[2] = (byte)(length >> 8 & 0xFF);
                bytBuffer[3] = (byte)(length & 0xFF);
            }
            else if (lenSize == 4)
            {
                bytBuffer[2] = (byte)(length >> 56 & 0xFF);
                bytBuffer[3] = (byte)(length >> 48 & 0xFF);
                bytBuffer[4] = (byte)(length >> 40 & 0xFF);
                bytBuffer[5] = (byte)(length >> 32 & 0xFF);
                bytBuffer[6] = (byte)(length >> 24 & 0xFF);
                bytBuffer[7] = (byte)(length >> 16 & 0xFF);
                bytBuffer[8] = (byte)(length >> 8 & 0xFF);
                bytBuffer[9] = (byte)(length & 0xFF);
            }

            if (Mask)
            {
                for (UInt64 i = 0; i < 4; i++)
                {
                    bytBuffer[offset + i] = MaskingKey[i];
                }
                offset += 4;
            }

            for (UInt64 i = 0; i < length; i++)
            {
                if (Mask) bytData[i] ^= MaskingKey[i % 4];
                bytBuffer[offset + i] = bytData[i];
            }

            return bytBuffer;
        }
        /// <summary>
        /// 创建一个 Mask 掩码
        /// </summary>
        /// <returns></returns>
        private static byte[] createMaskingKey()
        {
            var key = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(key);
            return key;
        }
        #endregion
        #endregion
    }
}
