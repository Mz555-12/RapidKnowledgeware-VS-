using IOC;
using IOC.Annotations;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// 日志展示业务逻辑服务：处理日志数据的查询与导出
    /// </summary>
    [Service]
    public class LoggingDisplayService : ILoggingDisplayService
    {
        /// <summary>
        /// 初始化日志展示服务
        /// </summary>
        public LoggingDisplayService()
        {
        }

        /// <summary>
        /// 获取指定日志类型的可用日期列表
        /// </summary>
        public List<string> GetAvailableDates(string logType)
        {
            var dates = new HashSet<string>();
            var months = BeanFactory.GetBean<ILoggingService>().GetAvailableMonths(logType);
            foreach (var month in months)
            {
                if (logType == "Chat")
                {
                    var logs = BeanFactory.GetBean<ILoggingService>().LoadChatLogs(month);
                    foreach (var log in logs)
                        dates.Add(log.Timestamp.ToString("yyyy-MM-dd"));
                }
                else
                {
                    var logs = BeanFactory.GetBean<ILoggingService>().LoadOperationLogs(month);
                    foreach (var log in logs)
                        dates.Add(log.Timestamp.ToString("yyyy-MM-dd"));
                }
            }
            return dates.OrderByDescending(d => d).ToList();
        }

        /// <summary>
        /// 加载指定条件的日志条目列表
        /// </summary>
        public List<LogEntryViewModel> LoadLogs(string logType, string selectedDate, string searchKeyword)
        {
            var result = new List<LogEntryViewModel>();
            if (string.IsNullOrEmpty(selectedDate)) return result;

            if (!DateTime.TryParseExact(selectedDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime targetDate))
                return result;
            string month = targetDate.ToString("yyyy-MM");
            string keyword = searchKeyword?.Trim() ?? "";

            if (logType == "Chat")
            {
                var logs = BeanFactory.GetBean<ILoggingService>().LoadChatLogs(month);
                var filtered = logs
                    .Where(l => l.Timestamp.Date == targetDate.Date)
                    .Where(l => string.IsNullOrEmpty(keyword) ||
                                (l.UserMessage?.Contains(keyword) == true) ||
                                (l.AIResponse?.Contains(keyword) == true) ||
                                (l.SessionName?.Contains(keyword) == true))
                    .OrderByDescending(l => l.Timestamp)
                    .ToList();

                foreach (var log in filtered)
                {
                    result.Add(new LogEntryViewModel
                    {
                        Timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        SessionInfo = $"[{log.SessionName}]",
                        Content = $"用户: {log.UserMessage}\nAI ({log.ModelUsed}): {log.AIResponse}",
                        RawData = log
                    });
                }
            }
            else
            {
                var logs = BeanFactory.GetBean<ILoggingService>().LoadOperationLogs(month);
                var filtered = logs
                    .Where(l => l.Timestamp.Date == targetDate.Date)
                    .Where(l => string.IsNullOrEmpty(keyword) ||
                                (l.Target?.Contains(keyword) == true) ||
                                (l.Details?.Contains(keyword) == true))
                    .OrderByDescending(l => l.Timestamp)
                    .ToList();

                foreach (var log in filtered)
                {
                    string status = log.Success ? "✓" : "✗";
                    result.Add(new LogEntryViewModel
                    {
                        Timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        SessionInfo = $"[{GetOpTypeShort(log.Type)}]",
                        Content = $"{status} {log.Target}" + (string.IsNullOrEmpty(log.Details) ? "" : $"\n详情: {log.Details}"),
                        RawData = log
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 导出指定日志类型的日志内容字符串
        /// </summary>
        public string ExportLogs(string logType, string selectedDate)
        {
            if (string.IsNullOrEmpty(selectedDate)) return null;

            if (!DateTime.TryParseExact(selectedDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                return null;
            string month = date.ToString("yyyy-MM");

            if (logType == "Chat")
                return BeanFactory.GetBean<ILoggingService>().ExportChatLogsToTxt(month);
            else
                return BeanFactory.GetBean<ILoggingService>().ExportOperationLogsToTxt(month);
        }

        private string GetOpTypeShort(OperationType type)
        {
            return type switch
            {
                OperationType.CreateSession => "新建",
                OperationType.DeleteSession => "删除",
                OperationType.RenameSession => "重命名",
                OperationType.EditSessionParams => "编辑参数",
                OperationType.AddKnowledgeFile => "添加文件",
                OperationType.DeleteKnowledgeFile => "删除文件",
                OperationType.ReindexFile => "重新索引",
                OperationType.ToggleKnowledgeLink => "切换知识库",
                OperationType.ExportLog => "导出日志",
                _ => type.ToString()
            };
        }
    }

    /// <summary>
    /// 日志条目视图模型（用于 UI 列表绑定）
    /// </summary>
    public class LogEntryViewModel
    {
        public string Timestamp { get; set; }
        public string SessionInfo { get; set; }
        public string Content { get; set; }
        public object RawData { get; set; }
    }
}
