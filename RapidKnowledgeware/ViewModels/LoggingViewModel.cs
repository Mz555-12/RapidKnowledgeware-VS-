using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.LoggingFunc;
using RapidKnowledgeware.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace RapidKnowledgeware.ViewModels
{
    public class LoggingViewModel : ObservableObject
    {
        private readonly LoggingUIService _uiService;

        public LoggingViewModel()
        {
            _uiService = new LoggingUIService(this);
        }

        /// <summary>
        /// 设置日志视图引用
        /// </summary>
        public void SetLoggingView(LoggingView view)
        {
            _uiService.SetLoggingView(view);
        }

        /// <summary>
        /// 显示日志覆盖层
        /// </summary>
        public void Show(Grid overlayContainer, LoggingView view)
        {
            _uiService.ShowLoggingView(overlayContainer, view);
        }

        /// <summary>
        /// 隐藏日志覆盖层
        /// </summary>
        public void Hide(Grid overlayContainer, LoggingView view)
        {
            _uiService.HideLoggingView(overlayContainer, view);
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

        private string _selectedMonth;
        /// <summary>
        /// 当前选中的月份
        /// </summary>
        public string SelectedMonth
        {
            get => _selectedMonth;
            set { _selectedMonth = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 可用的月份列表
        /// </summary>
        public ObservableCollection<string> AvailableMonths { get; } = new ObservableCollection<string>();

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
                            _uiService.RefreshMonthList();
                            _uiService.LoadLogs();
                        }
                    });
                }
                return _switchLogTypeCommand;
            }
        }

        /// <summary>
        /// 选择月份命令
        /// </summary>
        private CommandBase _selectMonthCommand;
        public CommandBase SelectMonthCommand
        {
            get
            {
                if (_selectMonthCommand == null)
                {
                    _selectMonthCommand = new CommandBase();
                    _selectMonthCommand.DoExecute = new Action<object>(param =>
                    {
                        if (param is string month)
                        {
                            SelectedMonth = month;
                            _uiService.LoadLogs();
                        }
                    });
                }
                return _selectMonthCommand;
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
                        _uiService.LoadLogs();
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
                        _uiService.RefreshMonthList();
                        _uiService.LoadLogs();
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
                        _uiService.ExportCurrentLogs();
                    });
                }
                return _exportCommand;
            }
        }

        #endregion
    }
}