using Newtonsoft.Json;
using OllamaFramework.Embedding;
using OllamaFramework.LLM;
using OllamaFramework.Models;
using OllamaFramework.Rag;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static OllamaFramework.Models.Serializable;


/// <summary>
/// 检索增强生成（RAG）服务
/// </summary>
public class RagService
{
    private readonly AnalysesFile _embeddingService;
    private readonly ContentOut _llmService;
    private readonly List<DocumentChunk> _indexedChunks = new List<DocumentChunk>();

    /// <summary>
    /// 当前已索引的文档块数量
    /// </summary>
    public int IndexedChunkCount => _indexedChunks.Count;

    /// <summary>
    /// 默认的 LLM 生成参数，用于所有查询（除非单独覆盖）
    /// </summary>
    public LLMParameters DefaultLLMParameters { get; set; } = new LLMParameters();




    private static readonly string IndexFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json", "rag_index.json");


    /// <summary>
    /// 初始化 RAG 服务
    /// </summary>
    /// <param name="ollamaEndpoint">Ollama 服务地址</param>
    /// <param name="embeddingModel">嵌入模型名称（可选）</param>
    /// <param name="chatModel">对话模型名称（可选）</param>
    public RagService(string ollamaEndpoint = "http://localhost:11434",
                      string embeddingModel = null,
                      string chatModel = null)
    {
        _embeddingService = new AnalysesFile(ollamaEndpoint, embeddingModel);
        _llmService = new ContentOut(ollamaEndpoint, chatModel);
    }

    /// <summary>
    /// 上传并索引文档（支持多级分隔符）
    /// </summary>
    /// <param name="filePath">文档路径</param>
    /// <param name="chunkSeparators">分块分隔符数组（按最长优先匹配）</param>
    /// <param name="clearExisting">是否清空现有索引</param>
    /// <returns>生成的文档块数量</returns>
    public async Task<int> IndexDocumentAsync(string filePath, string[] chunkSeparators, bool clearExisting = false)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在: {filePath}");

        // 读取文件内容
        string content = await _embeddingService.LoadFileAsync(filePath);

        // 分块
        var chunks = _embeddingService.SplitIntoChunks(content, chunkSeparators);

        // 分块日志
        _embeddingService.LogChunks(chunks);

