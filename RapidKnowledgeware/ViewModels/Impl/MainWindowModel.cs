using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Helpers;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Properties;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IOC;
using IOC.Annotations;

namespace RapidKnowledgeware.ViewModels.Impl
{
    [Controller]
    public class MainWindowModel : ObservableObject, IMainWindowModel
    {
        // ---------- 字段绑定 ----------
        public MainModel MainModel { get; set; } = new MainModel();

        /// <summary>
        /// LLM 全局参数模型（用于 XAML 绑定）
        /// </summary>
        public LLMAdjustModel LLMAdjustModel => BeanFactory.GetBean<ILLMAdjustService>().Current;

        private IMainWindowService _service;
        private bool _isLogViewVisible = false;

        // SpaceAdjustView 引用（由 SetSpaceAdjustViewReferences 设置）
        private Grid _spaceAdjustOverlay;
        private SpaceAdjustView _spaceAdjustViewControl;

        /// <summary>
        /// 当前会话是否有消息
        /// </summary>
        public bool HasMessages => SelectedSession?.Messages?.Count > 0;

        /// <summary>
        /// 根据时间生成的问候语
        /// </summary>
        public string GreetingText => _service.GetGreetingByTime();


        private bool _greetingAnimationPlayed = false; // 是否已播放过问候动画
        private bool _isGreetingAnimating = false;     // 动画是否正在进行
        private CancellationTokenSource _greetingCts;  // 取消令牌


        private string _greetingDisplayText = "";
        /// <summary>
        /// 问候语流式显示文本（动画逐字填充）
        /// </summary>
        public string GreetingDisplayText
        {
            get => _greetingDisplayText;
            set { _greetingDisplayText = value; RaisePropertyChanged(); }
        }



        private bool _isSending = false;
        /// <summary>
        /// 是否正在发送消息（用于切换发送/停止按钮显示）
        /// </summary>
        public bool IsSending
        {
            get => _isSending;
            set { _isSending = value; RaisePropertyChanged(); }
        }

        // ---------- 构造函数 ----------
        public MainWindowModel()
        {
            BeanFactory.GetBean<ILoggingService>().CleanOldLogs();

            _service = BeanFactory.GetBean<IMainWindowService>();
            Sessions = _service.LoadSessions();

            if (Sessions.Count == 0)
            {
                var newSession = _service.CreateNewSession(Sessions);
                if (newSession != null)
                {
                    Sessions.Insert(0, newSession);
                }
            }

            var firstSession = Sessions.FirstOrDefault();
            if (firstSession != null)
            {
                firstSession.IsSelected = true;
                SelectedSession = firstSession;
            }
            IsSpaceAdjustVisible = false;

            AttachSessionPropertyChanged();
            Sessions.CollectionChanged += OnSessionsCollectionChanged;

            // 检查并启动问候动画（程序初次启动无消息时）
            CheckAndStartGreetingAnimation();
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
                        _service.SaveAllSettings(Sessions);
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
                            // === 隐藏前记录变更并保存 ===
                            _service.RecordAndSaveAllSettingsChanges();
                            WindowControls.Show_Title();
                            SlidingView.HideImmediately(spaceView, overlay);
                            BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(KnowledgeBaseModel.Instance);
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

                        var vm = logView.ViewModel;

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

        /// <summary>
        /// 打开调试窗口命令
        /// </summary>
        private CommandBase _openDebugWindowCommand;
        public CommandBase OpenDebugWindowCommand
        {
            get
            {
                if (_openDebugWindowCommand == null)
                {
                    _openDebugWindowCommand = new CommandBase();
                    _openDebugWindowCommand.DoExecute = new Action<object>(_ =>
                    {
                        var debugWindow = new DebugWindow();
                        debugWindow.Owner = Application.Current.MainWindow;//主次关系
                        // 获取工作区尺寸
                        var workArea = SystemParameters.WorkArea;
                        // Left = 0 贴左边
                        debugWindow.Left = 0;
                        // Top = (工作区高度 - 窗口高度) / 2 实现垂直居中
                        debugWindow.Top = (workArea.Height - debugWindow.Height) / 2;
                        // 每次打开调试窗口前清空旧日志（当次运行只保留新产生的内容）
                        DebugModel.Instance.LogEntries.Clear();
                        debugWindow.Show();
                    });
                }
                return _openDebugWindowCommand;
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
                    RaisePropertyChanged(nameof(HasMessages));      // 切换会话时刷新输入框位置
                    RaisePropertyChanged(nameof(GreetingText));    // 切换会话时刷新问候语
                    (_sendMessageCommand as CommandBase)?.RaiseCanExecuteChanged();
                    MainWindow.ScrollChatToEnd();   // 切换会话后自动滚动到底部

                    // 如果切换到有消息的会话，停止问候动画
                    if (HasMessages)
                        StopGreetingAnimation();
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

        private ISpaceAdjustViewModel _spaceAdjustVM;
        public ISpaceAdjustViewModel SpaceAdjustVM
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
                    _newSessionCommand.DoExecute = new Action<object>(_ =>
                    {
                        var newSession = _service.CreateNewSession(Sessions);
                        if (newSession != null)
                        {
                            Sessions.Insert(0, newSession);
                            SelectedSession = newSession;
                            ResetAndStartGreetingAnimation();
                        }
                    });
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
                    _sendMessageCommand.DoExecute = new Action<object>(async _ =>
                    {
                        string input = InputText;
                        if (string.IsNullOrWhiteSpace(input)) return;
                        InputText = string.Empty;
                        IsSending = true;

                        await _service.SendMessageAsync(SelectedSession, input, () =>
                        {
                            IsSending = false;
                            SaveSessions();
                            RefreshUIAssistProperties();
                            CheckAndStartGreetingAnimation();
                        });
                    });
                    _sendMessageCommand.DoCanExecute = new Func<object, bool>(_ =>
                        !string.IsNullOrWhiteSpace(InputText));
                }
                return _sendMessageCommand;
            }
        }

