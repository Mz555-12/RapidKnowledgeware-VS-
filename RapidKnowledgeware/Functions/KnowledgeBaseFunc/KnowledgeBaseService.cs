using OllamaFramework.Embedding;
using OllamaFramework.Rag;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Functions.KnowledgeBaseFunc
{
    public class KnowledgeBaseService
    {
        private readonly KnowledgeBaseModel _model;







        public KnowledgeBaseService(KnowledgeBaseModel model)
        {
            _model = model;
            _ = RagServiceInstance;
        }

        /// <summary>
        /// 解析用户输入的分隔符字符串为数组
        /// </summary>
        private static string[] ParseSeparatorsFromRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return new[] { "###" };

            var separators = rule.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim())
                                 .Where(s => !string.IsNullOrEmpty(s))
                                 .ToArray();
            return separators.Length > 0 ? separators : new[] { "###" };
        }

        // 
        public string[] ParseSeparators()
        {
            return ParseSeparatorsFromRule(_model.BlockRule);
        }

        /// <summary>
        /// 批量索引多个文件（核心逻辑）
        /// </summary>
        public async Task<int> IndexFilesAsync(IEnumerable<string> filePaths, IProgress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)> progress = null)
        {
            int successCount = 0;
            var ragService = RagServiceInstance;// 每次获取最新实例
            var separators = ParseSeparators();

            foreach (var filePath in filePaths)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                try
                {
                    int chunkCount = await ragService.IndexDocumentAsync(filePath, separators, clearExisting: false);
                    var item = new KnowledgeFileItem
                    {
                        FilePath = filePath,
                        ChunkCount = chunkCount,
                        IsIndexed = true,
                        ImportBlockRule = _model.BlockRule   // 记录当前全局规则
                    };
                    _model.FileItems.Add(item);
                    successCount++;
                    progress?.Report((fileName, true, chunkCount, null));
                }
                catch (Exception ex)
                {
                    // 检查异常消息中是否包含模型错误关键词
                    string errorMsg = ex.ToString();
                    if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"嵌入模型 \"{_model.CurrentEmbeddingName}\" 不可用，请检查模型名称。\n\n错误详情: {ex.Message}",
                                            "嵌入模型错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        progress?.Report((fileName, false, 0, ex.Message));
                        break; // 模型错误直接终止批量处理
                    }
                    else
                    {
                        progress?.Report((fileName, false, 0, ex.Message));
                    }
                }
            }
            ragService.SaveIndex();  // 索引变更后保存
            return successCount;
        }

        /// <summary>
        /// 重新索引所有文件（清空后重建）
        /// </summary>
        public async Task ReindexAllFilesAsync()
        {
            RagServiceInstance.ClearIndex();
            var filesSnapshot = _model.FileItems.ToList();
            foreach (var file in filesSnapshot)
            {
                if (!_model.FileItems.Contains(file))
                    continue;
                await ReindexSingleFileAsync(file);
            }
            RagServiceInstance.SaveIndex();
        }

        /// <summary>
        /// 重新索引单个文件（更新 RAG 索引）
        /// </summary>
        public async Task ReindexSingleFileAsync(KnowledgeFileItem fileItem)
        {
            // 移除旧索引
            RagServiceInstance.RemoveChunksBySource(fileItem.FilePath);

            var analysis = new AnalysesFile(embeddingModel: _model.CurrentEmbeddingName);
            string content = await analysis.LoadFileAsync(fileItem.FilePath);

            string ruleToUse = !string.IsNullOrWhiteSpace(fileItem.ImportBlockRule)
                               ? fileItem.ImportBlockRule
                               : _model.BlockRule;
            var separators = ParseSeparatorsFromRule(ruleToUse);
            var allChunks = analysis.SplitIntoChunks(content, separators);

            var newChunks = new List<DocumentChunk>();
            for (int i = 0; i < allChunks.Count; i++)
            {
                if (fileItem.DeletedChunkIndices.Contains(i))
                    continue;
                var emb = await analysis.GenerateEmbeddingAsync(allChunks[i]);
                newChunks.Add(new DocumentChunk
                {
                    Content = allChunks[i],
                    Embedding = emb,
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = fileItem.FilePath,
                        ["chunk_index"] = i,
                        ["timestamp"] = DateTime.Now
                    }
                });
            }
            RagServiceInstance.AddChunks(newChunks);
            RagServiceInstance.SaveIndex();

            fileItem.ChunkCount = newChunks.Count;
            fileItem.IsIndexed = true;
        }

        /// <summary>
        /// 刷新当前显示的分块列表（用于删除块后更新 UI 绑定）
        /// </summary>
        public void RefreshDisplayedChunks(KnowledgeFileItem fileItem)
        {
            var analysis = new AnalysesFile();
            string content = Task.Run(() => analysis.LoadFileAsync(fileItem.FilePath)).Result;

            string ruleToUse = !string.IsNullOrWhiteSpace(fileItem.ImportBlockRule)
                               ? fileItem.ImportBlockRule
                               : _model.BlockRule;
            var separators = ParseSeparatorsFromRule(ruleToUse);
            var allChunks = analysis.SplitIntoChunks(content, separators);

            _model.FileBlocks.Clear();
            for (int i = 0; i < allChunks.Count; i++)
            {
                if (fileItem.DeletedChunkIndices.Contains(i))
                    continue;
                _model.FileBlocks.Add(new FileChunkItem
                {
                    Content = allChunks[i],
                    OriginalIndex = i
                });
            }
        }

        /// <summary>
        /// 加载文件的分块内容（用于显示预览）
        /// </summary>
        public async Task<List<FileChunkItem>> LoadFileChunksAsync(KnowledgeFileItem fileItem)
        {
            var analysis = new AnalysesFile();
            string content = await analysis.LoadFileAsync(fileItem.FilePath);

            string ruleToUse = !string.IsNullOrWhiteSpace(fileItem.ImportBlockRule)
                               ? fileItem.ImportBlockRule
                               : _model.BlockRule;
            var separators = ParseSeparatorsFromRule(ruleToUse);
            var allChunks = await Task.Run(() => analysis.SplitIntoChunks(content, separators));

            var result = new List<FileChunkItem>();
            for (int i = 0; i < allChunks.Count; i++)
            {
                if (fileItem.DeletedChunkIndices.Contains(i))
                    continue;
                result.Add(new FileChunkItem
                {
                    Content = allChunks[i],
                    OriginalIndex = i
                });
            }
            return result;
        }

        public void RemoveFileFromIndex(string filePath)
        {
            RagServiceInstance.RemoveChunksBySource(filePath);
            RagServiceInstance.SaveIndex();
        }

        public void RemoveChunkAndSave(string filePath, int chunkIndex)
        {
            // 需要在 RagService 中添加 RemoveChunkBySourceAndIndex 方法
            RagServiceInstance.RemoveChunkBySourceAndIndex(filePath, chunkIndex);
            RagServiceInstance.SaveIndex();
        }


        private static RagService _ragServiceInstance;
        private static string _lastEmbeddingModel;
        private static string _lastBaseURL;
        private static readonly object _ragServiceLock = new object();

        /// <summary>
        /// 获取全局唯一的 RagService 单例实例（线程安全，配置变更时自动重建）
        /// </summary>
        public static RagService RagServiceInstance
        {
            get
            {
                string currentModel = KnowledgeBaseModel.Instance.CurrentEmbeddingName;
                string currentBaseURL = LLMAdjustFunc.LLMAdjustService.Current.Default_BaseURL;

                // 检查是否需要重建实例
                bool needRebuild = _ragServiceInstance == null ||
                                   _lastEmbeddingModel != currentModel ||
                                   _lastBaseURL != currentBaseURL;

                if (needRebuild)
                {
                    lock (_ragServiceLock)
                    {
                        // 双重检查
                        if (_ragServiceInstance == null ||
                            _lastEmbeddingModel != currentModel ||
                            _lastBaseURL != currentBaseURL)
                        {
                            // 保存旧索引？此处自动重建会丢失之前加载的索引数据，但索引已持久化到磁盘，
                            // 重建后调用 LoadIndex() 即可恢复。若担心性能，可考虑保留索引缓存。
                            _ragServiceInstance = new RagService(
                                ollamaEndpoint: currentBaseURL,
                                embeddingModel: currentModel
                            );
                            _ragServiceInstance.LoadIndex();
                            _lastEmbeddingModel = currentModel;
                            _lastBaseURL = currentBaseURL;

                            System.Diagnostics.Debug.WriteLine($"[RagService] 配置变更重建，模型：{currentModel}，BaseURL：{currentBaseURL}");
                        }
                    }
                }
                return _ragServiceInstance;
            }
        }
    }
}