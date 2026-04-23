using System;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 操作日志条目（仅记录关键操作）
    /// </summary>
    public class OperationsLog
    {
        /// <summary>
        /// 时间戳（精确到秒）
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 操作类型（枚举）
        /// </summary>
        public OperationType Type { get; set; }

        /// <summary>
        /// 操作名称（中文描述，便于人工阅读）
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// 操作目标对象（如会话名、文件路径）
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// 操作结果（成功/失败）
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 附加信息（失败原因、旧名称等）
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 操作类型枚举
    /// </summary>
    public enum OperationType
    {
        CreateSession,      // 新建会话
        DeleteSession,      // 删除会话
        RenameSession,      // 重命名会话
        EditSessionParams,  // 编辑会话参数
        AddKnowledgeFile,   // 添加知识文件
        DeleteKnowledgeFile,// 删除知识文件
        ReindexFile,        // 重新索引文件
        ToggleKnowledgeLink,// 切换知识库对接
        ExportLog,                      // 导出日志
        EditLLMParams,                  // 编辑全局LLM参数
        ToggleDeepThinking,     // 切换深度思考模式
        EditKnowledgeBaseParams,        // 编辑知识库参数（新增）
    }
}