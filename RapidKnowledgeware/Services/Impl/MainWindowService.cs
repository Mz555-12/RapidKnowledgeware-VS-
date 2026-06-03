using IOC;
using IOC.Annotations;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Helpers;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// 主窗口业务逻辑服务
    /// </summary>
    [Service]
    public class MainWindowService : IMainWindowService
    {
        private IChatService _currentChatService;

        /// <summary>
        /// 初始化主窗口服务
        /// </summary>
        public MainWindowService() { }

        public void ForceStopAndFinalizeAllSessions(ObservableCollection<ChatSessionModel> sessions)
        {
            // 1. 停止当前生成
            StopMessage();

            // 2. 遍历所有会话，清理加载状态
            foreach (var session in sessions)
            {
                if (session.Messages == null) continue;

                var lastAIMessage = session.Messages.LastOrDefault(m => !m.IsUserMessage);
                if (lastAIMessage != null && lastAIMessage.IsLoading)
                {
                    // 如果还在加载，强制结束，并添加中断提示
                    lastAIMessage.IsLoading = false;
                    if (string.IsNullOrEmpty(lastAIMessage.Content2))
                    {
                        lastAIMessage.Content2 = "[生成被中断]";
                    }
                    else
                    {
                        lastAIMessage.Content2 += "\n[生成被中断]";
                    }
                }
            }
        }

        #region 会话管理

        /// <summary>
        /// 创建新会话，返回新建的 ChatSessionModel；若首个会话无消息则返回 null
        /// </summary>
        public ChatSessionModel CreateNewSession(ObservableCollection<ChatSessionModel> sessions)
        {
            // 检查当前选中会话是否有聊天记录，若没有则不允许新建
            var latestSession = sessions.FirstOrDefault();
            if (latestSession != null && latestSession.Messages.Count == 0)
            {
                return null;
            }

            string baseName = "对话";

            // 找出最小的未使用编号
            int newNumber = 1;
            while (sessions.Any(s => s.DisplayName == $"{baseName} {newNumber}"))
            {
                newNumber++;
            }

            string newName = $"{baseName} {newNumber}";

            // 显式初始化 SpaceParameters，包含知识库默认参数
            var newSession = new ChatSessionModel
            {
                DisplayName = newName,
                SpaceParameters = new SpaceAdjustModel
                {
                    ChatLLM = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_ChatLLM,
                    Temperature = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_Temperature,
                    TopP = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_TopP,
                    RepeatPenalty = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_RepeatPenalty,
                    SystemPrompt = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_SystemPrompt,
                    QueryRefusalResponse = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_RefusalResponse,
                    IsLinkKnowledgeBase = true,
                    SearchQuantity = KnowledgeBaseModel.Instance.Default_SearchQuantity,
                    IndexSimilarityThreshold = KnowledgeBaseModel.Instance.Default_IndexSimilarityThreshold,

                    DeepThinkingLLM = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_DeepThinkingLLM,
                    IsDeepThinking = false
                }
            };

            // 记录操作日志
            BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
            {
                Timestamp = DateTime.Now,
                Type = OperationType.CreateSession,
                ActionName = "新建会话",
                Target = newName,
                Success = true
            });

            return newSession;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async System.Threading.Tasks.Task SendMessageAsync(ChatSessionModel session, string userInput, Action onCompleted)
        {
            _currentChatService = BeanFactory.GetBean<IChatService>();
            _currentChatService.Initialize(session);
            try
            {
                await _currentChatService.SendMessageAsync(userInput, token => { /* UI 已自动更新 */ });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindowService] 发送消息异常: {ex.Message}");
            }
            finally
            {
                onCompleted();
            }
        }

        /// <summary>
        /// 删除会话，返回删除后应选中的 session（或 null 表示用户取消）
        /// </summary>
        public ChatSessionModel DeleteSession(ChatSessionModel session, ObservableCollection<ChatSessionModel> sessions)
        {
            var result = MessageBox.Show(
                $"确定要删除会话\u201C{session.DisplayName}\u201D吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return null;

            int index = sessions.IndexOf(session);
            string deletedName = session.DisplayName;
            sessions.Remove(session);

            ChatSessionModel newSelectedSession;

            // 如果删除后没有会话了，自动新建一个
            if (sessions.Count == 0)
            {
                var autoSession = new ChatSessionModel
                {
                    DisplayName = "自动创建的对话",
                    SpaceParameters = BeanFactory.GetBean<ILLMAdjustService>().CreateSpaceParametersFromDefault()
                };
                sessions.Add(autoSession);
                newSelectedSession = autoSession;
            }
            else
            {
                int newIndex = Math.Min(index, sessions.Count - 1);
                newSelectedSession = sessions[newIndex];
            }

            // 记录操作日志
            BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
            {
                Timestamp = DateTime.Now,
                Type = OperationType.DeleteSession,
                ActionName = "删除会话",
                Target = deletedName,
                Success = true
            });
            Debug.WriteLine($"[Delete] 已删除会话: {deletedName}");

            return newSelectedSession;
        }

        /// <summary>
        /// 重命名会话
        /// </summary>
        public void RenameSession(ChatSessionModel session, ObservableCollection<ChatSessionModel> sessions)
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
            if (sessions.Any(s => s != session && s.DisplayName == newName))
            {
                MessageBox.Show(
                    $"名称\u201C{newName}\u201D已存在，请使用其他名称！",
                    "重命名失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string oldName = session.DisplayName;
            session.DisplayName = newName;

            // 记录操作日志
            BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
            {
                Timestamp = DateTime.Now,
                Type = OperationType.RenameSession,
                ActionName = "重命名会话",
                Target = newName,
                Success = true,
                Details = $"原名：{oldName}"
            });

            Debug.WriteLine($"[Rename] 会话重命名为: {newName}");
        }

        /// <summary>
        /// 停止当前正在生成的消息
        /// </summary>
        public void StopMessage()
        {
            _currentChatService?.StopGeneration();
        }

        #endregion

        #region 持久化
        /// <summary>
        /// 加载所有会话
        /// </summary>
        public ObservableCollection<ChatSessionModel> LoadSessions()
        {
            var loaded = BeanFactory.GetBean<IAppSettingsRepository>().LoadSettings<ObservableCollection<ChatSessionModel>>();
            var sessions = loaded ?? new ObservableCollection<ChatSessionModel>();

            // 清除所有会话的选中状态，避免多个被选中
            foreach (var session in sessions)
            {
                session.IsSelected = false;
            }

            return sessions;
        }

        /// <summary>
        /// 保存所有会话
        /// </summary>
        public void SaveSessions(ObservableCollection<ChatSessionModel> sessions)
        {
            BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(sessions);
        }

        /// <summary>
        /// 保存所有设置（会话 + LLM参数 + 知识库）
        /// </summary>
        public void SaveAllSettings(ObservableCollection<ChatSessionModel> sessions)
        {
            SaveSessions(sessions);
            BeanFactory.GetBean<ILLMAdjustService>().Save();
            BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(KnowledgeBaseModel.Instance);
        }

        /// <summary>
        /// 对比知识库模型和 LLM 全局模型的快照，记录详细变更日志，并保存配置
        /// </summary>
        public void RecordAndSaveAllSettingsChanges()
        {
            // 1. 处理 LLM 全局模型
            BeanFactory.GetBean<ILLMAdjustService>().Save();

            // 2. 处理知识库模型
            var kbModel = KnowledgeBaseModel.Instance;
            string kbChanges = BeanFactory.GetBean<ISettingsChangeTracker>().GetChangesAndClear(kbModel);
            if (!string.IsNullOrEmpty(kbChanges))
            {
                string formattedChanges = kbChanges.Replace("; ", "\n");
                BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.EditKnowledgeBaseParams,
                    ActionName = "编辑知识库参数",
                    Target = "全局知识库设置",
                    Success = true,
                    Details = formattedChanges
                });
                Debug.WriteLine($"[MainWindowService] 知识库参数变更：\n{formattedChanges}");
            }

            // 3. 保存知识库配置到文件
            BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(kbModel);

            // 4. 重新捕获快照
            BeanFactory.GetBean<ISettingsChangeTracker>().CaptureSnapshot(kbModel);
        }

        #endregion

        #region SpaceAdjustView 变更记录

        /// <summary>
        /// 记录空间参数变更日志
        /// </summary>
        public void RecordSpaceAdjustChanges(ISpaceAdjustViewModel spaceAdjustVM)
        {
            if (spaceAdjustVM?.SpaceAdjustModel != null)
            {
                string changes = BeanFactory.GetBean<ISettingsChangeTracker>().GetChangesAndClear(spaceAdjustVM.SpaceAdjustModel);
                if (!string.IsNullOrEmpty(changes))
                {
                    var session = spaceAdjustVM.Session;
                    // 将分号替换为换行，美化输出
                    string formattedChanges = changes.Replace("; ", "\n");
                    BeanFactory.GetBean<ILoggingService>().WriteOperationLog(new OperationsLog
                    {
                        Timestamp = DateTime.Now,
                        Type = OperationType.EditSessionParams,
                        ActionName = "编辑会话参数",
                        Target = session.DisplayName,
                        Success = true,
                        Details = formattedChanges
                    });
                    Debug.WriteLine($"[RecordSpaceAdjustChanges] 参数变更:\n{formattedChanges}");
                }
            }
        }

        #endregion

        #region UI 辅助状态
        /// <summary>
        /// 根据当前时间获取问候语
        /// </summary>
        public string GetGreetingByTime()
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