        // 生成嵌入向量
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks);

        // 构建索引项
        var newChunks = new List<DocumentChunk>();
        for (int i = 0; i < chunks.Count; i++)
        {
            newChunks.Add(new DocumentChunk
            {
                Content = chunks[i],
                Embedding = embeddings[i],
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = filePath,
                    ["chunk_index"] = i,
                    ["timestamp"] = DateTime.Now
                }
            });
        }

        if (clearExisting)
            _indexedChunks.Clear();

        _indexedChunks.AddRange(newChunks);
        return chunks.Count;
    }

    /// <summary>
    /// 使用单个分隔符索引文档（兼容旧调用）
    /// </summary>
    public async Task<int> IndexDocumentAsync(string filePath, string chunkSeparator, bool clearExisting = false)
    {
        return await IndexDocumentAsync(filePath, new[] { chunkSeparator }, clearExisting);
    }

    /// <summary>
    /// 清空所有已索引的文档块
    /// </summary>
    public void ClearIndex()
    {
        _indexedChunks.Clear();
    }

    /// <summary>
    /// 检索与查询最相关的 K 个文档块
    /// </summary>
    /// <param name="query">查询文本</param>
    /// <param name="topK">返回最相关的前 K 个块</param>
    /// <param name="minSimilarity">最小相似度阈值（0~1），低于此值的结果将被过滤</param>
    /// <returns>按相似度降序排列的检索结果</returns>
    public async Task<List<(DocumentChunk Chunk, float Similarity)>> RetrieveAsync(string query, int topK = 3, float minSimilarity = 0.0f)
    {
        if (_indexedChunks.Count == 0)
            return new List<(DocumentChunk, float)>();


        try
        {
            // 生成查询向量
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            // 计算相似度并排序
            var scored = _indexedChunks
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Similarity = AnalysesFile.CosineSimilarity(queryEmbedding, chunk.Embedding)
                })
                .Where(x => x.Similarity >= minSimilarity)
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .Select(x => (x.Chunk, x.Similarity))
                .ToList();

            return scored;

        }
        catch (Exception ex)
        {
            // 抛出包含模型名称的明确异常，上层（ChatService）可捕获并弹框
            throw new Exception($"嵌入模型 '{_embeddingService.EmbeddingModelName}' 调用失败: {ex.Message}", ex);
        }

    }

    /// <summary>
    /// 构建 RAG 增强的提示词
    /// </summary>
    /// <param name="query">用户原始问题</param>
    /// <param name="retrievedChunks">检索到的相关文档块</param>
    /// <param name="promptTemplate">自定义提示模板，占位符 {context} 和 {question} 将被替换</param>
    /// <returns>组装好的完整提示词</returns>
    public string BuildAugmentedPrompt(string query, List<(DocumentChunk Chunk, float Similarity)> retrievedChunks, string promptTemplate = null)
    {
        if (string.IsNullOrEmpty(promptTemplate))
        {
            promptTemplate =
                "你是一个知识库助手，请严格根据以下提供的上下文信息回答问题。\n" +
                "如果上下文不足以回答，请明确告知用户，不要编造内容。\n\n" +
                "上下文：\n{context}\n\n" +
                "问题：{question}\n" +
                "回答：";
        }

        var contextBuilder = new StringBuilder();
        for (int i = 0; i < retrievedChunks.Count; i++)
        {
            var chunk = retrievedChunks[i].Chunk;
            contextBuilder.AppendLine($"[片段 {i + 1}] (来源: {chunk.Metadata["source"]})");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
        }

        return promptTemplate
            .Replace("{context}", contextBuilder.ToString().Trim())
            .Replace("{question}", query);
    }

    /// <summary>
    /// 执行 RAG 查询（非流式）
    /// </summary>
    /// <param name="query">用户问题</param>
    /// <param name="topK">检索块数量</param>
    /// <param name="minSimilarity">最小相似度阈值</param>
    /// <param name="promptTemplate">自定义提示模板</param>
    /// <param name="llmParameters">LLM 生成参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含回答和检索详细信息的响应</returns>
    public async Task<RagResponse> QueryAsync(
        string query,
        int topK = 3,
        float minSimilarity = 0.0f,
        string promptTemplate = null,
        LLMParameters llmParameters = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveParams = llmParameters ?? DefaultLLMParameters;
        // 检索
        var retrieved = await RetrieveAsync(query, topK, minSimilarity);

        if (retrieved.Count == 0)
        {
            return new RagResponse
            {
                Answer = "未找到相关上下文信息，无法回答该问题。",
                RetrievedChunks = new List<(DocumentChunk, float)>(),
                PromptUsed = null
            };
        }

        // 构建增强提示词
        string augmentedPrompt = BuildAugmentedPrompt(query, retrieved, promptTemplate);

        // 生成回答
        string answer = await _llmService.GenerateAsync(augmentedPrompt, llmParameters, cancellationToken);

        return new RagResponse
        {
            Answer = answer,
            RetrievedChunks = retrieved,
            PromptUsed = augmentedPrompt
        };
    }

    /// <summary>
    /// 执行 RAG 查询（流式）
    /// </summary>
    /// <param name="query">用户问题</param>
    /// <param name="onChunkReceived">每收到一个 token 时的回调</param>
    /// <param name="topK">检索块数量</param>
    /// <param name="minSimilarity">最小相似度阈值</param>
    /// <param name="promptTemplate">自定义提示模板</param>
    /// <param name="llmParameters">LLM 生成参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含回答和检索详细信息的响应</returns>
    public async Task<RagResponse> QueryStreamingAsync(
        string query,
        Action<string> onChunkReceived,
        int topK = 3,
        float minSimilarity = 0.0f,
        string promptTemplate = null,
        LLMParameters llmParameters = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveParams = llmParameters ?? DefaultLLMParameters;
        // 检索
        var retrieved = await RetrieveAsync(query, topK, minSimilarity);

        if (retrieved.Count == 0)
        {
            onChunkReceived?.Invoke("未找到相关上下文信息，无法回答该问题。");
            return new RagResponse
            {
                Answer = "未找到相关上下文信息，无法回答该问题。",
                RetrievedChunks = new List<(DocumentChunk, float)>(),
                PromptUsed = null
            };
        }

        // 构建增强提示词
        string augmentedPrompt = BuildAugmentedPrompt(query, retrieved, promptTemplate);

        // 流式生成回答
        string answer = await _llmService.GenerateStreamingAsync(augmentedPrompt, onChunkReceived, llmParameters, cancellationToken);

        return new RagResponse
        {
            Answer = answer,
            RetrievedChunks = retrieved,
            PromptUsed = augmentedPrompt
        };
    }

    /// <summary>
    /// 便捷方法：控制台流式 RAG 查询
    /// </summary>
    public async Task<RagResponse> QueryStreamingToConsoleAsync(
        string query,
        int topK = 3,
        float minSimilarity = 0.0f,
        string promptTemplate = null,
        LLMParameters llmParameters = null,
        CancellationToken cancellationToken = default)
    {
        Console.Write("回答: ");
        var result = await QueryStreamingAsync(query, token => Console.Write(token), topK, minSimilarity, promptTemplate, llmParameters, cancellationToken);
        Console.WriteLine();
        return result;
    }

    /// <summary>
    /// 导出当前索引的所有文档块（调试用）
    /// </summary>
    public List<DocumentChunk> ExportIndexedChunks()
    {
        return _indexedChunks.ToList();
    }


    /// <summary>
    /// 移除所有来源于指定文件的文档块
    /// </summary>
    public void RemoveChunksBySource(string filePath)
    {
        _indexedChunks.RemoveAll(c => c.Metadata["source"]?.ToString() == filePath);
    }

    /// <summary>
    /// 批量添加文档块（用于重建索引）
    /// </summary>
    public void AddChunks(IEnumerable<DocumentChunk> chunks)
    {
        _indexedChunks.AddRange(chunks);
    }

    /// <summary>
    /// 保存当前索引到文件
    /// </summary>
    public void SaveIndex()
    {
        try
        {
            var data = new IndexData
            {
                Chunks = _indexedChunks.Select(c => new ChunkData
                {
                    Content = c.Content,
                    Embedding = c.Embedding,
                    Metadata = c.Metadata
                }).ToList()
            };
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(IndexFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存索引失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从文件加载索引
    /// </summary>
    public void LoadIndex()
    {
        if (!File.Exists(IndexFilePath)) return;
        try
        {
            string json = File.ReadAllText(IndexFilePath);
            var data = JsonConvert.DeserializeObject<IndexData>(json);
            _indexedChunks.Clear();
            _indexedChunks.AddRange(data.Chunks.Select(d => new DocumentChunk
            {
                Content = d.Content,
                Embedding = d.Embedding,
                Metadata = d.Metadata
            }));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载索引失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除指定文件的特定索引块
    /// </summary>
    /// <param name="filePath">源文件路径</param>
    /// <param name="chunkIndex">分块索引</param>
    public void RemoveChunkBySourceAndIndex(string filePath, int chunkIndex)
    {
        _indexedChunks.RemoveAll(c =>
            c.Metadata["source"]?.ToString() == filePath &&
            Convert.ToInt32(c.Metadata["chunk_index"]) == chunkIndex);
    }

}