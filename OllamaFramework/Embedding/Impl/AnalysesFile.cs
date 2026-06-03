using OllamaFramework.Embedding;
using OllamaFramework.Models;
using OllamaSharp;
using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaFramework.Embedding.Impl
{
    public class AnalysesFile : IAnalysesFile
    {
        private readonly IOllamaApiClient _ollamaClient;
        private readonly string _embeddingModel;
        private readonly string _ollamaEndpoint;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        public string EmbeddingModelName => _embeddingModel;

        public AnalysesFile(string ollamaEndpoint = "http://localhost:11434", string embeddingModel = null)
        {
            _ollamaClient = new OllamaApiClient(ollamaEndpoint);
            _embeddingModel = embeddingModel ?? EmbeddingModel.CurrentEmbeddingModel;
            _ollamaEndpoint = ollamaEndpoint?.TrimEnd('/') ?? "http://localhost:11434";
        }

        public AnalysesFile(IOllamaApiClient ollamaClient, string embeddingModel = null)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _embeddingModel = embeddingModel ?? EmbeddingModel.CurrentEmbeddingModel;
            _ollamaEndpoint = "http://localhost:11434";
        }

        /// <summary>
        /// 读取文本文件内容
        /// </summary>
        public async Task<string> LoadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// 根据自定义分隔符（支持多个，按最长优先）将文本分割为块
        /// </summary>
        /// <param name="content">原始文本内容</param>
        /// <param name="separators">分隔符数组，例如 ["###", "####"]，会按长度降序尝试匹配</param>
        /// <param name="trimChunks">是否去除每个块前后的空白字符</param>
        /// <returns>分割后的文本块列表</returns>
        public List<string> SplitIntoChunks(string content, string[] separators, bool trimChunks = true)
        {
            if (string.IsNullOrEmpty(content) || separators == null || separators.Length == 0)
                return new List<string> { content };

            // 按长度降序排序，确保优先匹配更长的分隔符
            var sortedSeparators = separators.OrderByDescending(s => s.Length).ToArray();

            var chunks = new List<string>();
            int startIndex = 0;

            while (startIndex < content.Length)
            {
                // 查找最早出现的分隔符（在所有分隔符中取最小索引）
                int nextSeparatorIndex = -1;
                string matchedSeparator = null;

                foreach (var sep in sortedSeparators)
                {
                    int index = content.IndexOf(sep, startIndex, StringComparison.Ordinal);
                    if (index >= 0 && (nextSeparatorIndex == -1 || index < nextSeparatorIndex))
                    {
                        nextSeparatorIndex = index;
                        matchedSeparator = sep;
                    }
                }

                if (nextSeparatorIndex == -1)
                {
                    // 剩余部分作为最后一个块
                    string lastChunk = content.Substring(startIndex);
                    if (trimChunks) lastChunk = lastChunk.Trim();
                    if (!string.IsNullOrWhiteSpace(lastChunk))
                        chunks.Add(lastChunk);
                    break;
                }

                // 提取从 startIndex 到分隔符之前的内容
                string chunk = content.Substring(startIndex, nextSeparatorIndex - startIndex);
                if (trimChunks) chunk = chunk.Trim();
                if (!string.IsNullOrWhiteSpace(chunk))
                    chunks.Add(chunk);

                // 跳过已匹配的分隔符，继续处理后续内容
                startIndex = nextSeparatorIndex + matchedSeparator.Length;
            }

            return chunks;
        }

        /// <summary>
        /// 将分块信息输出到 Debug 日志（控制台）
        /// </summary>
        public void LogChunks(IEnumerable<string> chunks)
        {
            int index = 0;
            foreach (var chunk in chunks)
            {
                Debug.WriteLine($"--- 块 {++index} ---");
                Debug.WriteLine(chunk);
                Debug.WriteLine(new string('-', 40));
            }
            Console.WriteLine($"[AnalysesFile] 共生成 {chunks.Count()} 个文本块。");
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("输入文本不能为空", nameof(text));

            Debug.WriteLine($"[GenerateEmbeddingAsync] 开始生成嵌入向量，模型: {_embeddingModel}，文本长度: {text.Length}，BaseURL: {_ollamaEndpoint}");

            try
            {
                var request = new EmbedRequest
                {
                    Model = _embeddingModel,
                    Input = new List<string> { text }
                };

                var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
                Debug.WriteLine($"[GenerateEmbeddingAsync] OllamaSharp请求JSON: {requestJson}");

                var response = await _ollamaClient.EmbedAsync(request, cancellationToken);
                var embedding = response?.Embeddings?.FirstOrDefault();

                if (embedding == null || !embedding.Any())
                    throw new InvalidOperationException("嵌入向量生成失败：API返回空结果");

                Debug.WriteLine($"[GenerateEmbeddingAsync] OllamaSharp调用成功，向量维度: {embedding.Count()}");
                return embedding.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GenerateEmbeddingAsync] OllamaSharp调用失败，尝试HttpClient备用方案。异常类型: {ex.GetType().Name}，消息: {ex.Message}，内部异常: {ex.InnerException?.Message ?? "无"}");

                try
                {
                    return await GenerateEmbeddingViaHttpAsync(text, cancellationToken);
                }
                catch (Exception httpEx)
                {
                    Debug.WriteLine($"[GenerateEmbeddingAsync] HttpClient备用方案也失败。异常: {httpEx.Message}");
                    throw new Exception($"嵌入模型 '{_embeddingModel}' 调用失败(OllamaSharp: {ex.Message}; HttpClient: {httpEx.Message})", ex);
                }
            }
        }

        private async Task<float[]> GenerateEmbeddingViaHttpAsync(string text, CancellationToken cancellationToken)
        {
            var requestBody = $"{{\"model\":\"{_embeddingModel}\",\"input\":[{System.Text.Json.JsonSerializer.Serialize(text)}]}}";
            Debug.WriteLine($"[GenerateEmbeddingViaHttp] POST {_ollamaEndpoint}/api/embed，请求体: {requestBody}");

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_ollamaEndpoint}/api/embed", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[GenerateEmbeddingViaHttp] API返回错误，状态码: {response.StatusCode}，响应: {errorBody}");
                throw new Exception($"Ollama API返回 {(int)response.StatusCode}: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[GenerateEmbeddingViaHttp] API返回成功，响应长度: {responseBody.Length}");

            using (var doc = System.Text.Json.JsonDocument.Parse(responseBody))
            {
                var embeddingsArray = doc.RootElement.GetProperty("embeddings");
                if (embeddingsArray.GetArrayLength() == 0)
                    throw new Exception("API返回空嵌入数组");

                var firstEmbedding = embeddingsArray[0];
                var result = new List<float>();
                foreach (var item in firstEmbedding.EnumerateArray())
                {
                    result.Add(item.GetSingle());
                }

                Debug.WriteLine($"[GenerateEmbeddingViaHttp] 成功，向量维度: {result.Count}");
                return result.ToArray();
            }
        }

        /// <summary>
        /// 批量为多个文本块生成嵌入向量
        /// </summary>
        public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default)
        {
            var embeddings = new List<float[]>();
            foreach (var chunk in chunks)
            {
                var vector = await GenerateEmbeddingAsync(chunk, cancellationToken);
                embeddings.Add(vector);
            }
            return embeddings;
        }

        /// <summary>
        /// 计算两个向量的余弦相似度
        /// </summary>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("向量长度不一致");

            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            if (magA == 0 || magB == 0)
                return 0;

            return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        /// <summary>
        /// 完整处理流程：加载文件 → 分块 → 显示块 → 生成嵌入
        /// </summary>
        public async Task<List<float[]>> ProcessFileAsync(string filePath, string[] chunkSeparator)
        {
            // 1. 读取文件
            string content = await LoadFileAsync(filePath);

            // 2. 分块
            var chunks = SplitIntoChunks(content, chunkSeparator);

            // 3. 日志输出
            LogChunks(chunks);

            // 4. 生成嵌入向量
            var embeddings = await GenerateEmbeddingsAsync(chunks);

            return embeddings;
        }
    }
}