using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.ChatFunc;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
using RapidKnowledgeware.Functions.LoggingFunc;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Properties;
using RapidKnowledgeware.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public class MainWindowModel : ObservableObject
    {
        // ---------- 字段绑定 ----------
        public MainModel MainModel { get; set; } = new MainModel();
        private MainWindowService _service;
        private bool _isLogViewVisible = false;
        private LoggingViewModel _logVM;

        /// <summary>
        /// 当前会话是否有消息（委托给 Service）
        /// </summary>
        public bool HasMessages => _service.HasMessages;

        /// <summary>
        /// 根据时间生成的问候语（委托给 Service）
        /// </summary>
        public string GreetingText => _service.GreetingText;

        // ---------- 构造函数 ----------
        public MainWindowModel()
        {
            LoggingService.CleanOldLogs();

            _service = new MainWindowService(this, MainModel);
            _service.LoadSessions();

            if (Sessions.Count == 0)
                _service.CreateNewSession();

            var firstSession = Sessions.FirstOrDefault();
            if (firstSession != null)
            {
                firstSession.IsSelected = true;
                SelectedSession = firstSession;
            }
            IsSpaceAdjustVisible = false;

            _service.AttachSessionPropertyChanged();
            Sessions.CollectionChanged += _service.OnSessionsCollectionChanged;



        }

        /// <summary>
        /// 关闭主窗口命令
        /// </summary>
        private CommandBase _closeMainWindowCommand;
        public CommandBase CloseMainWindowCommand
        {
            get
            {
                if (_closeMainWindowCommand == null)
                {
                    _closeMainWindowCommand = new CommandBase();
                    _closeMainWindowCommand.DoExecute = new Action<object>((o) =>
                    {
                        _service.SaveAllSettings();
                        (o as Window).Close();
                    });
                }
                return _closeMainWindowCommand;
            }
        }

        /// <summary>
        /// 打开空间参数视图命令
        /// </summary>
        private CommandBase _openSpaceParametersViewCommand;
        private bool _isSpaceViewVisible = false;
        public CommandBase OpenSpaceParametersViewCommand
        {
            get
            {
                if (_openSpaceParametersViewCommand == null)
                {
                    _openSpaceParametersViewCommand = new CommandBase();
                    _openSpaceParametersViewCommand.DoExecute = new Action<object>((o) =>
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        var overlay = mainWindow?.FindName("OverlayContainer") as Grid;
                        var spaceView = mainWindow?.FindName("SpaceView") as SpaceParametersView;
                        if (overlay == null || spaceView == null) return;

                        if (!_isSpaceViewVisible)
                        {
                            WindowControls.Hide_Title();
                            SlidingView.SlideInFromLeft(spaceView, overlay);
                            _isSpaceViewVisible = true;
                        }
                        else
                        {
                            WindowControls.Show_Title();
                            SlidingView.HideImmediately(spaceView, overlay);
                            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
                            _isSpaceViewVisible = false;
                        }
                    });
                }
                return _openSpaceParametersViewCommand;
            }
        }

        /// <summary>
        /// 打开日志视图命令
        /// </summary>
        private CommandBase _logCommand;
        public CommandBase LogCommand
        {
            get
            {
                if (_logCommand == null)
                {
                    _logCommand = new CommandBase();
                    _logCommand.DoExecute = new Action<object>((o) =>
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        var overlay = mainWindow?.FindName("LogOverlayContainer") as Grid;
                        var logView = mainWindow?.FindName("LogView") as LoggingView;
                        if (overlay == null || logView == null) return;

                        // 直接使用视图自带的 ViewModel（永远不为 null）
                        var vm = logView.ViewModel;
                        vm.SetLoggingView(logView);   // 确保 UI 服务知道视图引用

                        if (!_isLogViewVisible)
                        {
                            vm.Show(overlay, logView);
                            _isLogViewVisible = true;
                        }
                        else
                        {
                            vm.Hide(overlay, logView);
                            _isLogViewVisible = false;
                        }
                    });
                }
                return _logCommand;
            }
        }



        // ---------- 会话管理属性 ----------
        private ObservableCollection<ChatSessionModel> _sessions;
        public ObservableCollection<ChatSessionModel> Sessions
        {
            get => _sessions;
            set { _sessions = value; RaisePropertyChanged(); }
        }

        private ChatSessionModel _selectedSession;
        public ChatSessionModel SelectedSession
        {
            get => _selectedSession;
            set
            {
                if (_selectedSession != value)
                {
                    if (_selectedSession != null)
                        _selectedSession.IsSelected = false;
                    _selectedSession = value;
                    if (_selectedSession != null)
                        _selectedSession.IsSelected = true;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(CurrentMessages));
                    RaisePropertyChanged(nameof(HasMessages));      // 添加：切换会话时刷新输入框位置
                    RaisePropertyChanged(nameof(GreetingText));    // 添加：切换会话时刷新问候语
                    (_sendMessageCommand as CommandBase)?.RaiseCanExecuteChanged();
                    MainWindow.ScrollChatToEnd();   // 切换会话后自动滚动到底部
                }
            }
        }

        public ObservableCollection<ChatMessageModel> CurrentMessages => SelectedSession?.Messages;

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                RaisePropertyChanged();
                (_sendMessageCommand as CommandBase)?.RaiseCanExecuteChanged();
            }
        }

        // 右侧 SpaceAdjustView 弹出控制
        private bool _isSpaceAdjustVisible;
        public bool IsSpaceAdjustVisible
        {
            get => _isSpaceAdjustVisible;
            set { _isSpaceAdjustVisible = value; RaisePropertyChanged(); }
        }

        private SpaceAdjustViewModel _spaceAdjustVM;
        public SpaceAdjustViewModel SpaceAdjustVM
        {
            get => _spaceAdjustVM;
            set { _spaceAdjustVM = value; RaisePropertyChanged(); }
        }

        // ---------- 命令定义 ----------

        /// <summary>
        /// 新建会话命令
        /// </summary>
        private CommandBase _newSessionCommand;
        public CommandBase NewSessionCommand
        {
            get
            {
                if (_newSessionCommand == null)
                {
                    _newSessionCommand = new CommandBase();
                    _newSessionCommand.DoExecute = new Action<object>(_ => _service.CreateNewSession());
                }
                return _newSessionCommand;
            }
        }

        /// <summary>
        /// 发送消息命令
        /// </summary>
        private CommandBase _sendMessageCommand;
        public CommandBase SendMessageCommand
        {
            get
            {
                if (_sendMessageCommand == null)
                {
                    _sendMessageCommand = new CommandBase();
                    _sendMessageCommand.DoExecute = new Action<object>(async _ => await _service.SendMessageAsync());
                    _sendMessageCommand.DoCanExecute = new Func<object, bool>(_ =>
                        !string.IsNullOrWhiteSpace(InputText));
                }
                return _sendMessageCommand;
            }
        }

        /// <summary>
        /// 编辑会话命令
        /// </summary>
        private CommandBase _editSessionCommand;
        public CommandBase EditSessionCommand
        {
            get
            {
                if (_editSessionCommand == null)
                {
                    _editSessionCommand = new CommandBase();
                    _editSessionCommand.DoExecute = new Action<object>(param => _service.EditSelectedSession(param));
                }
                return _editSessionCommand;
            }
        }

        /// <summary>
        /// 关闭空间调整视图命令
        /// </summary>
        private CommandBase _closeSpaceAdjustCommand;
        public CommandBase CloseSpaceAdjustCommand
        {
            get
            {
                if (_closeSpaceAdjustCommand == null)
                {
                    _closeSpaceAdjustCommand = new CommandBase();
                    _closeSpaceAdjustCommand.DoExecute = new Action<object>(_ => _service.HideSpaceAdjustView());
                }
                return _closeSpaceAdjustCommand;
            }
        }

        /// <summary>
        /// 删除会话命令
        /// </summary>
        private CommandBase _deleteSessionCommand;
        public CommandBase DeleteSessionCommand
        {
            get
            {
                if (_deleteSessionCommand == null)
                {
                    _deleteSessionCommand = new CommandBase();
                    _deleteSessionCommand.DoExecute = new Action<object>(param =>
                    {
                        var session = param as ChatSessionModel;
                        if (session != null) _service.DeleteSession(session);
                    });
                }
                return _deleteSessionCommand;
            }
        }

        /// <summary>
        /// 重命名会话命令
        /// </summary>
        private CommandBase _renameSessionCommand;
        public CommandBase RenameSessionCommand
        {
            get
            {
                if (_renameSessionCommand == null)
                {
                    _renameSessionCommand = new CommandBase();
                    _renameSessionCommand.DoExecute = new Action<object>(param =>
                    {
                        var session = param as ChatSessionModel;
                        if (session != null) _service.RenameSession(session);
                    });
                }
                return _renameSessionCommand;
            }
        }


        /// <summary>
        /// 设置 SpaceAdjustView 的视图引用（由 MainWindow 调用）
        /// </summary>
        public void SetSpaceAdjustViewReferences(Grid overlay, SpaceAdjustView view)
        {
            _service.SetSpaceAdjustViewReferences(overlay, view);
        }

        /// <summary>
        /// 窗口关闭时保存所有设置
        /// </summary>
        public void OnWindowClosing()
        {
            _service.SaveAllSettings();
        }

        public void RefreshUIAssistProperties()
        {
            RaisePropertyChanged(nameof(HasMessages));
            RaisePropertyChanged(nameof(GreetingText));
        }

        /// <summary>
        /// 保存所有会话（供外部调用）
        /// </summary>
        public void SaveSessions()
        {
            _service.SaveSessions();
        }
    }
}