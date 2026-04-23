using Microsoft.Extensions.AI;
using OllamaFramework.LLM;
using OllamaFramework.Models;
using RapidKnowledgeware.Functions.DebugFunc;
using RapidKnowledgeware.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Functions.ChatFunc
{
    /// <summary>
    /// 聊天服务类，负责处理消息发送、LLM 调用、RAG 检索和流式响应
    /// </summary>
    public class ChatService
    {
        private readonly ChatSessionModel _session;
        private ContentOut _llmService;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _ragCts;

        /// <summary>
        /// 初始化聊天服务
        /// </summary>
        /// <param name="session">关联的聊天会话</param>
        public ChatService(ChatSessionModel session)
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
            var defaultConfig = LLMAdjustFunc.LLMAdjustService.Current;

            // 根据是否开启深度思考选择模型
            string modelToUse = space.ChatLLM;
            bool isDeepThinkingEnabled = LLMAdjustFunc.LLMAdjustService.Current.GlobalIsDeepThinking;
            if (isDeepThinkingEnabled)
            {
                if (string.IsNullOrWhiteSpace(space.DeepThinkingLLM))
                {
                    DebugService.Error("深度思考模式已开启，但未配置深度思考模型。");
                    throw new InvalidOperationException("深度思考模型未配置，请先在空间参数中设置 DeepThinkingLLM。");
                }
                modelToUse = space.DeepThinkingLLM;
                DebugService.Info($"深度思考模式启用");
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
            var space = _session.SpaceParameters;
            bool isDeepThinkingEnabled = LLMAdjustFunc.LLMAdjustService.Current.GlobalIsDeepThinking;
            string actualModel = isDeepThinkingEnabled ? (space.DeepThinkingLLM ?? space.ChatLLM) : space.ChatLLM;

            // 提前验证模型配置，避免发送后才失败
            if (LLMAdjustFunc.LLMAdjustService.Current.GlobalIsDeepThinking && string.IsNullOrWhiteSpace(space.DeepThinkingLLM))
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
                DebugService.Info($"############### {_session.DisplayName} 聊天开始 #################");
                DebugService.Info($"模型为：{actualModel}");
                DebugService.Info($"{_session.DisplayName} 输入: {userInput}");

                MainWindow.ScrollChatToEnd();
                // 立即刷新输入框布局（居中→底部）
                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin?.DataContext is RapidKnowledgeware.ViewModels.MainWindowModel vm)
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
            string augmentedPrompt = userInput;
            // 仅当会话开启知识库对接时才执行检索
            if (_session.SpaceParameters.IsLinkKnowledgeBase)
            {
                _ragCts = new CancellationTokenSource();
                try
                {
                    // 直接获取全局单例，避免重复加载索引和初始化客户端
                    var ragService = KnowledgeBaseFunc.KnowledgeBaseService.RagServiceInstance;

                    if (ragService.IndexedChunkCount > 0)
                    {
                        DebugService.Info($"RAG 索引中有 {ragService.IndexedChunkCount} 个块，开始检索...");
                        Debug.WriteLine($"[ChatService] RAG 索引中有 {ragService.IndexedChunkCount} 个块，开始检索...");


                        // 使用会话级知识库参数
                        int searchQuantity = _session.SpaceParameters.SearchQuantity;
                        float similarityThreshold = _session.SpaceParameters.IndexSimilarityThreshold;
                        DebugService.Info($"当前相似度阈值为：{similarityThreshold}，最大可检索：{searchQuantity} 个块");
                        var retrieved = await ragService.RetrieveAsync(userInput, topK: searchQuantity, minSimilarity: similarityThreshold,
                cancellationToken: _ragCts.Token);
                        // 将检索结果拼接成一条完整的调试信息
                        var sb = new StringBuilder();
                        

                        if (retrieved.Count > 0)
                        {
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
                            DebugService.Info(sb.ToString());


                            DebugService.Info($"找到了 {retrieved.Count} 个块");
                            // 直接使用全局默认上下文大小
                            int contextSize = LLMAdjustFunc.LLMAdjustService.Current.Default_ContextSize;
                            augmentedPrompt = PromptTruncationService.BuildTruncatedPrompt(userInput, retrieved, contextSize);
                            DebugService.Info($"已构建增强提示词（上下文窗口：{contextSize}字符），实际长度: {augmentedPrompt.Length}");
                            Debug.WriteLine($"[ChatService] 已构建增强提示词，长度: {augmentedPrompt.Length}");

                            

                        }
                        else
                        {
                            DebugService.Info("未检索到足够相关内容");
                            Debug.WriteLine("[ChatService] 未检索到足够相关内容，使用原始问题");
                        }
                    }
                    else
                    {
                        DebugService.Info("RAG 索引为空，跳过检索");
                        Debug.WriteLine("[ChatService] RAG 索引为空，跳过检索");
                    }
                }
                catch (Exception ex)
                {
                    DebugService.Error($"RAG 检索失败,跳过检索：", ex);
                    Debug.WriteLine($"[ChatService] RAG 检索失败: {ex.Message}，使用原始问题");

                    // 检查是否为嵌入模型错误
                    string errorMsg = ex.ToString();
                    if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {

                            DebugService.Error($"嵌入模型 \"{KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName}\" 调用失败，请检查模型名称。\n\n错误详情：",ex);
                            MessageBox.Show($"嵌入模型 \"{KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName}\" 调用失败，请检查模型名称。\n\n错误详情: {ex.Message}",
                                            "嵌入模型错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                }
            }
            else
            {
                DebugService.Info($"当前会话未对接知识库，跳过RAG检索");
                Debug.WriteLine("[ChatService] 当前会话未对接知识库，跳过RAG检索");
            }
            // ---------- RAG 检索结束 ----------

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
                DebugService.Info($"AI回复：{aiMessage.Content2}");
                DebugService.Info($"完成，共接收 {tokenCount} 个token，最终回答长度: {fullResponse.Length}");
                Debug.WriteLine($"[ChatService] GenerateStreamingAsync 完成，共接收 {tokenCount} 个token，最终回答长度: {fullResponse.Length}");
            }
            catch (OperationCanceledException ex)
            {
                DebugService.Info("LLM 生成被用户取消");
                aiMessage.Content2 = "[生成已中止]";
                Debug.WriteLine($"[ChatService] 生成被取消: {ex.Message}");
                throw; // 重新抛出，让外层 catch 处理
            }
            catch (Exception ex)
            {
                DebugService.Error("生成失败：",ex);
                Debug.WriteLine($"[ChatService] LLM 生成失败: {ex.Message}");

                // 检查是否为对话模型错误
                string errorMsg = ex.ToString();
                if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        DebugService.Error($"对话模型 \"{_session.SpaceParameters.ChatLLM}\" 调用失败，请检查模型名称。\n\n错误详情:", ex);
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
                        DebugService.Error($"关闭加载状态，Content2长度: {aiMessage.Content2?.Length ?? 0}");
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
                await LoggingFunc.LoggingService.WriteChatLogAsync(chatLog);

                _ragCts = null;  // 检索完成后释放

                DebugService.Info($"############### 本轮 {_session.DisplayName} 聊天结束 #################");
                Debug.WriteLine("[ChatService] SendMessageAsync 结束");
                
            }
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