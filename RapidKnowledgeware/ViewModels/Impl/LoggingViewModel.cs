using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Helpers;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.Services.Impl;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using IOC;
using IOC.Annotations;

namespace RapidKnowledgeware.ViewModels.Impl
{
    [Controller]
    public class LoggingViewModel : ObservableObject, ILoggingViewModel
    {
        private readonly ILoggingDisplayService _displayService;

        public LoggingViewModel()
        {
            _displayService = BeanFactory.GetBean<ILoggingDisplayService>();
        }

        /// <summary>
        /// 显示日志覆盖层
        /// </summary>
        public void Show(Grid overlayContainer, LoggingView view)
        {
            if (overlayContainer == null || view == null) return;

            // 调用滑动动画显示视图
            WindowControls.Hide_Title(2);
            SlidingView.SlideInFromLeft(view, overlayContainer);
            IsLogViewVisible = true;

            // 刷新数据
            RefreshDateList();
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (AvailableDates.Contains(today))
                SelectedDate = today;
            else if (AvailableDates.Any())
                SelectedDate = AvailableDates.First();

            LoadLogsFromService();
        }

        /// <summary>
        /// 隐藏日志覆盖层
        /// </summary>
        public void Hide(Grid overlayContainer, LoggingView view)
        {
            if (overlayContainer == null || view == null) return;

            WindowControls.Show_Title();
            SlidingView.HideImmediately(view, overlayContainer);
            IsLogViewVisible = false;
        }

        private bool _isLogViewVisible;
        /// <summary>
        /// 日志视图是否可见
        /// </summary>
        public bool IsLogViewVisible
        {
            get => _isLogViewVisible;
            set { _isLogViewVisible = value; RaisePropertyChanged(); }
        }

        private string _logType = "Chat";
        /// <summary>
        /// 当前日志类型（Chat / Operations）
        /// </summary>
        public string LogType
        {
            get => _logType;
            set { _logType = value; RaisePropertyChanged(); }
        }

        private string _selectedDate;
        /// <summary>
        /// 当前选中的日期（格式 yyyy-MM-dd）
        /// </summary>
        public string SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    RaisePropertyChanged();
                    if (!string.IsNullOrEmpty(_selectedDate))
                        LoadLogsFromService();
                }
            }
        }

        /// <summary>
        /// 可用的日期列表（格式 yyyy-MM-dd），按倒序排列
        /// </summary>
        public ObservableCollection<string> AvailableDates { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 日志条目列表（用于 UI 绑定）
        /// </summary>
        public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new ObservableCollection<LogEntryViewModel>();

        private string _searchKeyword;
        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set { _searchKeyword = value; RaisePropertyChanged(); }
        }

        #region 私有方法

        /// <summary>
        /// 刷新日期列表（调用 Service 获取数据，更新集合）
        /// </summary>
        private void RefreshDateList()
        {
            var dates = _displayService.GetAvailableDates(LogType);
            AvailableDates.Clear();
            foreach (var d in dates)
                AvailableDates.Add(d);

            if (!AvailableDates.Contains(SelectedDate))
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (AvailableDates.Contains(today))
                    SelectedDate = today;
                else
                    SelectedDate = AvailableDates.FirstOrDefault();
            }
        }

        /// <summary>
        /// 加载日志数据（调用 Service 获取数据，更新集合）
        /// </summary>
        private void LoadLogsFromService()
        {
            var entries = _displayService.LoadLogs(LogType, SelectedDate, SearchKeyword);
            LogEntries.Clear();
            foreach (var entry in entries)
                LogEntries.Add(entry);
        }

        /// <summary>
        /// 导出当前日志（调用 Service 获取内容，处理文件保存对话框）
        /// </summary>
        private void ExportCurrentLogs()
        {
            string content = _displayService.ExportLogs(LogType, SelectedDate);
            if (string.IsNullOrEmpty(content))
            {
                MainWindow.SetStatusMessage("没有可导出的日志内容");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "文本文件|*.txt",
                FileName = $"{LogType}_Log_{SelectedDate}.txt"
            };
            if (dialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dialog.FileName, content);
                MainWindow.SetStatusMessage($"日志已导出至: {System.IO.Path.GetFileName(dialog.FileName)}");

                BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.ExportLog,
                    ActionName = "导出日志",
                    Target = $"{LogType} - {SelectedDate}",
                    Success = true,
                    Details = dialog.FileName
                });
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 切换日志类型命令
        /// </summary>
        private CommandBase _switchLogTypeCommand;
        public CommandBase SwitchLogTypeCommand
        {
            get
            {
                if (_switchLogTypeCommand == null)
                {
                    _switchLogTypeCommand = new CommandBase();
                    _switchLogTypeCommand.DoExecute = new Action<object>(param =>
                    {
                        if (param is string type)
                        {
                            LogType = type;
                            RefreshDateList();
                            LoadLogsFromService();
                        }
                    });
                }
                return _switchLogTypeCommand;
            }
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        private CommandBase _searchCommand;
        public CommandBase SearchCommand
        {
            get
            {
                if (_searchCommand == null)
                {
                    _searchCommand = new CommandBase();
                    _searchCommand.DoExecute = new Action<object>(_ =>
                    {
                        LoadLogsFromService();
                    });
                }
                return _searchCommand;
            }
        }

        /// <summary>
        /// 刷新命令
        /// </summary>
        private CommandBase _refreshCommand;
        public CommandBase RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new CommandBase();
                    _refreshCommand.DoExecute = new Action<object>(_ =>
                    {
                        RefreshDateList();
                        LoadLogsFromService();
                    });
                }
                return _refreshCommand;
            }
        }

        /// <summary>
        /// 导出命令
        /// </summary>
        private CommandBase _exportCommand;
        public CommandBase ExportCommand
        {
            get
            {
                if (_exportCommand == null)
                {
                    _exportCommand = new CommandBase();
                    _exportCommand.DoExecute = new Action<object>(_ =>
                    {
                        ExportCurrentLogs();
                    });
                }
                return _exportCommand;
            }
        }

        #endregion
    }
}
