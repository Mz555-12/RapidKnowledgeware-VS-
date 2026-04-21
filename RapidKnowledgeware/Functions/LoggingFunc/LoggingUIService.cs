using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            WindowControls.Hide_Title(0);
            SlidingView.SlideInFromLeft(view, overlayContainer);
            _viewModel.IsLogViewVisible = true;

            // 加载月份列表并默认选中当前月份
            RefreshMonthList();
            string currentMonth = DateTime.Now.ToString("yyyy-MM");
            if (_viewModel.AvailableMonths.Contains(currentMonth))
                _viewModel.SelectedMonth = currentMonth;
            else if (_viewModel.AvailableMonths.Any())
                _viewModel.SelectedMonth = _viewModel.AvailableMonths.First();
        }

        /// <summary>
        /// 隐藏日志覆盖层
        /// </summary>
        public void HideLoggingView(Grid overlayContainer, FrameworkElement view)
        {
            if (overlayContainer == null || view == null) return;
            WindowControls.Show_Title();
            SlidingView.HideImmediately(view, overlayContainer);
            _viewModel.IsLogViewVisible = false;
        }

        /// <summary>
        /// 刷新月份列表（根据当前日志类型）
        /// </summary>
        public void RefreshMonthList()
        {
            string logType = _viewModel.LogType;
            var months = LoggingService.GetAvailableMonths(logType);
            _viewModel.AvailableMonths.Clear();
            foreach (var m in months)
                _viewModel.AvailableMonths.Add(m);

            // 如果当前选中的月份不在列表中，自动选第一个
            if (!_viewModel.AvailableMonths.Contains(_viewModel.SelectedMonth))
                _viewModel.SelectedMonth = _viewModel.AvailableMonths.FirstOrDefault();
        }

        /// <summary>
        /// 加载当前选中的日志数据
        /// </summary>
        public void LoadLogs()
        {
            _viewModel.LogEntries.Clear();
            if (string.IsNullOrEmpty(_viewModel.SelectedMonth)) return;

            if (_viewModel.LogType == "Chat")
            {
                var logs = LoggingService.LoadChatLogs(_viewModel.SelectedMonth);
                string keyword = _viewModel.SearchKeyword?.Trim() ?? "";
                var filtered = string.IsNullOrEmpty(keyword)
                    ? logs
                    : logs.Where(l =>
                        (l.UserMessage?.Contains(keyword) == true) ||
                        (l.AIResponse?.Contains(keyword) == true) ||
                        (l.SessionName?.Contains(keyword) == true)).ToList();

                _viewModel.LogEntries.Clear();
                foreach (var log in filtered.OrderByDescending(l => l.Timestamp))
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
                var logs = LoggingService.LoadOperationLogs(_viewModel.SelectedMonth);
                string keyword = _viewModel.SearchKeyword?.Trim() ?? "";
                var filtered = string.IsNullOrEmpty(keyword)
                    ? logs
                    : logs.Where(l =>
                        l.Target?.Contains(keyword) == true ||
                        (l.Details?.Contains(keyword) == true)).ToList();

                _viewModel.LogEntries.Clear();
                foreach (var log in filtered.OrderByDescending(l => l.Timestamp))
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
            if (string.IsNullOrEmpty(_viewModel.SelectedMonth)) return;

            string content;
            if (_viewModel.LogType == "Chat")
                content = LoggingService.ExportChatLogsToTxt(_viewModel.SelectedMonth);
            else
                content = LoggingService.ExportOperationLogsToTxt(_viewModel.SelectedMonth);

            if (string.IsNullOrEmpty(content))
            {
                MainWindow.SetStatusMessage("没有可导出的日志内容");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "文本文件|*.txt",
                FileName = $"{_viewModel.LogType}_Log_{_viewModel.SelectedMonth}.txt"
            };
            if (dialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dialog.FileName, content);
                MainWindow.SetStatusMessage($"日志已导出至: {System.IO.Path.GetFileName(dialog.FileName)}");

                // 记录导出操作
                LoggingService.WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.ExportLog,
                    Target = $"{_viewModel.LogType} - {_viewModel.SelectedMonth}",
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