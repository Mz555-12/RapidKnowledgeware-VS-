using System;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 聊天日志条目
    /// </summary>
    public class ChatLog
    {
        /// <summary>
        /// 时间戳（精确到秒）
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 会话唯一标识符
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// 会话显示名称
        /// </summary>
        public string SessionName { get; set; }

        /// <summary>
        /// 用户消息内容
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// AI回复内容（包含思考过程和正式回答）
        /// </summary>
        public string AIResponse { get; set; }

        /// <summary>
        /// 使用的对话模型名称
        /// </summary>
        public string ModelUsed { get; set; }

    }
}