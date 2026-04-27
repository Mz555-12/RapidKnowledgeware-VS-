namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// AI 回复消息的段落块
    /// </summary>
    public class MessageBlock
    {
        /// <summary>
        /// 块类型：Text 或 Code
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 文本内容（代码块时包含语言和代码）
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 代码块的语言标识（如 "rapid"）
        /// </summary>
        public string Language { get; set; }
    }
}