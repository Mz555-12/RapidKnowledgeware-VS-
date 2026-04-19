using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.ChatFunc;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
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
        // ---------- 原有成员 ----------
        public MainModel MainModel { get; set; } = new MainModel();
        private bool _isUpdatingSelection = false; // 防止递归


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
                        SaveAllSettings();
                        (o as Window).Close();
                    });
                }
                return _closeMainWindowCommand;
            }
        }

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

        // ---------- 新增会话管理 ----------
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
                    _isUpdatingSelection = true;
                    if (_selectedSession != null)
                        _selectedSession.IsSelected = false;
                    _selectedSession = value;
                    if (_selectedSession != null)
                        _selectedSession.IsSelected = true;
                    _isUpdatingSelection = false;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(CurrentMessages));
                    (_sendMessageCommand as CommandBase)?.RaiseCanExecuteChanged();
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
                // 通知发送命令刷新可用状态
                (_sendMessageCommand as CommandBase)?.RaiseCanExecuteChanged();
            }
        }

        // 右侧 SpaceAdjustView 弹出（用于编辑会话参数）
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

        // 视图引用（由 MainWindow 设置）
        private Grid _spaceAdjustOverlay;
        private SpaceAdjustView _spaceAdjustViewControl;

        // 命令
        private CommandBase _newSessionCommand;
        public CommandBase NewSessionCommand
        {
            get
            {
                if (_newSessionCommand == null)
                {
                    _newSessionCommand = new CommandBase();
                    _newSessionCommand.DoExecute = new Action<object>(_ => CreateNewSession());
                }
                return _newSessionCommand;
            }
        }

        private CommandBase _sendMessageCommand;
        public CommandBase SendMessageCommand
        {
            get
            {
                if (_sendMessageCommand == null)
                {
                    _sendMessageCommand = new CommandBase();
                    _sendMessageCommand.DoExecute = new Action<object>(async _ => await SendMessage());
                    _sendMessageCommand.DoCanExecute = new Func<object, bool>(_ =>
                        !string.IsNullOrWhiteSpace(InputText));
                }
                return _sendMessageCommand;
            }
        }

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
                        Debug.WriteLine($"[Edit] 参数: {param}");
                        EditSelectedSession(param);
                    });
                    _editSessionCommand.DoCanExecute = new Func<object, bool>(_ => true); // 始终可用
                }
                return _editSessionCommand;
            }
        }

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

        private ChatService _currentChatService;

        // 构造函数
        public MainWindowModel()
        {
            LoadSessions();
            if (Sessions.Count == 0)
                CreateNewSession();
            var firstSession = Sessions.FirstOrDefault();
            if (firstSession != null)
            {
                firstSession.IsSelected = true;
                SelectedSession = firstSession;
            }
            IsSpaceAdjustVisible = false;   // 确保覆盖层初始隐藏

            // 订阅每个会话的属性变更，以同步 IsSelected -> SelectedSession
            AttachSessionPropertyChanged();
            Sessions.CollectionChanged += OnSessionsCollectionChanged;
        }

        private void CreateNewSession()
        {
            // 检查当前选中会话是否有聊天记录，若没有则不允许新建
            if (SelectedSession != null && SelectedSession.Messages.Count == 0)
            {
                MainWindow.SetStatusMessage($"当前“{SelectedSession.DisplayName}”为空，请先发送消息再新建对话");
                return;
            }

            string baseName = "对话";

            // 找出最小的未使用编号
            int newNumber = 1;
            while (Sessions.Any(s => s.DisplayName == $"{baseName} {newNumber}"))
            {
                newNumber++;
            }

            string newName = $"{baseName} {newNumber}";

            var newSession = new ChatSessionModel
            {
                DisplayName = newName,
                SpaceParameters = LLMAdjustService.CreateSpaceParametersFromDefault()
            };
            Sessions.Insert(0, newSession);
            SelectedSession = newSession;
            SaveSessions();
            MainWindow.SetStatusMessage($"已创建新会话：{newName}");
        }

        private async System.Threading.Tasks.Task SendMessage()
        {
            // 如果没有选中任何会话，自动新建一个
            if (SelectedSession == null)
            {
                CreateNewSession();
            }

            var userInput = InputText;
            InputText = "";

            _currentChatService = new ChatService(SelectedSession);
            await _currentChatService.SendMessageAsync(userInput, token => { /* UI 已自动更新 */ });
            SaveSessions();
        }

        private void EditSelectedSession(object parameter)
        {
            var session = parameter as ChatSessionModel;
            if (session == null)
            {
                Debug.WriteLine("[Edit] 参数无效");
                return;
            }
            Debug.WriteLine($"[Edit] 编辑会话: {session.DisplayName}");
            SelectedSession = session;
            SpaceAdjustVM = new SpaceAdjustViewModel(session, CloseSpaceAdjustCommand);
            ShowSpaceAdjustView();
        }

        public void SetSpaceAdjustViewReferences(Grid overlay, SpaceAdjustView view)
        {
            _spaceAdjustOverlay = overlay;
            _spaceAdjustViewControl = view;
        }

        private void ShowSpaceAdjustView()
        {
            Debug.WriteLine("[ShowSpaceAdjustView] 开始显示");
            if (_spaceAdjustOverlay == null || _spaceAdjustViewControl == null) return;
            WindowControls.Hide_Title(0);
            SlidingView.SlideInFromLeft(_spaceAdjustViewControl, _spaceAdjustOverlay);
            IsSpaceAdjustVisible = true;
            Debug.WriteLine($"[ShowSpaceAdjustView] Visibility={_spaceAdjustOverlay.Visibility}");
        }

        private void HideSpaceAdjustView()
        {
            Debug.WriteLine($"[HideSpaceAdjustView] 调用堆栈: {Environment.StackTrace}");
            Debug.WriteLine("[HideSpaceAdjustView] 开始隐藏");
            if (_spaceAdjustOverlay == null || _spaceAdjustViewControl == null) return;
            WindowControls.Show_Title();
            SlidingView.HideImmediately(_spaceAdjustViewControl, _spaceAdjustOverlay);
            IsSpaceAdjustVisible = false;
            SaveSessions();
            Debug.WriteLine($"[HideSpaceAdjustView] Visibility={_spaceAdjustOverlay.Visibility}");
        }

        // 持久化
        private void LoadSessions()
        {
            var loaded = AppSettingsManager.LoadSettings<ObservableCollection<ChatSessionModel>>();
            Sessions = loaded ?? new ObservableCollection<ChatSessionModel>();

            // 清除所有会话的选中状态，避免多个被选中
            foreach (var session in Sessions)
            {
                session.IsSelected = false;
            }
        }
        private void SaveSessions()
        {
            AppSettingsManager.SaveSettings(Sessions);
        }

        private void SaveAllSettings()
        {
            SaveSessions();
            LLMAdjustService.Save();
            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
        }

        // 供 MainWindow 调用保存
        public void OnWindowClosing()
        {
            SaveAllSettings();
        }
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
                        if (session == null) return;
                        DeleteSession(session);
                    });
                }
                return _deleteSessionCommand;
            }
        }

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
                        if (session == null) return;
                        RenameSession(session);
                    });
                }
                return _renameSessionCommand;
            }
        }

        private void DeleteSession(ChatSessionModel session)
        {
            var result = System.Windows.MessageBox.Show(
                $"确定要删除会话“{session.DisplayName}”吗？",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            int index = Sessions.IndexOf(session);
            string deletedName = session.DisplayName;
            Sessions.Remove(session);

            // 如果删除后没有会话了，自动新建一个
            if (Sessions.Count == 0)
            {
                // 创建自动会话，名称固定为“自动创建的对话”
                var autoSession = new ChatSessionModel
                {
                    DisplayName = "自动创建的对话",
                    SpaceParameters = LLMAdjustService.CreateSpaceParametersFromDefault()
                };
                Sessions.Add(autoSession);
                SelectedSession = autoSession;
                MainWindow.SetStatusMessage($"已删除最后一个会话“{deletedName}”，已自动创建新会话");
            }
            else if (SelectedSession == session)
            {
                int newIndex = Math.Min(index, Sessions.Count - 1);
                SelectedSession = Sessions[newIndex];
                MainWindow.SetStatusMessage($"已删除会话：{deletedName}");
            }
            else
            {
                MainWindow.SetStatusMessage($"已删除会话：{deletedName}");
            }

            SaveSessions();
            Debug.WriteLine($"[Delete] 已删除会话: {deletedName}");
        }

        private void RenameSession(ChatSessionModel session)
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入新名称：",
                "重命名会话",
                session.DisplayName,
                -1, -1);

            if (string.IsNullOrWhiteSpace(newName))
                return;

            newName = newName.Trim();

            if (newName == session.DisplayName)
                return;

            // 检查是否与其他会话重名
            if (Sessions.Any(s => s != session && s.DisplayName == newName))
            {
                System.Windows.MessageBox.Show(
                    $"名称“{newName}”已存在，请使用其他名称。",
                    "重命名失败",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            string oldName = session.DisplayName;
            session.DisplayName = newName;
            SaveSessions();
            Debug.WriteLine($"[Rename] 会话重命名为: {newName}");
            MainWindow.SetStatusMessage($"已将“{oldName}”重命名为“{newName}”");
        }

        private void OnSessionsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ChatSessionModel session in e.NewItems)
                {
                    session.PropertyChanged += OnSessionPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ChatSessionModel session in e.OldItems)
                {
                    session.PropertyChanged -= OnSessionPropertyChanged;
                }
            }
        }

        private void AttachSessionPropertyChanged()
        {
            foreach (var session in Sessions)
            {
                session.PropertyChanged += OnSessionPropertyChanged;
            }
        }

        private void OnSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            if (e.PropertyName == nameof(ChatSessionModel.IsSelected))
            {
                var session = sender as ChatSessionModel;
                if (session != null && session.IsSelected && SelectedSession != session)
                {
                    _isUpdatingSelection = true;
                    SelectedSession = session;
                    _isUpdatingSelection = false;
                }
            }
        }

      


    }
}