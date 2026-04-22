using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.ChatFunc;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RapidKnowledgeware.Functions.MainWindowFunc
{
    /// <summary>
    /// 主窗口业务逻辑服务类
    /// </summary>
    public class MainWindowService : ObservableObject
    {
        private readonly MainWindowModel _viewModel;
        private readonly MainModel _mainModel;
        private ChatService _currentChatService;

        // 视图引用
        private Grid _spaceAdjustOverlay;
        private SpaceAdjustView _spaceAdjustViewControl;

        /// <summary>
        /// 防止递归的标志
        /// </summary>
        private bool _isUpdatingSelection = false;

        /// <summary>
        /// 初始化主窗口服务
        /// </summary>
        /// <param name="viewModel">主窗口 ViewModel</param>
        /// <param name="mainModel">主窗口数据模型</param>
        public MainWindowService(MainWindowModel viewModel, MainModel mainModel)
        {
            _viewModel = viewModel;
            _mainModel = mainModel;
        }

        #region 会话管理

        /// <summary>
        /// 创建新会话
        /// </summary>
        public void CreateNewSession()
        {
            // 检查当前选中会话是否有聊天记录，若没有则不允许新建
            var latestSession = _viewModel.Sessions.FirstOrDefault();
            if (latestSession != null && latestSession.Messages.Count == 0)
            {
                // 如果当前选中的不是该空会话，则跳转过去
                if (_viewModel.SelectedSession != latestSession)
                {
                    _viewModel.SelectedSession = latestSession;
                    MainWindow.SetStatusMessage($"已切换到空会话“{latestSession.DisplayName}”");
                }
                else
                {
                    MainWindow.SetStatusMessage($"“{latestSession.DisplayName}”无聊天记录，请先发送消息再新建对话");
                }
                return;
            }

            string baseName = "对话";

            // 找出最小的未使用编号
            int newNumber = 1;
            while (_viewModel.Sessions.Any(s => s.DisplayName == $"{baseName} {newNumber}"))
            {
                newNumber++;
            }

            string newName = $"{baseName} {newNumber}";

            // 修改点：显式初始化 SpaceParameters，包含知识库默认参数
            var newSession = new ChatSessionModel
            {
                DisplayName = newName,
                SpaceParameters = new SpaceAdjustModel
                {
                    ChatLLM = LLMAdjustService.Current.Default_ChatLLM,
                    Temperature = LLMAdjustService.Current.Default_Temperature,
                    TopP = LLMAdjustService.Current.Default_TopP,
                    RepeatPenalty = LLMAdjustService.Current.Default_RepeatPenalty,
                    SystemPrompt = LLMAdjustService.Current.Default_SystemPrompt,
                    QueryRefusalResponse = LLMAdjustService.Current.Default_RefusalResponse,
                    IsLinkKnowledgeBase = true,
                    SearchQuantity = KnowledgeBaseModel.Instance.Default_SearchQuantity,
                    IndexSimilarityThreshold = KnowledgeBaseModel.Instance.Default_IndexSimilarityThreshold
                }
            };
            _viewModel.Sessions.Insert(0, newSession);
            _viewModel.SelectedSession = newSession;
            SaveSessions();

            // 记录操作日志
            LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
            {
                Timestamp = DateTime.Now,
                Type = OperationType.CreateSession,
                ActionName = "新建会话",
                Target = newName,
                Success = true
            });

            MainWindow.SetStatusMessage($"已创建新会话：{newName}");
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async System.Threading.Tasks.Task SendMessageAsync()
        {
            // 如果没有选中任何会话，自动新建一个
            if (_viewModel.SelectedSession == null)
            {
                CreateNewSession();
            }

            var userInput = _viewModel.InputText;
            _viewModel.InputText = "";

            _currentChatService = new ChatService(_viewModel.SelectedSession);
            await _currentChatService.SendMessageAsync(userInput, token => { /* UI 已自动更新 */ });
            _viewModel.RefreshUIAssistProperties();   // 消息已添加，刷新界面
            SaveSessions();
        }

        /// <summary>
        /// 编辑选中的会话
        /// </summary>
        /// <param name="parameter">会话对象</param>
        public void EditSelectedSession(object parameter)
        {
            var session = parameter as ChatSessionModel;
            if (session == null)
            {
                Debug.WriteLine("[Edit] 参数无效");
                return;
            }
            Debug.WriteLine($"[Edit] 编辑会话: {session.DisplayName}，IsLinkKnowledgeBase = {session.SpaceParameters.IsLinkKnowledgeBase}");

            // 直接使用内存中的会话对象创建 ViewModel，无需从磁盘重新加载（避免覆盖内存中的最新状态）
            _viewModel.SelectedSession = session;
            _viewModel.SpaceAdjustVM = new SpaceAdjustViewModel(session, _viewModel.CloseSpaceAdjustCommand);
            ShowSpaceAdjustView();

        }

        /// <summary>
        /// 删除会话
        /// </summary>
        /// <param name="session">要删除的会话</param>
        public void DeleteSession(ChatSessionModel session)
        {
            var result = MessageBox.Show(
                $"确定要删除会话“{session.DisplayName}”吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            int index = _viewModel.Sessions.IndexOf(session);
            string deletedName = session.DisplayName;
            _viewModel.Sessions.Remove(session);

            // 如果删除后没有会话了，自动新建一个
            if (_viewModel.Sessions.Count == 0)
            {
                var autoSession = new ChatSessionModel
                {
                    DisplayName = "自动创建的对话",
                    SpaceParameters = LLMAdjustService.CreateSpaceParametersFromDefault()
                };
                _viewModel.Sessions.Add(autoSession);
                _viewModel.SelectedSession = autoSession;
                MainWindow.SetStatusMessage($"已删除最后一个会话“{deletedName}”，已自动创建新会话");
            }
            else if (_viewModel.SelectedSession == session)
            {
                int newIndex = Math.Min(index, _viewModel.Sessions.Count - 1);
                _viewModel.SelectedSession = _viewModel.Sessions[newIndex];
                MainWindow.SetStatusMessage($"已删除会话：{deletedName}");
            }
            else
            {
                MainWindow.SetStatusMessage($"已删除会话：{deletedName}");
            }

            _viewModel.RefreshUIAssistProperties();
            SaveSessions();

            // 记录操作日志
            LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
            {
                Timestamp = DateTime.Now,
                Type = OperationType.DeleteSession,
                ActionName = "删除会话",
                Target = deletedName,
                Success = true
            });
            Debug.WriteLine($"[Delete] 已删除会话: {deletedName}");
        }

        /// <summary>
        /// 重命名会话
        /// </summary>
        /// <param name="session">要重命名的会话</param>
        public void RenameSession(ChatSessionModel session)
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
            if (_viewModel.Sessions.Any(s => s != session && s.DisplayName == newName))
            {
                MessageBox.Show(
                    $"名称“{newName}”已存在，请使用其他名称。",
                    "重命名失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string oldName = session.DisplayName;
            session.DisplayName = newName;
            SaveSessions();

            // 记录操作日志
            LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
            {
                Timestamp = DateTime.Now,
                Type = OperationType.RenameSession,
                ActionName = "重命名会话",
                Target = newName,
                Success = true,
                Details = $"原名称: {oldName}"
            });

            Debug.WriteLine($"[Rename] 会话重命名为: {newName}");
            MainWindow.SetStatusMessage($"已将“{oldName}”重命名为“{newName}”");
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 加载所有会话
        /// </summary>
        public void LoadSessions()
        {
            var loaded = AppSettingsManager.LoadSettings<ObservableCollection<ChatSessionModel>>();
            _viewModel.Sessions = loaded ?? new ObservableCollection<ChatSessionModel>();

            // 清除所有会话的选中状态，避免多个被选中
            foreach (var session in _viewModel.Sessions)
            {
                session.IsSelected = false;
            }
        }

        /// <summary>
        /// 保存所有会话
        /// </summary>
        public void SaveSessions()
        {
            AppSettingsManager.SaveSettings(_viewModel.Sessions);
        }

        /// <summary>
        /// 保存所有设置（会话 + LLM参数 + 知识库）
        /// </summary>
        public void SaveAllSettings()
        {
            SaveSessions();
            LLMAdjustService.Save();
            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
        }


        // RapidKnowledgeware.Functions.MainWindowFunc.MainWindowService.cs 中的 RecordAndSaveAllSettingsChanges 方法
        /// <summary>
        /// 对比知识库模型和 LLM 全局模型的快照，记录详细变更日志，并保存配置
        /// </summary>
        public void RecordAndSaveAllSettingsChanges()
        {
            // 1. 处理 LLM 全局模型
            LLMAdjustFunc.LLMAdjustService.Save();

            // 2. 处理知识库模型
            var kbModel = KnowledgeBaseModel.Instance;
            string kbChanges = SettingsChangeTracker.GetChangesAndClear(kbModel);
            if (!string.IsNullOrEmpty(kbChanges))
            {
                string formattedChanges = kbChanges.Replace("; ", "\n");
                LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.EditKnowledgeBaseParams,
                    ActionName = "编辑知识库参数",
                    Target = "全局知识库设置",
                    Success = true,
                    Details = formattedChanges
                });
                Debug.WriteLine($"[MainWindowService] 知识库参数变更:\n{formattedChanges}");
            }

            // 3. 保存知识库配置到文件
            AppSettingsManager.SaveSettings(kbModel);

            // 4. 重新捕获快照
            SettingsChangeTracker.CaptureSnapshot(kbModel);
        }

        

        #endregion

        #region SpaceAdjustView 动画控制

        /// <summary>
        /// 设置 SpaceAdjustView 的视图引用
        /// </summary>
        /// <param name="overlay">覆盖层容器</param>
        /// <param name="view">SpaceAdjustView 控件</param>
        public void SetSpaceAdjustViewReferences(Grid overlay, SpaceAdjustView view)
        {
            _spaceAdjustOverlay = overlay;
            _spaceAdjustViewControl = view;
        }

        /// <summary>
        /// 显示 SpaceAdjustView
        /// </summary>
        public void ShowSpaceAdjustView()
        {
            Debug.WriteLine("[ShowSpaceAdjustView] 开始显示");
            if (_spaceAdjustOverlay == null || _spaceAdjustViewControl == null) return;
            WindowControls.Hide_Title(0);
            SlidingView.SlideInFromLeft(_spaceAdjustViewControl, _spaceAdjustOverlay);
            _viewModel.IsSpaceAdjustVisible = true;
            Debug.WriteLine($"[ShowSpaceAdjustView] Visibility={_spaceAdjustOverlay.Visibility}");
        }

        /// <summary>
        /// 隐藏 SpaceAdjustView
        /// </summary>
        public void HideSpaceAdjustView()
        {
            Debug.WriteLine("[HideSpaceAdjustView] 开始隐藏");
            if (_spaceAdjustOverlay == null || _spaceAdjustViewControl == null) return;

            // 对比空间参数变更并记录详细日志
            if (_viewModel.SpaceAdjustVM?.SpaceAdjustModel != null)
            {
                string changes = Functions.MainWindowFunc.SettingsChangeTracker.GetChangesAndClear(_viewModel.SpaceAdjustVM.SpaceAdjustModel);
                if (!string.IsNullOrEmpty(changes))
                {
                    var session = _viewModel.SpaceAdjustVM.Session;
                    // 将分号替换为换行，美化输出
                    string formattedChanges = changes.Replace("; ", "\n");
                    LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
                    {
                        Timestamp = DateTime.Now,
                        Type = OperationType.EditSessionParams,
                        ActionName = "编辑会话参数",
                        Target = session.DisplayName,
                        Success = true,
                        Details = formattedChanges
                    });
                    Debug.WriteLine($"[HideSpaceAdjustView] 参数变更:\n{formattedChanges}");
                }
            }

            WindowControls.Show_Title();
            SlidingView.HideImmediately(_spaceAdjustViewControl, _spaceAdjustOverlay);
            _viewModel.IsSpaceAdjustVisible = false;
            SaveSessions();
            Debug.WriteLine($"[HideSpaceAdjustView] Visibility={_spaceAdjustOverlay.Visibility}");
        }

        #endregion

        #region 会话集合变更订阅

        /// <summary>
        /// 订阅所有会话的属性变更事件
        /// </summary>
        public void AttachSessionPropertyChanged()
        {
            foreach (var session in _viewModel.Sessions)
            {
                session.PropertyChanged += OnSessionPropertyChanged;
            }
        }

        /// <summary>
        /// 会话集合变更时的处理
        /// </summary>
        public void OnSessionsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

        /// <summary>
        /// 单个会话属性变更时的处理（用于同步 IsSelected）
        /// </summary>
        private void OnSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            if (e.PropertyName == nameof(ChatSessionModel.IsSelected))
            {
                var session = sender as ChatSessionModel;
                if (session != null && session.IsSelected && _viewModel.SelectedSession != session)
                {
                    _isUpdatingSelection = true;
                    _viewModel.SelectedSession = session;
                    _isUpdatingSelection = false;
                }
            }
        }

        #endregion

        #region UI 辅助状态

        /// <summary>
        /// 获取当前选中会话是否有消息（用于控制输入框位置）
        /// </summary>
        public bool HasMessages => _viewModel.SelectedSession != null && _viewModel.SelectedSession.Messages.Count > 0;

        /// <summary>
        /// 根据当前时间生成问候语
        /// </summary>
        public string GreetingText => GetGreetingByTime();

        /// <summary>
        /// 根据当前时间获取问候语
        /// </summary>
        private string GetGreetingByTime()
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 5 && hour < 12)
                return "早上好，有什么需要帮助的嘛？";
            else if (hour >= 12 && hour < 14)
                return "中午好，有什么需要帮助的嘛？";
            else if (hour >= 14 && hour < 18)
                return "下午好，有什么需要帮助的嘛？";
            else if (hour >= 18 && hour < 24)
                return "晚上好，有什么需要帮助的嘛？";
            else
                return "凌晨好，有什么需要帮助的嘛？";
        }

        

        #endregion
    }
}