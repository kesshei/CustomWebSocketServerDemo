using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketLib
{
    /// <summary>
    /// 操作码
    /// </summary>
    public enum OperType
    {
        /// <summary>
        /// 表示连续消息片断
        /// </summary>
        Row = 0x0,
        /// <summary>
        /// 表示文本消息片断
        /// </summary>
        Text = 0x1,
        /// <summary>
        /// 表未二进制消息片断
        /// </summary>
        Binary = 0x2,
        /// <summary>
        /// 表示连接关闭
        /// </summary>
        Close = 0x8,
        /// <summary>
        /// 表示心跳检查的ping
        /// </summary>
        Ping = 0x9,
        /// <summary>
        /// 表示心跳检查的pong 
        /// </summary>
        Pong = 0xA,
        /// <summary>
        /// 未知命令  包含 为将来的非控制消息片断保留的操作码(0x3-7)
        /// </summary>
        Unkown
    }
}
