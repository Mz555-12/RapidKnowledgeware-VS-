using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OllamaFramework.Models;
using OllamaSharp;
using OllamaSharp.Models;

namespace OllamaFramework.Embedding
{
    /// <summary>
    /// 文件分析与嵌入向量生成服务
    /// </summary>
    public class AnalysesFile
    {
        private readonly IOllamaApiClient _ollamaClient;
        private readonly string _embeddingModel;

        public string EmbeddingModelName => _embeddingModel;

        /// <summary>
        /// 初始化分析服务
        /// </summary>
        /// <param name="ollamaEndpoint">Ollama 服务地址，默认 http://localhost:11434</param>
        /// <param name="embeddingModel">嵌入模型名称，默认使用 bge-m3:567m</param>
        public AnalysesFile(string ollamaEndpoint = "http://localhost:11434", string embeddingModel = null)
        {
            _ollamaClient = new OllamaApiClient(ollamaEndpoint);
            _embeddingModel = embeddingModel ?? EmbeddingModel.CurrentEmbeddingModel;
        }

        /// <summary>
        /// 初始化分析服务（可注入已配置的 IOllamaApiClient）
        /// </summary>
        public AnalysesFile(IOllamaApiClient ollamaClient, string embeddingModel = null)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _embeddingModel = embeddingModel ?? EmbeddingModel.CurrentEmbeddingModel;
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

        /// <summary>
        /// 为单个文本生成嵌入向量
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("输入文本不能为空", nameof(text));

            var request = new EmbedRequest
            {
                Model = _embeddingModel,
                Input = new List<string> { text }
            };
            try
            {
                var response = await _ollamaClient.EmbedAsync(request);
                var embedding = response?.Embeddings?.FirstOrDefault();

                if (embedding == null || !embedding.Any())
                    throw new InvalidOperationException("嵌入向量生成失败");

                return embedding.ToArray();
            }
            catch (Exception ex)
            {

                // 重新抛出包含模型名的异常，便于上层识别
                throw new Exception($"嵌入模型 '{_embeddingModel}' 调用失败: {ex.Message}", ex);
            }


        }

        /// <summary>
        /// 批量为多个文本块生成嵌入向量
        /// </summary>
        public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> chunks)
        {
            var embeddings = new List<float[]>();
            foreach (var chunk in chunks)
            {
                var vector = await GenerateEmbeddingAsync(chunk);
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