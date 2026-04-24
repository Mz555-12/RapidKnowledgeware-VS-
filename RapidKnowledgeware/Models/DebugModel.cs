using System;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 调试窗口数据模型（单例）
    /// </summary>
    public class DebugModel
    {
        private static DebugModel _instance;
        public static DebugModel Instance => _instance ?? (_instance = new DebugModel());

        /// <summary>
        /// 日志条目集合
        /// </summary>
        public ObservableCollection<DebugLogEntry> LogEntries { get; } = new ObservableCollection<DebugLogEntry>();

        private DebugModel() { }

        
    }


    /// <summary>
    /// 调试窗口单条日志条目
    /// </summary>
    public class DebugLogEntry
    {
        /// <summary>
        /// 记录时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别（Info / Error）
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 日志内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 异常详情（仅 Error 时可能有）
        /// </summary>
        public string ExceptionDetail { get; set; }
    }
}