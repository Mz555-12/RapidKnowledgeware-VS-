using OllamaFramework.LLM;
using OllamaFramework.Models;
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
            Debug.WriteLine($"[ChatService] 使用 BaseURL: {defaultConfig.Default_BaseURL}, ChatLLM: {space.ChatLLM}");
            _llmService = new ContentOut(defaultConfig.Default_BaseURL, space.ChatLLM);
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
                    Content = userInput
                });
                Debug.WriteLine($"[ChatService] 已添加用户消息到会话: {_session.DisplayName}");
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
                Content = "",
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
                try
                {
                    // 直接获取全局单例，避免重复加载索引和初始化客户端
                    var ragService = KnowledgeBaseFunc.KnowledgeBaseService.RagServiceInstance;

                    if (ragService.IndexedChunkCount > 0)
                    {
                        Debug.WriteLine($"[ChatService] RAG 索引中有 {ragService.IndexedChunkCount} 个块，开始检索...");
                        // 提高相似度阈值到 0.5f，减少检索数量到 2，压缩提示词长度
                        var retrieved = await ragService.RetrieveAsync(userInput, topK: KnowledgeBaseModel.Instance.SearchQuantity, minSimilarity: KnowledgeBaseModel.Instance.IndexSimilarityThreshold);
                        Debug.WriteLine($"[ChatService] 检索到 {retrieved.Count} 个相关块，最高相似度: {retrieved.FirstOrDefault().Similarity}");

                        if (retrieved.Count > 0)
                        {
                            augmentedPrompt = ragService.BuildAugmentedPrompt(userInput, retrieved);
                            Debug.WriteLine($"[ChatService] 已构建增强提示词，长度: {augmentedPrompt.Length}");
                        }
                        else
                        {
                            Debug.WriteLine("[ChatService] 未检索到足够相关内容，使用原始问题");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[ChatService] RAG 索引为空，跳过检索");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ChatService] RAG 检索失败: {ex.Message}，使用原始问题");

                    // 检查是否为嵌入模型错误
                    string errorMsg = ex.ToString();
                    if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"嵌入模型 \"{KnowledgeBaseModel.Instance.CurrentEmbeddingName}\" 调用失败，请检查模型名称。\n\n错误详情: {ex.Message}",
                                            "嵌入模型错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                }
            }
            else
            {
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

                        // 解析 think 标签
                        string thinkContent = "";
                        string mainContent = fullResponse;

                        int thinkStart = fullResponse.IndexOf("<think>");
                        if (thinkStart != -1)
                        {
                            int thinkEnd = fullResponse.IndexOf("</think>", thinkStart);
                            if (thinkEnd != -1)
                            {
                                thinkContent = fullResponse.Substring(thinkStart + 7, thinkEnd - thinkStart - 7);
                                mainContent = fullResponse.Substring(0, thinkStart) + fullResponse.Substring(thinkEnd + 8);
                            }
                            else
                            {
                                thinkContent = fullResponse.Substring(thinkStart + 7);
                                mainContent = fullResponse.Substring(0, thinkStart);
                            }
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            aiMessage.Content = thinkContent;
                            aiMessage.Content2 = mainContent;

                            if (aiMessage.IsLoading && !string.IsNullOrEmpty(mainContent))
                            {
                                aiMessage.IsLoading = false;
                            }

                            onTokenReceived?.Invoke(token);
                            MainWindow.ScrollChatToEnd();
                        });

                        if (tokenCount % 50 == 0)
                        {
                            Debug.WriteLine($"[ChatService] 已接收 {tokenCount} 个token");
                        }
                    },
                    parameters: _llmService.DefaultParameters,
                    cancellationToken: _cts.Token);
                Debug.WriteLine($"[ChatService] GenerateStreamingAsync 完成，共接收 {tokenCount} 个token，最终回答长度: {fullResponse.Length}");
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine($"[ChatService] 生成被取消: {ex.Message}");
                aiMessage.Content2 += "\n[生成已中止]";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatService] LLM 生成失败: {ex.Message}");

                // 检查是否为对话模型错误
                string errorMsg = ex.ToString();
                if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
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
                Debug.WriteLine("[ChatService] SendMessageAsync 结束");
            }
        }

        /// <summary>
        /// 停止当前正在生成的 AI 响应
        /// </summary>
        public void StopGeneration()
        {
            _cts?.Cancel();
        }
    }
}