using Newtonsoft.Json;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Functions.LoggingFunc
{
    /// <summary>
    /// 日志服务：负责日志的异步写入、读取、清理和导出
    /// </summary>
    public static class LoggingService
    {
        private static readonly string LogRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json", "Log");
        private static readonly string ChatLogDir = Path.Combine(LogRoot, "Chat");
        private static readonly string OpsLogDir = Path.Combine(LogRoot, "Operations");
        private static readonly object ChatLock = new object();
        private static readonly object OpsLock = new object();

        /// <summary>
        /// 静态构造函数，确保日志目录存在
        /// </summary>
        static LoggingService()
        {
            if (!Directory.Exists(ChatLogDir))
                Directory.CreateDirectory(ChatLogDir);
            if (!Directory.Exists(OpsLogDir))
                Directory.CreateDirectory(OpsLogDir);
        }

        #region 写入

        /// <summary>
        /// 异步写入聊天日志（追加到当前月份文件）
        /// </summary>
        public static async Task WriteChatLogAsync(ChatLog log)
        {
            if (log == null) return;
            await Task.Run(() =>
            {
                lock (ChatLock)
                {
                    string filePath = GetMonthlyFilePath(ChatLogDir, log.Timestamp);
                    AppendLogToFile(filePath, log);
                }
            });
        }

        /// <summary>
        /// 异步写入操作日志（追加到当前月份文件）
        /// </summary>
        public static async Task WriteOperationLogAsync(OperationsLog log)
        {
            if (log == null) return;
            await Task.Run(() =>
            {
                lock (OpsLock)
                {
                    string filePath = GetMonthlyFilePath(OpsLogDir, log.Timestamp);
                    AppendLogToFile(filePath, log);
                }
            });
        }

        /// <summary>
        /// 同步写入操作日志（用于必须在当前上下文保存的场景）
        /// </summary>
        public static void WriteOperationLog(OperationsLog log)
        {
            if (log == null) return;
            lock (OpsLock)
            {
                string filePath = GetMonthlyFilePath(OpsLogDir, log.Timestamp);
                AppendLogToFile(filePath, log);
            }
        }

        private static string GetMonthlyFilePath(string baseDir, DateTime timestamp)
        {
            string month = timestamp.ToString("yyyy-MM");
            return Path.Combine(baseDir, $"{month}.json");
        }

        private static void AppendLogToFile<T>(string filePath, T entry)
        {
            try
            {
                List<T> logs;
                if (File.Exists(filePath))
                {
                    string existingJson = File.ReadAllText(filePath);
                    logs = JsonConvert.DeserializeObject<List<T>>(existingJson) ?? new List<T>();
                }
                else
                {
                    logs = new List<T>();
                }
                logs.Add(entry);
                string newJson = JsonConvert.SerializeObject(logs, Formatting.Indented);
                File.WriteAllText(filePath, newJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoggingService] 写入日志失败: {ex.Message}");
            }
        }

        #endregion

        #region 读取

        /// <summary>
        /// 获取所有可用的月份列表（基于文件名）
        /// </summary>
        /// <param name="logType">"Chat" 或 "Operations"</param>
        public static List<string> GetAvailableMonths(string logType)
        {
            string dir = logType == "Chat" ? ChatLogDir : OpsLogDir;
            if (!Directory.Exists(dir)) return new List<string>();

            return Directory.GetFiles(dir, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderByDescending(m => m)
                .ToList();
        }

        /// <summary>
        /// 加载指定月份的聊天日志
        /// </summary>
        public static List<ChatLog> LoadChatLogs(string month)
        {
            string filePath = Path.Combine(ChatLogDir, $"{month}.json");
            if (!File.Exists(filePath)) return new List<ChatLog>();
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<ChatLog>>(json) ?? new List<ChatLog>();
            }
            catch
            {
                return new List<ChatLog>();
            }
        }

        /// <summary>
        /// 加载指定月份的操作日志
        /// </summary>
        public static List<OperationsLog> LoadOperationLogs(string month)
        {
            string filePath = Path.Combine(OpsLogDir, $"{month}.json");
            if (!File.Exists(filePath)) return new List<OperationsLog>();
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<OperationsLog>>(json) ?? new List<OperationsLog>();
            }
            catch
            {
                return new List<OperationsLog>();
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理过期的日志文件（保留最近12个月）
        /// </summary>
        public static void CleanOldLogs()
        {
            Task.Run(() =>
            {
                CleanDirectory(ChatLogDir);
                CleanDirectory(OpsLogDir);
            });
        }

        private static void CleanDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return;
            var files = Directory.GetFiles(dir, "*.json")
                .Select(f => new { Path = f, Month = Path.GetFileNameWithoutExtension(f) })
                .Where(x => DateTime.TryParseExact(x.Month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                .OrderByDescending(x => x.Month)
                .ToList();

            // 保留最近12个文件
            var toDelete = files.Skip(12).ToList();
            foreach (var file in toDelete)
            {
                try { File.Delete(file.Path); } catch { }
            }
        }

        #endregion

        #region 导出 TXT

        /// <summary>
        /// 导出指定月份的聊天日志为 TXT
        /// </summary>
        public static string ExportChatLogsToTxt(string month)
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
        public static string ExportOperationLogsToTxt(string month)
        {
            var logs = LoadOperationLogs(month);
            if (logs.Count == 0) return string.Empty;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== 操作日志 - {month} ===\n");
            foreach (var log in logs)
            {
                string status = log.Success ? "成功" : "失败";
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {GetOperationTypeName(log.Type)} - {status}");
                sb.AppendLine($"目标: {log.Target}");
                if (!string.IsNullOrEmpty(log.Details))
                    sb.AppendLine($"详情: {log.Details}");
                sb.AppendLine(new string('-', 50));
            }
            return sb.ToString();
        }

        private static string GetOperationTypeName(OperationType type)
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

        #endregion
    }
}