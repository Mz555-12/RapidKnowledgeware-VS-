using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RapidKnowledgeware.Functions.LoggingFunc
{
    /// <summary>
    /// 日志 UI 交互服务：处理日志视图的显示、切换和数据绑定
    /// </summary>
    public class LoggingUIService
    {
        private readonly LoggingViewModel _viewModel;
        private LoggingView _loggingView;

        /// <summary>
        /// 初始化日志 UI 服务
        /// </summary>
        public LoggingUIService(LoggingViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        /// <summary>
        /// 设置日志视图引用
        /// </summary>
        public void SetLoggingView(LoggingView view)
        {
            _loggingView = view;
        }

        /// <summary>
        /// 显示日志覆盖层（使用已有动画）
        /// </summary>
        public void ShowLoggingView(Grid overlayContainer, FrameworkElement view)
        {
            if (overlayContainer == null || view == null) return;

            // 直接显示
            overlayContainer.Visibility = Visibility.Visible;
            // 若之前被动画移出屏幕，需重置偏移
            var transform = view.RenderTransform as TranslateTransform;
            if (transform != null)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                transform.X = 0;
            }

            _viewModel.IsLogViewVisible = true;
            RefreshDateList();
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_viewModel.AvailableDates.Contains(today))
                _viewModel.SelectedDate = today;
            else if (_viewModel.AvailableDates.Any())
                _viewModel.SelectedDate = _viewModel.AvailableDates.First();

            LoadLogs();
        }

        /// <summary>
        /// 隐藏日志覆盖层
        /// </summary>
        public void HideLoggingView(Grid overlayContainer, FrameworkElement view)
        {
            if (overlayContainer == null || view == null) return;
            overlayContainer.Visibility = Visibility.Collapsed;
            _viewModel.IsLogViewVisible = false;
        }

        /// <summary>
        /// 刷新日期列表（根据当前日志类型，扫描所有月份文件内的实际日期）
        /// </summary>
        public void RefreshDateList()
        {
            string logType = _viewModel.LogType;
            var dates = new HashSet<string>();
            var months = LoggingService.GetAvailableMonths(logType);
            foreach (var month in months)
            {
                if (logType == "Chat")
                {
                    var logs = LoggingService.LoadChatLogs(month);
                    foreach (var log in logs)
                        dates.Add(log.Timestamp.ToString("yyyy-MM-dd"));
                }
                else
                {
                    var logs = LoggingService.LoadOperationLogs(month);
                    foreach (var log in logs)
                        dates.Add(log.Timestamp.ToString("yyyy-MM-dd"));
                }
            }
            var sortedDates = dates.OrderByDescending(d => d).ToList();
            _viewModel.AvailableDates.Clear();
            foreach (var d in sortedDates)
                _viewModel.AvailableDates.Add(d);

            if (!_viewModel.AvailableDates.Contains(_viewModel.SelectedDate))
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (_viewModel.AvailableDates.Contains(today))
                    _viewModel.SelectedDate = today;
                else
                    _viewModel.SelectedDate = _viewModel.AvailableDates.FirstOrDefault();
            }
        }

        /// <summary>
        /// 加载当前选中的日志数据（按日期过滤）
        /// </summary>
        public void LoadLogs()
        {
            _viewModel.LogEntries.Clear();
            if (string.IsNullOrEmpty(_viewModel.SelectedDate)) return;

            if (!DateTime.TryParseExact(_viewModel.SelectedDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime targetDate))
                return;
            string month = targetDate.ToString("yyyy-MM");
            string keyword = _viewModel.SearchKeyword?.Trim() ?? "";

            if (_viewModel.LogType == "Chat")
            {
                var logs = LoggingService.LoadChatLogs(month);
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
                    _viewModel.LogEntries.Add(new LogEntryViewModel
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
                var logs = LoggingService.LoadOperationLogs(month);
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
                    _viewModel.LogEntries.Add(new LogEntryViewModel
                    {
                        Timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        SessionInfo = $"[{GetOpTypeShort(log.Type)}]",
                        Content = $"{status} {log.Target}" + (string.IsNullOrEmpty(log.Details) ? "" : $"\n详情: {log.Details}"),
                        RawData = log
                    });
                }
            }
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

        /// <summary>
        /// 导出当前选中月份的日志为 TXT 文件
        /// </summary>
        public void ExportCurrentLogs()
        {
            if (string.IsNullOrEmpty(_viewModel.SelectedDate)) return;

            if (!DateTime.TryParseExact(_viewModel.SelectedDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                return;
            string month = date.ToString("yyyy-MM");

            string content;
            if (_viewModel.LogType == "Chat")
                content = LoggingService.ExportChatLogsToTxt(month);
            else
                content = LoggingService.ExportOperationLogsToTxt(month);

            if (string.IsNullOrEmpty(content))
            {
                MainWindow.SetStatusMessage("没有可导出的日志内容");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "文本文件|*.txt",
                FileName = $"{_viewModel.LogType}_Log_{_viewModel.SelectedDate}.txt"
            };
            if (dialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dialog.FileName, content);
                MainWindow.SetStatusMessage($"日志已导出至: {System.IO.Path.GetFileName(dialog.FileName)}");

                LoggingService.WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.ExportLog,
                    ActionName = "导出日志",
                    Target = $"{_viewModel.LogType} - {_viewModel.SelectedDate}",
                    Success = true,
                    Details = dialog.FileName
                });
            }
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