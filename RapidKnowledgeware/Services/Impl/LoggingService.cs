using IOC;
using IOC.Annotations;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// 日志服务：负责日志的写入、读取、清理和导出（业务逻辑层）
    /// 文件 I/O 委托给 ILogRepository（DAO 层）
    /// </summary>
    [Service]
    public class LoggingService : ILoggingService
    {
        private readonly ILogRepository _logRepository;
        private readonly object ChatLock = new object();
        private readonly object OpsLock = new object();

        public LoggingService()
        {
            _logRepository = BeanFactory.GetBean<ILogRepository>();
            _logRepository.EnsureDirectoriesExist();
        }

        #region 写入

        /// <summary>
        /// 异步写入聊天日志（追加到当前月份文件）
        /// </summary>
        public async Task WriteChatLogAsync(ChatLog log)
        {
            if (log == null) return;
            await Task.Run(() =>
            {
                lock (ChatLock)
                {
                    _logRepository.AppendLog(_logRepository.ChatLogDir, log.Timestamp, log);
                }
            });
        }

        /// <summary>
        /// 异步写入操作日志（追加到当前月份文件）
        /// </summary>
        public async Task WriteOperationLogAsync(OperationsLog log)
        {
            if (log == null) return;
            await Task.Run(() =>
            {
                lock (OpsLock)
                {
                    _logRepository.AppendLog(_logRepository.OpsLogDir, log.Timestamp, log);
                }
            });
        }

        /// <summary>
        /// 同步写入操作日志（用于必须在当前上下文保存的场景）
        /// </summary>
        public void WriteOperationLog(OperationsLog log)
        {
            if (log == null) return;
            lock (OpsLock)
            {
                _logRepository.AppendLog(_logRepository.OpsLogDir, log.Timestamp, log);
            }
        }

        #endregion

        #region 读取

        /// <summary>
        /// 获取所有可用的月份列表（基于文件名）
        /// </summary>
        public List<string> GetAvailableMonths(string logType)
        {
            string baseDir = logType == "Chat" ? _logRepository.ChatLogDir : _logRepository.OpsLogDir;
            return _logRepository.GetAvailableMonths(baseDir);
        }

        /// <summary>
        /// 加载指定月份的聊天日志
        /// </summary>
        public List<ChatLog> LoadChatLogs(string month)
        {
            return _logRepository.LoadLogs<ChatLog>(_logRepository.ChatLogDir, month);
        }

        /// <summary>
        /// 加载指定月份的操作日志
        /// </summary>
        public List<OperationsLog> LoadOperationLogs(string month)
        {
            return _logRepository.LoadLogs<OperationsLog>(_logRepository.OpsLogDir, month);
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理过期的日志文件（保留最近12个月）
        /// </summary>
        public void CleanOldLogs()
        {
            Task.Run(() =>
            {
                _logRepository.CleanOldFiles(_logRepository.ChatLogDir, 12);
                _logRepository.CleanOldFiles(_logRepository.OpsLogDir, 12);
            });
        }

        #endregion

        #region 导出 TXT

        /// <summary>
        /// 导出指定月份的聊天日志为 TXT
        /// </summary>
        public string ExportChatLogsToTxt(string month)
        {
            var logs = LoadChatLogs(month);
            if (logs.Count == 0) return string.Empty;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== 聊天日志 - {month} ===\n");
            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] 会话: {log.SessionName} ({log.SessionId})");
                sb.AppendLine($"用户: {log.UserMessage}");
                sb.AppendLine($"AI ({log.ModelUsed}): {log.AIResponse}");
                sb.AppendLine(new string('-', 50));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 导出指定月份的操作日志为 TXT
        /// </summary>
        public string ExportOperationLogsToTxt(string month)
        {
            var logs = LoadOperationLogs(month);
            if (logs.Count == 0) return string.Empty;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== 操作日志 - {month} ===\n");
            foreach (var log in logs)
            {
                string status = log.Success ? "成功" : "失败";
                string opName = GetOperationDisplayName(log);
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {opName} - {status}");
                sb.AppendLine($"目标: {log.Target}");
                if (!string.IsNullOrEmpty(log.Details))
                    sb.AppendLine($"详情: {log.Details}");
                sb.AppendLine(new string('-', 50));
            }
            return sb.ToString();
        }

        private string GetOperationTypeName(OperationType type)
        {
            return type switch
            {
                OperationType.CreateSession => "新建会话",
                OperationType.DeleteSession => "删除会话",
                OperationType.RenameSession => "重命名会话",
                OperationType.EditSessionParams => "编辑会话参数",
                OperationType.AddKnowledgeFile => "添加知识文件",
                OperationType.DeleteKnowledgeFile => "删除知识文件",
                OperationType.ReindexFile => "重新索引文件",
                OperationType.ToggleKnowledgeLink => "切换知识库对接",
                OperationType.ExportLog => "导出日志",
                _ => type.ToString()
            };
        }

        private string GetOperationDisplayName(OperationsLog log)
        {
            if (!string.IsNullOrEmpty(log.ActionName))
                return log.ActionName;
            return GetOperationTypeName(log.Type);
        }

        #endregion
    }
}
