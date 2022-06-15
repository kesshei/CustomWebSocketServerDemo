using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketLib
{
    /// <summary>
    /// 消息
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 消息的头部信息
        /// </summary>
        public MessageHeader head { get; set; }
        /// <summary>
        /// 消息实体
        /// </summary>
        public string Data { get; set; }
    }
}
