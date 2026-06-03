using Microsoft.Extensions.AI;
using OllamaFramework.LLM;
using OllamaFramework.LLM.Impl;
using OllamaFramework.Models;
using OllamaFramework.Rag;
using IOC;
using IOC.Annotations;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels.Impl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// 聊天服务类，负责处理消息发送、LLM 调用、RAG 检索和流式响应
    /// </summary>
    [Service]
    public class ChatService : IChatService
    {
        private ChatSessionModel _session;
        private IContentOut _llmService;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _ragCts;

        public ChatService()
        {
        }

        public void Initialize(ChatSessionModel session)
        {
            _session = session;
        }

        /// <summary>
        /// 确保 LLM 服务已初始化，使用会话专属参数和全局默认配置
        /// </summary>
        private void EnsureLLMService()
        {
            Debug.WriteLine($"[ChatService] EnsureLLMService 开始，会话: {_session.DisplayName}");
            var space = _session.SpaceParameters;
            var defaultConfig = BeanFactory.GetBean<ILLMAdjustService>().Current;

            // 根据是否开启深度思考选择模型
            string modelToUse = space.ChatLLM;
            bool isDeepThinkingEnabled = BeanFactory.GetBean<ILLMAdjustService>().Current.GlobalIsDeepThinking;
            if (isDeepThinkingEnabled)
            {
                if (string.IsNullOrWhiteSpace(space.DeepThinkingLLM))
                {
                    BeanFactory.GetBean<IDebugService>().Error("深度思考模式已开启，但未配置深度思考模型。");
                    throw new InvalidOperationException("深度思考模型未配置，请先在空间参数中设置 DeepThinkingLLM。");
                }
                modelToUse = space.DeepThinkingLLM;
                BeanFactory.GetBean<IDebugService>().Info($"深度思考模式启用");
            }

            Debug.WriteLine($"[ChatService] 使用 BaseURL: {defaultConfig.Default_BaseURL}, 模型: {modelToUse}");
            _llmService = new ContentOut(defaultConfig.Default_BaseURL, modelToUse);
            _llmService.DefaultParameters = new LLMParameters
            {
                Temperature = space.Temperature,
                TopP = space.TopP,
                RepeatPenalty = space.RepeatPenalty,
                ContextSize = defaultConfig.Default_ContextSize,
                SystemPrompt = space.SystemPrompt,
                RefusalResponse = space.QueryRefusalResponse,
                Stream = true
            };
            Debug.WriteLine("[ChatService] LLM服务初始化完成");
        }

        /// <summary>
        /// 发送用户消息并获取 AI 流式响应（支持 RAG 检索增强）
        /// </summary>
        /// <param name="userInput">用户输入内容</param>
        /// <param name="onTokenReceived">每收到一个 token 时的回调</param>
        public async Task SendMessageAsync(string userInput, Action<string> onTokenReceived)
        {
            // 用于存储检索结果（稍后统一截断）
            List<(DocumentChunk Chunk, float Similarity)> retrievedChunks = null;
            var space = _session.SpaceParameters;
            bool isDeepThinkingEnabled = BeanFactory.GetBean<ILLMAdjustService>().Current.GlobalIsDeepThinking;
            string actualModel = isDeepThinkingEnabled ? (space.DeepThinkingLLM ?? space.ChatLLM) : space.ChatLLM;

            // 提前验证模型配置，避免发送后才失败
            if (BeanFactory.GetBean<ILLMAdjustService>().Current.GlobalIsDeepThinking && string.IsNullOrWhiteSpace(space.DeepThinkingLLM))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("深度思考模式已开启，但未配置深度思考模型。\n请先在空间参数中填写 DeepThinkingLLM。",
                                    "模型未配置", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            // 去除前后空白字符，保留中间内容
            userInput = userInput?.Trim();


            Debug.WriteLine($"[ChatService] SendMessageAsync 开始，输入: {userInput}");
            if (string.IsNullOrWhiteSpace(userInput))
            {
                Debug.WriteLine("[ChatService] 输入为空，退出");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _session.Messages.Add(new ChatMessageModel
                {
                    IsUserMessage = true,
                    UserContent = userInput
                });
                Debug.WriteLine($"[ChatService] 已添加用户消息到会话: {_session.DisplayName}");
                BeanFactory.GetBean<IDebugService>().Info($"############### {_session.DisplayName} 聊天开始 #################");
                BeanFactory.GetBean<IDebugService>().Info($"模型为：{actualModel}");
                BeanFactory.GetBean<IDebugService>().Info($"{_session.DisplayName} 输入: {userInput}");

                MainWindow.ScrollChatToEnd();
                // 立即刷新输入框布局（居中→底部）
                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin?.DataContext is MainWindowModel vm)
                {
                    vm.RefreshUIAssistProperties();
                }
            });

            var aiMessage = new ChatMessageModel
            {
                IsUserMessage = false,
                IsLoading = true,
                ThinkingContent = "",
                Content2 = ""
            };
            Application.Current.Dispatcher.Invoke(() =>
            {
                _session.Messages.Add(aiMessage);
                Debug.WriteLine("[ChatService] 已添加AI消息占位符");
            });

            // ---------- RAG 检索（工业级优化版）----------
            string kbContext = string.Empty;
            if (_session.SpaceParameters.IsLinkKnowledgeBase)
            {
                _ragCts = new CancellationTokenSource();
                try
                {
                    var ragService = BeanFactory.GetBean<IKnowledgeBaseService>().RagServiceInstance;
                    if (ragService.IndexedChunkCount > 0)
                    {
                        BeanFactory.GetBean<IDebugService>().Info($"RAG 索引中有 {ragService.IndexedChunkCount} 个块，开始检索...");
                        int searchQuantity = _session.SpaceParameters.SearchQuantity;
                        float similarityThreshold = _session.SpaceParameters.IndexSimilarityThreshold;
                        var retrieved = await ragService.RetrieveAsync(userInput, topK: searchQuantity, minSimilarity: similarityThreshold, cancellationToken: _ragCts.Token);
                        if (retrieved.Count > 0)
                        {
                            // 检索结果日志保留
                            var sb = new StringBuilder();
                            sb.AppendLine("############ 索引到的块 ################");
                            for (int i = 0; i < retrieved.Count; i++)
                            {
                                var item = retrieved[i];
                                string source = item.Chunk.Metadata.TryGetValue("source", out object src) ? src.ToString() : "未知来源";
                                string chunkIndex = item.Chunk.Metadata.TryGetValue("chunk_index", out object idx) ? idx.ToString() : "?";
                                sb.AppendLine($"\n  [{i + 1}] 相似度：{item.Similarity:F4}");
                                sb.AppendLine($"     来源：{System.IO.Path.GetFileName(source)}");
                                sb.AppendLine($"     块为：{chunkIndex}\n");
                            }
                            sb.AppendLine("######################################");
                            BeanFactory.GetBean<IDebugService>().Info(sb.ToString());

                            retrievedChunks = retrieved;   // 留待最终组装时使用
                        }
                        else
                        {
                            BeanFactory.GetBean<IDebugService>().Info("未检索到足够相关内容");
                        }
                    }
                    else
                    {
                        BeanFactory.GetBean<IDebugService>().Info("RAG 索引为空，跳过检索");
                    }
                }
                catch (Exception ex)
                {
                    BeanFactory.GetBean<IDebugService>().Error($"RAG 检索失败,跳过检索：", ex);
                    // 模型错误提示保持不变
                }
            }
            else
            {
                BeanFactory.GetBean<IDebugService>().Info($"当前会话未对接知识库，跳过RAG检索");
            }
            // ---------- RAG 检索结束 ----------

            // ---------- 组装最终提示词（知识库 + 历史记录）----------
            int contextSizeFinal = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_ContextSize;
            float historyPercent = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_ChatHistoryPercentage;
            int maxHistoryLen = (int)(contextSizeFinal * historyPercent);
            int historyRounds = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_ChatHistoryMemory;

            BeanFactory.GetBean<IDebugService>().Info($"上下文记忆总长度={contextSizeFinal}, 历史占比={historyPercent}, 最大历史记忆长度={maxHistoryLen}, 获取的知识库记忆长度暂未计算");

            string systemPrompt = _session.SpaceParameters.SystemPrompt ?? "You are a helpful assistant.";
            string promptTemplate = systemPrompt + "\n\n" +
                    "上下文：\n{context}\n\n" +
                    "{history}" +
                    "问题：{question}\n" +
                    "回答：";

            string templateWithoutPlaceholders = promptTemplate.Replace("{context}", "").Replace("{history}", "").Replace("{question}", "");
            int fixedLength = templateWithoutPlaceholders.Length + userInput.Length;

            // ---- 历史记录提取 ----
            string historyText = null;
            int historyRoundsUsed = 0;
            int historyTotalLength = 0;

            if (historyRounds > 0)
            {
                int remainingForAll = contextSizeFinal - fixedLength;
                int historyBudget = Math.Min(remainingForAll, maxHistoryLen);
                int firstRoundBudget = historyBudget;

                // 最近一轮放宽检查
                var lastPair = GetLastCompletedPair(_session.Messages);
                if (lastPair != null)
                {
                    int lastPairLen = lastPair.Value.User.Length + lastPair.Value.AI.Length;
                    if (lastPairLen > historyBudget)
                    {
                        int relaxedBudget = Math.Min(remainingForAll, (int)(contextSizeFinal * 0.475));
                        firstRoundBudget = Math.Max(relaxedBudget, historyBudget);
                        BeanFactory.GetBean<IDebugService>().Info($"[放宽条件] lastPairLen={lastPairLen} 超出标准预算 {historyBudget}，放宽首轮预算至 {firstRoundBudget}");
                    }
                }

                var historyResult = BuildChatHistory(_session.Messages, firstRoundBudget, historyBudget, historyRounds);
                if (historyResult != null)
                {
                    historyText = historyResult.Value.historyText;
                    historyRoundsUsed = historyResult.Value.rounds;
                    historyTotalLength = historyResult.Value.totalLength;
                }
            }

            // ---- 知识库截断（动态使用剩余空间）----
            int kbAvailable = contextSizeFinal - fixedLength - (historyText?.Length ?? 0);
            kbContext = string.Empty;
            if (retrievedChunks != null && retrievedChunks.Count > 0)
            {
                kbContext = BeanFactory.GetBean<IPromptTruncationService>().BuildTruncatedContext(retrievedChunks, kbAvailable);
            }

            // ---- 最终拼接 ----
            string augmentedPrompt = promptTemplate
                .Replace("{context}", kbContext)
                .Replace("{history}", historyText != null ? historyText + Environment.NewLine : "")
                .Replace("{question}", userInput);

            BeanFactory.GetBean<IDebugService>().Info($"最终提示词长度: {augmentedPrompt.Length} | 知识库长度: {kbContext.Length} | 历史附加: {(historyText != null ? "Yes" : "No")} | 历史长度: {historyRoundsUsed}轮 | 历史记忆为:{historyTotalLength}字 | 历史轮数限制: {historyRounds}");



            //########################################

            Debug.WriteLine("[ChatService] 调用 EnsureLLMService");
            EnsureLLMService();
            _cts = new CancellationTokenSource();
            Debug.WriteLine("[ChatService] 已创建 CancellationTokenSource");

            string fullResponse = "";
            int tokenCount = 0;

            try
            {

                Debug.WriteLine($"[ChatService] 开始调用 GenerateStreamingAsync，模型: {_llmService.DefaultParameters}");
                await _llmService.GenerateStreamingAsync(
                    augmentedPrompt,   // 使用增强后的提示词
                    token =>
                    {
                        tokenCount++;
                        fullResponse += token;

                        // 解析 think 标签（兼容多种标记）
                        string thinkContent = "";
                        string mainContent = fullResponse;


                        // 支持多种思考标记（XML 与纯文本格式）
                        string[] startTags = { "<think>", "<thinking>", "<思考>", "Thinking..." };
                        string[] endTags = { "</think>", "</thinking>", "</思考>", "...done thinking." };

                        int thinkStart = -1;
                        int thinkEnd = -1;          // ← 提升到这里声明
                        int selectedTagIndex = -1;
                        for (int i = 0; i < startTags.Length; i++)
                        {
                            int idx = fullResponse.IndexOf(startTags[i]);
                            if (idx != -1)
                            {
                                thinkStart = idx;
                                selectedTagIndex = i;
                                break;
                            }
                        }

                        if (thinkStart != -1)
                        {
                            thinkEnd = fullResponse.IndexOf(endTags[selectedTagIndex], thinkStart + startTags[selectedTagIndex].Length);  // ← 直接赋值
                            if (thinkEnd != -1)
                            {
                                int thinkContentStart = thinkStart + startTags[selectedTagIndex].Length;
                                thinkContent = fullResponse.Substring(thinkContentStart, thinkEnd - thinkContentStart);
                                mainContent = fullResponse.Substring(0, thinkStart) + fullResponse.Substring(thinkEnd + endTags[selectedTagIndex].Length);
                            }
                            else
                            {
                                thinkContent = fullResponse.Substring(thinkStart + startTags[selectedTagIndex].Length);
                                mainContent = fullResponse.Substring(0, thinkStart);
                            }
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            aiMessage.ThinkingContent = thinkContent;
                            aiMessage.Content2 = mainContent;

                            // 控制思考区域自动展开/折叠
                            bool thinkClosed = thinkEnd != -1;  // 现在可以访问了
                            if (!string.IsNullOrEmpty(thinkContent) && !thinkClosed)
                            {
                                // 思考进行中 → 展开
                                aiMessage.IsThinkingExpanded = true;
                            }
                            else if (thinkClosed || !string.IsNullOrEmpty(mainContent))
                            {
                                // 思考已结束或没有思考内容 → 折叠
                                aiMessage.IsThinkingExpanded = false;
                            }

                            if (aiMessage.IsLoading && (!string.IsNullOrEmpty(thinkContent) || !string.IsNullOrEmpty(mainContent)))
                            {
                                aiMessage.IsLoading = false;
                            }

                            onTokenReceived?.Invoke(token);
                            MainWindow.ScrollChatToEnd();
                        });
                    },
                    parameters: _llmService.DefaultParameters,
                    cancellationToken: _cts.Token);
                BeanFactory.GetBean<IDebugService>().Info($"AI回复：{aiMessage.Content2}");
                BeanFactory.GetBean<IDebugService>().Info($"完成，共接收 {tokenCount} 个token，最终回答长度: {fullResponse.Length}");
                Debug.WriteLine($"[ChatService] GenerateStreamingAsync 完成，共接收 {tokenCount} 个token，最终回答长度: {fullResponse.Length}");
            }
            catch (OperationCanceledException ex)
            {
                BeanFactory.GetBean<IDebugService>().Info("LLM 生成被用户取消");
                aiMessage.Content2 = "[生成已中止]";
                Debug.WriteLine($"[ChatService] 生成被取消: {ex.Message}");
                throw; // 重新抛出，让外层 catch 处理
            }
            catch (Exception ex)
            {
                BeanFactory.GetBean<IDebugService>().Error("生成失败：", ex);
                Debug.WriteLine($"[ChatService] LLM 生成失败: {ex.Message}");

                // 检查是否为对话模型错误
                string errorMsg = ex.ToString();
                if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        BeanFactory.GetBean<IDebugService>().Error($"对话模型 \"{_session.SpaceParameters.ChatLLM}\" 调用失败，请检查模型名称。\n\n错误详情:", ex);
                        MessageBox.Show($"对话模型 \"{_session.SpaceParameters.ChatLLM}\" 调用失败，请检查模型名称。\n\n错误详情: {ex.Message}",
                                        "对话模型错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                else
                {
                    // 非模型错误仍显示在聊天界面
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        aiMessage.Content2 = $"[错误: {ex.Message}]";
                    });
                }
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (aiMessage.IsLoading)
                    {
                        aiMessage.IsLoading = false;
                        BeanFactory.GetBean<IDebugService>().Error($"关闭加载状态，Content2长度: {aiMessage.Content2?.Length ?? 0}");
                        Debug.WriteLine($"[ChatService] finally中关闭加载状态，Content2长度: {aiMessage.Content2?.Length ?? 0}");
                    }
                    MainWindow.ScrollChatToEnd();
                });
                _cts = null;

                // 记录聊天日志
                var chatLog = new ChatLog
                {
                    Timestamp = DateTime.Now,
                    SessionId = _session.SessionId,
                    SessionName = _session.DisplayName,
                    UserMessage = userInput,
                    AIResponse = fullResponse,
                    ModelUsed = _session.SpaceParameters.ChatLLM
                };
                await BeanFactory.GetBean<ILoggingService>().WriteChatLogAsync(chatLog);

                _ragCts = null;  // 检索完成后释放

                BeanFactory.GetBean<IDebugService>().Info($"############### 本轮 {_session.DisplayName} 聊天结束 #################");
                Debug.WriteLine("[ChatService] SendMessageAsync 结束");

            }
        }



        /// <summary>
        /// 从消息集合中提取最近的历史对话
        /// </summary>
        /// <param name="firstRoundBudget">最近一轮对话允许的最大纯文本长度</param>
        /// <param name="standardBudget">后续每轮对话允许的最大累计纯文本长度</param>
        /// <param name="maxRounds">最多保留的对话轮数</param>
        /// <summary>
        /// 提取最近的历史对话，返回格式化文本、实际轮数和纯文本总长度
        /// </summary>
        private static (string historyText, int rounds, int totalLength)? BuildChatHistory(
            ObservableCollection<ChatMessageModel> messages,
            int firstRoundBudget,
            int standardBudget,
            int maxRounds)
        {
            if (firstRoundBudget <= 0 || standardBudget <= 0 || maxRounds <= 0)
                return null;

            var completedPairs = new List<(string User, string AI)>();
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].IsUserMessage && !string.IsNullOrWhiteSpace(messages[i].UserContent))
                {
                    if (i + 1 < messages.Count && !messages[i + 1].IsUserMessage && !messages[i + 1].IsLoading)
                    {
                        string aiContent = messages[i + 1].Content2;
                        if (!string.IsNullOrWhiteSpace(aiContent))
                            completedPairs.Add((messages[i].UserContent, aiContent));
                        i++;
                    }
                }
            }

            if (completedPairs.Count == 0)
                return null;

            int totalPlainLength = 0;
            var selectedPairs = new List<(string User, string AI)>();

            for (int idx = completedPairs.Count - 1; idx >= 0 && selectedPairs.Count < maxRounds; idx--)
            {
                var pair = completedPairs[idx];
                int pairLength = pair.User.Length + pair.AI.Length;
                int budget = (selectedPairs.Count == 0) ? firstRoundBudget : standardBudget;

                Debug.WriteLine($"[历史轮次判断] 轮号:{idx + 1}, 长度:{pairLength}, 累计:{totalPlainLength}, 预算:{budget}, 是否首轮:{selectedPairs.Count == 0}");

                if (totalPlainLength + pairLength > budget)
                {
                    Debug.WriteLine($"[历史轮次丢弃] 轮号:{idx + 1} 超出预算，整轮丢弃且不再尝试更早轮次");
                    break;
                }
                selectedPairs.Insert(0, pair);
                totalPlainLength += pairLength;

                Debug.WriteLine($"[历史轮次加入] 轮号:{idx + 1} 已加入，累计纯文本长度:{totalPlainLength}");
            }

            if (selectedPairs.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine("历史对话：");
            foreach (var (user, ai) in selectedPairs)
            {
                sb.AppendLine($"用户：{user}");
                sb.AppendLine($"AI：{ai}");
                sb.AppendLine();
            }

            return (sb.ToString().TrimEnd(), selectedPairs.Count, totalPlainLength);
        }


        /// <summary>
        /// 获取最近一个完成的用户-AI消息对（用户消息及其后续AI回答）
        /// </summary>
        private static (string User, string AI)? GetLastCompletedPair(ObservableCollection<ChatMessageModel> messages)
        {
            for (int i = messages.Count - 1; i >= 1; i--)
            {
                if (!messages[i].IsUserMessage && !messages[i].IsLoading &&
                    messages[i - 1].IsUserMessage && !string.IsNullOrWhiteSpace(messages[i - 1].UserContent))
                {
                    string aiContent = messages[i].Content2;
                    if (!string.IsNullOrWhiteSpace(aiContent))
                        return (messages[i - 1].UserContent, aiContent);
                }
            }
            return null;
        }

        /// <summary>
        /// 停止当前正在生成的 AI 响应
        /// </summary>
        public void StopGeneration()
        {
            _ragCts?.Cancel();
            _cts?.Cancel();
        }
    }
}