        /// <summary>
        /// 编辑会话命令（显示 SpaceAdjustView）
        /// </summary>
        private CommandBase _editSessionCommand;
        public CommandBase EditSessionCommand
        {
            get
            {
                if (_editSessionCommand == null)
                {
                    _editSessionCommand = new CommandBase();
                    _editSessionCommand.DoExecute = new Action<object>(param =>
                    {
                        var session = param as ChatSessionModel;
                        if (session == null) return;
                        ShowSpaceAdjustView(session);
                    });
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
                    _closeSpaceAdjustCommand.DoExecute = new Action<object>(_ => HideSpaceAdjustView());
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
                        if (session != null)
                        {
                            var newSelected = _service.DeleteSession(session, Sessions);
                            if (newSelected != null)
                                SelectedSession = newSelected;
                        }
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
                        if (session != null) _service.RenameSession(session, Sessions);
                    });
                }
                return _renameSessionCommand;
            }
        }


        /// <summary>
        /// 停止生成消息命令
        /// </summary>
        private CommandBase _stopMessageCommand;
        public CommandBase StopMessageCommand
        {
            get
            {
                if (_stopMessageCommand == null)
                {
                    _stopMessageCommand = new CommandBase();
                    _stopMessageCommand.DoExecute = new Action<object>(_ =>
                    {
                        _service.StopMessage();
                    });
                }
                return _stopMessageCommand;
            }
        }


        private CommandBase _deepThinkingCommand;
        /// <summary>
        /// 切换深度思考模式命令（全局状态）
        /// </summary>
        public CommandBase DeepThinkingCommand
        {
            get
            {
                if (_deepThinkingCommand == null)
                {
                    _deepThinkingCommand = new CommandBase();
                    _deepThinkingCommand.DoExecute = new Action<object>(_ =>
                    {
                        var globalModel = BeanFactory.GetBean<ILLMAdjustService>().Current;
                        bool newState = !globalModel.GlobalIsDeepThinking;
                        globalModel.GlobalIsDeepThinking = newState;
                        BeanFactory.GetBean<ILLMAdjustService>().Save();

                        // 通知 UI 刷新
                        RaisePropertyChanged(nameof(LLMAdjustModel.GlobalIsDeepThinking));

                        string status = newState ? "开启" : "关闭";
                        MainWindow.SetStatusMessage($"深度思考模式已{status}");

                        BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
                        {
                            Timestamp = DateTime.Now,
                            Type = OperationType.ToggleDeepThinking,
                            ActionName = "切换深度思考",
                            Target = "全局设置",
                            Success = true,
                            Details = status
                        });
                    });
                }
                return _deepThinkingCommand;
            }
        }



        private CommandBase _copyCodeCommand;
        /// <summary>
        /// 复制代码块内容到剪贴板命令
        /// </summary>
        public CommandBase CopyCodeCommand
        {
            get
            {
                if (_copyCodeCommand == null)
                {
                    _copyCodeCommand = new CommandBase();
                    _copyCodeCommand.DoExecute = new Action<object>(param =>
                    {
                        if (param is string code)
                        {
                            try { Clipboard.SetText(code); }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"复制失败: {ex.Message}"); }
                        }
                    });
                }
                return _copyCodeCommand;
            }
        }




        /// <summary>
        /// 设置 SpaceAdjustView 的视图引用（由 MainWindow 调用）
        /// </summary>
        public void SetSpaceAdjustViewReferences(Grid overlay, SpaceAdjustView view)
        {
            _spaceAdjustOverlay = overlay;
            _spaceAdjustViewControl = view;
        }

        /// <summary>
        /// 显示 SpaceAdjustView（编辑会话参数）
        /// </summary>
        private void ShowSpaceAdjustView(ChatSessionModel session)
        {
            if (_spaceAdjustOverlay == null || _spaceAdjustViewControl == null) return;

            // 创建并初始化 SpaceAdjustViewModel
            var spaceAdjustVM = BeanFactory.GetBean<ISpaceAdjustViewModel>();
            spaceAdjustVM.Initialize(session, CloseSpaceAdjustCommand);
            SpaceAdjustVM = spaceAdjustVM;

            // 显示视图
            IsSpaceAdjustVisible = true;
            SlidingView.SlideInFromLeft(_spaceAdjustViewControl, _spaceAdjustOverlay);
        }

        /// <summary>
        /// 隐藏 SpaceAdjustView
        /// </summary>
        private void HideSpaceAdjustView()
        {
            if (_spaceAdjustOverlay == null || _spaceAdjustViewControl == null) return;

            // 记录变更
            if (SpaceAdjustVM != null)
                _service.RecordSpaceAdjustChanges(SpaceAdjustVM);

            IsSpaceAdjustVisible = false;
            SlidingView.HideImmediately(_spaceAdjustViewControl, _spaceAdjustOverlay);
        }

        /// <summary>
        /// 窗口关闭时保存所有设置
        /// </summary>
        public void OnWindowClosing()
        {
            // 先停止所有任务并清理状态
            _service.ForceStopAndFinalizeAllSessions(Sessions);

            // 再保存设置（包括被清理后的会话）
            _service.SaveAllSettings(Sessions);
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
            _service.SaveSessions(Sessions);
        }

        #region 会话属性变更自动保存
        /// <summary>
        /// 为所有已有会话附加 PropertyChanged 监听
        /// </summary>
        private void AttachSessionPropertyChanged()
        {
            foreach (var session in Sessions)
            {
                session.PropertyChanged -= Session_PropertyChanged;
                session.PropertyChanged += Session_PropertyChanged;
            }
        }

        /// <summary>
        /// 会话属性变更时自动保存（仅对关键属性触发）
        /// </summary>
        private void Session_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatSessionModel.DisplayName) ||
                e.PropertyName == nameof(ChatSessionModel.SpaceParameters))
            {
                _service.SaveSessions(Sessions);
            }
        }

        /// <summary>
        /// 会话集合变更时：附加/移除监听，并自动保存
        /// </summary>
        private void OnSessionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ChatSessionModel session in e.NewItems)
                    session.PropertyChanged += Session_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (ChatSessionModel session in e.OldItems)
                    session.PropertyChanged -= Session_PropertyChanged;
            }

            _service.SaveSessions(Sessions);
        }

        #endregion

        #region 问候动画
        /// <summary>
        /// 检查条件并启动问候动画（仅当无消息且未播放过）
        /// </summary>
        private void CheckAndStartGreetingAnimation()
        {
            if (!HasMessages && !_greetingAnimationPlayed && !_isGreetingAnimating)
            {
                _ = StartGreetingAnimationAsync();
            }
        }

        /// <summary>
        /// 异步逐字显示问候语
        /// </summary>
        private async System.Threading.Tasks.Task StartGreetingAnimationAsync()
        {
            _isGreetingAnimating = true;
            _greetingCts = new CancellationTokenSource();
            string fullText = GreetingText;
            GreetingDisplayText = "";

            try
            {
                for (int i = 0; i < fullText.Length; i++)
                {
                    if (_greetingCts.Token.IsCancellationRequested)
                        break;

                    GreetingDisplayText += fullText[i];
                    await System.Threading.Tasks.Task.Delay(40, _greetingCts.Token);
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException) { }
            finally
            {
                _isGreetingAnimating = false;
                _greetingAnimationPlayed = true;
                if (!_greetingCts.Token.IsCancellationRequested)
                    GreetingDisplayText = fullText;
            }
        }

        /// <summary>
        /// 停止正在进行的问候动画
        /// </summary>
        public void StopGreetingAnimation()
        {
            _greetingCts?.Cancel();
        }

        /// <summary>
        /// 重置问候动画标志并重新启动（新建会话时调用）
        /// </summary>
        public void ResetAndStartGreetingAnimation()
        {
            _greetingAnimationPlayed = false;
            CheckAndStartGreetingAnimation();
        }

        #endregion
    }
}
