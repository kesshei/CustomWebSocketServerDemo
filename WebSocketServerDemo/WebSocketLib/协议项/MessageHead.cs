using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketLib
{
    /// <summary>
    /// 消息头部信息
    /// </summary>
    public class MessageHeader
    {
        /// <summary>
        /// 1位，用来表明这是一个消息的最后的消息片断，当然第一个消息片断也可能是最后的一个消息片断；
        /// </summary>
        public Char FIN;
        /// <summary>
        /// 如果双方之间没有约定自定义协议，那么这几位的值都必须为0,否则必须断掉WebSocket连接；
        /// </summary>
        public Char RSV1;
        /// <summary>
        /// 如果双方之间没有约定自定义协议，那么这几位的值都必须为0,否则必须断掉WebSocket连接；
        /// </summary>
        public Char RSV2;
        /// <summary>
        /// 如果双方之间没有约定自定义协议，那么这几位的值都必须为0,否则必须断掉WebSocket连接；
        /// </summary>
        public Char RSV3;
        /// <summary>
        /// 操作码
        /// </summary>
        public OperType Opcode;
        /// <summary>
        /// 1位，定义传输的数据是否有加掩码,如果设置为1,掩码键必须放在masking-key区域，客户端发送给服务端的所有消息，此位的值都是1；
        /// </summary>
        public Char MASK;
        /// <summary>
        /// 传输数据的长度，以字节的形式表示
        /// </summary>
        public UInt64 Payloadlen;
        /// <summary>
        /// 0或4个字节，客户端发送给服务端的数据，都是通过内嵌的一个32位值作为掩码的；掩码键只有在掩码位设置为1的时候存在。
        /// </summary>
        public Byte[] Maskey;
        /// <summary>
        /// 数据开始位
        /// </summary>
        public Int32 PayloadDataStartIndex;
        /// <summary>
        /// 是否是结束帧
        /// </summary>
        public bool IsEof;
        /// <summary>
        /// Len字段的长度
        /// </summary>
        public int Len;
    }
}
