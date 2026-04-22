using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OllamaFramework.Models;
using OllamaSharp;
using OllamaSharp.Models;

namespace OllamaFramework.LLM
{
    /// <summary>
    /// 大语言模型对话输出服务（支持流式/非流式，可中途停止）
    /// </summary>
    public class ContentOut
    {
        private readonly IOllamaApiClient _ollamaClient;
        private readonly string _chatModel;
        private LLMParameters _defaultParameters;

        /// <summary>
        /// 获取或设置默认生成参数
        /// </summary>
        public LLMParameters DefaultParameters
        {
            get => _defaultParameters;
            set => _defaultParameters = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 初始化对话服务
        /// </summary>
        /// <param name="ollamaEndpoint">Ollama 服务地址</param>
        /// <param name="chatModel">对话模型名称，默认使用 qwen2.5:3b</param>
        public ContentOut(string ollamaEndpoint = "http://localhost:11434", string chatModel = null)
        {
            _ollamaClient = new OllamaApiClient(ollamaEndpoint);
            _chatModel = chatModel ?? LLMModel.CurrentChatModel;
            _defaultParameters = new LLMParameters();
        }

        /// <summary>
        /// 初始化对话服务（可注入已配置的 IOllamaApiClient）
        /// </summary>
        public ContentOut(IOllamaApiClient ollamaClient, string chatModel = null)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _chatModel = chatModel ?? LLMModel.CurrentChatModel;
            _defaultParameters = new LLMParameters();
        }

        /// <summary>
        /// 构建 GenerateRequest 对象
        /// </summary>
        private GenerateRequest BuildRequest(string prompt, LLMParameters parameters = null)
        {
            var p = parameters ?? _defaultParameters;

            var options = new RequestOptions
            {
                Temperature = p.Temperature,
                TopP = p.TopP,
                NumCtx = p.ContextSize,
                Seed = p.Seed,
                RepeatPenalty = p.RepeatPenalty
            };

            // 停止词设置到 Options.Stop 数组中
            if (p.StopWords != null && p.StopWords.Any())
            {
                options.Stop = p.StopWords.ToArray();
            }

            var request = new GenerateRequest
            {
                Model = _chatModel,
                Prompt = prompt,
                System = p.SystemPrompt,
                Options = options,
                Stream = p.Stream
            };

            return request;
        }

        /// <summary>
        /// 非流式生成回答
        /// </summary>
        /// <param name="prompt">用户提示词</param>
        /// <param name="parameters">自定义参数（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完整回答文本</returns>
        public async Task<string> GenerateAsync(string prompt, LLMParameters parameters = null, CancellationToken cancellationToken = default)
        {
            var p = parameters ?? _defaultParameters;
            var request = BuildRequest(prompt, p);
            request.Stream = false; // 强制非流式

            var fullResponse = new StringBuilder();
            try
            {
                // 即使 Stream = false，返回的仍是 IAsyncEnumerable，需用 await foreach 遍历
                await foreach (var chunk in _ollamaClient.GenerateAsync(request, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (!string.IsNullOrEmpty(chunk?.Response))
                    {
                        fullResponse.Append(chunk.Response);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return "[生成已中止]";
            }
            catch (Exception ex)
            {
                // 重新抛出包含模型名的异常
                throw new Exception($"对话模型 '{_chatModel}' 调用失败: {ex.Message}", ex);
            }

            // 如果没有获得任何响应，返回拒绝响应文本
            return fullResponse.Length > 0 ? fullResponse.ToString() : p.RefusalResponse;
        }

        /// <summary>
        /// 流式生成回答
        /// </summary>
        /// <param name="prompt">用户提示词</param>
        /// <param name="onChunkReceived">每收到一个 token 时的回调</param>
        /// <param name="parameters">自定义参数（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完整回答文本拼接结果</returns>
        public async Task<string> GenerateStreamingAsync(
            string prompt,
            Action<string> onChunkReceived,
            LLMParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            var p = parameters ?? _defaultParameters;
            var request = BuildRequest(prompt, p);
            request.Stream = true;

            var fullResponse = new StringBuilder();

            try
            {
                await foreach (var chunk in _ollamaClient.GenerateAsync(request, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var token = chunk?.Response;
                    if (!string.IsNullOrEmpty(token))
                    {
                        fullResponse.Append(token);
                        onChunkReceived?.Invoke(token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                onChunkReceived?.Invoke("\n[生成已中止]");
            }
            catch (Exception ex)
            {
                // 重新抛出包含模型名的异常
                throw new Exception($"对话模型 '{_chatModel}' 调用失败: {ex.Message}", ex);
            }

            return fullResponse.ToString();
        }

        /// <summary>
        /// 便捷方法：使用控制台逐字输出流式内容
        /// </summary>
        public async Task<string> GenerateStreamingToConsoleAsync(string prompt, LLMParameters parameters = null, CancellationToken cancellationToken = default)
        {
            return await GenerateStreamingAsync(prompt, token => Console.Write(token), parameters, cancellationToken);
        }

        /// <summary>
        /// 创建可中途取消的 CancellationTokenSource，便于外部停止生成
        /// </summary>
        public CancellationTokenSource CreateCancellationTokenSource()
        {
            return new CancellationTokenSource();
        }
    }
}