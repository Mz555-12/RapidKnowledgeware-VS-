using OllamaFramework.LLM;
using OllamaFramework.Models;
using RapidKnowledgeware.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Functions.ChatFunc
{
    public class ChatService
    {
        private readonly ChatSessionModel _session;
        private ContentOut _llmService;
        private CancellationTokenSource _cts;

        public ChatService(ChatSessionModel session)
        {
            _session = session;
        }

        private void EnsureLLMService()
        {
            var space = _session.SpaceParameters;
            var defaultConfig = LLMAdjustFunc.LLMAdjustService.Current;
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
        }

        public async Task SendMessageAsync(string userInput, Action<string> onTokenReceived)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _session.Messages.Add(new ChatMessageModel
                {
                    IsUserMessage = true,
                    Content = userInput
                });
            });

            var aiMessage = new ChatMessageModel
            {
                IsUserMessage = false,
                IsLoading = true,
                Content = "",
                Content2 = ""
            };
            Application.Current.Dispatcher.Invoke(() => _session.Messages.Add(aiMessage));

            EnsureLLMService();
            _cts = new CancellationTokenSource();

            string fullThink = "";
            string fullAnswer = "";
            bool thinkingPhase = true; // 实际项目中可根据模型输出内容切换

            try
            {
                await _llmService.GenerateStreamingAsync(
                    userInput,
                    token =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (thinkingPhase)
                            {
                                fullThink += token;
                                aiMessage.Content = fullThink;
                            }
                            else
                            {
                                fullAnswer += token;
                                aiMessage.Content2 = fullAnswer;
                            }
                            onTokenReceived?.Invoke(token);
                        });
                        // 这里可以加检测，例如遇到 "" 或 "" 等标记切换阶段
                    },
                    parameters: _llmService.DefaultParameters,
                    cancellationToken: _cts.Token);
            }
            catch (OperationCanceledException)
            {
                aiMessage.Content2 += "\n[生成已中止]";
            }
            catch (Exception ex)
            {
                aiMessage.Content2 = $"[错误: {ex.Message}]";
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => aiMessage.IsLoading = false);
                _cts = null;
            }
        }

        public void StopGeneration()
        {
            _cts?.Cancel();
        }
    }
}