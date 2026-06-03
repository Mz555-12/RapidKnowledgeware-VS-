using IOC;
using IOC.Annotations;
using OllamaFramework.Models;
using OllamaFramework.Rag;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Services.Impl
{
    [Service]
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly IRagIndexRepository _ragIndexRepository;

        private IRagService _ragServiceInstance;
        private string _lastEmbeddingModel;
        private string _lastBaseURL;
        private readonly object _ragServiceLock = new object();

        public KnowledgeBaseService()
        {
            _ragIndexRepository = BeanFactory.GetBean<IRagIndexRepository>();
            _ = RagServiceInstance;
        }

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

        public string[] ParseSeparators()
        {
            return ParseSeparatorsFromRule(KnowledgeBaseModel.Instance.Default_BlockRule);
        }

        public async Task<int> IndexFilesAsync(IEnumerable<string> filePaths, IProgress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)> progress = null)
        {
            int successCount = 0;
            var ragService = RagServiceInstance;
            var separators = ParseSeparators();

            BeanFactory.GetBean<IDebugService>().Info($"[IndexFilesAsync] 开始索引，RagService={(ragService != null ? "已创建" : "NULL")}，分隔符=[{string.Join(", ", separators)}]，嵌入模型={KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName}，BaseURL={BeanFactory.GetBean<ILLMAdjustService>().Current.Default_BaseURL}");

            foreach (var filePath in filePaths)
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                try
                {
                    BeanFactory.GetBean<IDebugService>().Info($"[IndexFilesAsync] 正在索引文件: {fileName}，路径: {filePath}");
                    int chunkCount = await ragService.IndexDocumentAsync(filePath, separators, clearExisting: false);
                    BeanFactory.GetBean<IDebugService>().Info($"[IndexFilesAsync] 文件 {fileName} 索引成功，{chunkCount} 个块");
                    var item = new KnowledgeFileItem
                    {
                        FilePath = filePath,
                        ChunkCount = chunkCount,
                        IsIndexed = true,
                        ImportBlockRule = KnowledgeBaseModel.Instance.Default_BlockRule
                    };
                    KnowledgeBaseModel.Instance.FileItems.Add(item);
                    successCount++;
                    progress?.Report((fileName, true, chunkCount, null));
                }
                catch (Exception ex)
                {
                    BeanFactory.GetBean<IDebugService>().Error($"[IndexFilesAsync] 文件 {fileName} 索引失败", ex);
                    string errorMsg = ex.ToString();
                    if (errorMsg.Contains("model") || errorMsg.Contains("404") || errorMsg.Contains("not found"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"嵌入模型 \"{KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName}\" 不可用，请检查模型名称。\n\n错误详情: {ex.Message}",
                                            "嵌入模型错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        progress?.Report((fileName, false, 0, ex.Message));
                        break;
                    }
                    else
                    {
                        progress?.Report((fileName, false, 0, ex.Message));
                    }
                }
            }
            _ragIndexRepository.SaveIndex(ragService);
            BeanFactory.GetBean<IDebugService>().Info($"[IndexFilesAsync] 批量索引完成，成功 {successCount} 个文件");
            return successCount;
        }

        public async Task ReindexAllFilesAsync()
        {
            _ragIndexRepository.ClearIndex(RagServiceInstance);
            var filesSnapshot = KnowledgeBaseModel.Instance.FileItems.ToList();
            foreach (var file in filesSnapshot)
            {
                if (!KnowledgeBaseModel.Instance.FileItems.Contains(file))
                    continue;
                await ReindexSingleFileAsync(file);
            }
            _ragIndexRepository.SaveIndex(RagServiceInstance);
        }

        public async Task ReindexSingleFileAsync(KnowledgeFileItem fileItem)
        {
            RagServiceInstance.RemoveChunksBySource(fileItem.FilePath);

            string embeddingModel = KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName;
            string content = await _ragIndexRepository.ReadFileContentAsync(fileItem.FilePath, embeddingModel);

            string ruleToUse = !string.IsNullOrWhiteSpace(fileItem.ImportBlockRule)
                               ? fileItem.ImportBlockRule
                               : KnowledgeBaseModel.Instance.Default_BlockRule;
            var separators = ParseSeparatorsFromRule(ruleToUse);
            var allChunks = _ragIndexRepository.SplitIntoChunks(content, separators, embeddingModel);

            var newChunks = new List<DocumentChunk>();
            for (int i = 0; i < allChunks.Count; i++)
            {
                if (fileItem.DeletedChunkIndices.Contains(i))
                    continue;
                var emb = await _ragIndexRepository.GenerateEmbeddingAsync(allChunks[i], embeddingModel);
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
            _ragIndexRepository.SaveIndex(RagServiceInstance);

            fileItem.ChunkCount = newChunks.Count;
            fileItem.IsIndexed = true;
        }

        public void RefreshDisplayedChunks(KnowledgeFileItem fileItem)
        {
            string embeddingModel = KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName;
            string content = Task.Run(() => _ragIndexRepository.ReadFileContentAsync(fileItem.FilePath, embeddingModel)).Result;

            string ruleToUse = !string.IsNullOrWhiteSpace(fileItem.ImportBlockRule)
                               ? fileItem.ImportBlockRule
                               : KnowledgeBaseModel.Instance.Default_BlockRule;
            var separators = ParseSeparatorsFromRule(ruleToUse);
            var allChunks = _ragIndexRepository.SplitIntoChunks(content, separators, embeddingModel);

            KnowledgeBaseModel.Instance.FileBlocks.Clear();
            for (int i = 0; i < allChunks.Count; i++)
            {
                if (fileItem.DeletedChunkIndices.Contains(i))
                    continue;
                KnowledgeBaseModel.Instance.FileBlocks.Add(new FileChunkItem
                {
                    Content = allChunks[i],
                    OriginalIndex = i
                });
            }
        }

        public async Task<List<FileChunkItem>> LoadFileChunksAsync(KnowledgeFileItem fileItem)
        {
            string embeddingModel = KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName;
            string content = await _ragIndexRepository.ReadFileContentAsync(fileItem.FilePath, embeddingModel);

            string ruleToUse = !string.IsNullOrWhiteSpace(fileItem.ImportBlockRule)
                               ? fileItem.ImportBlockRule
                               : KnowledgeBaseModel.Instance.Default_BlockRule;
            var separators = ParseSeparatorsFromRule(ruleToUse);
            var allChunks = await Task.Run(() => _ragIndexRepository.SplitIntoChunks(content, separators, embeddingModel));

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
            _ragIndexRepository.SaveIndex(RagServiceInstance);
        }

        public void RemoveChunkAndSave(string filePath, int chunkIndex)
        {
            RagServiceInstance.RemoveChunkBySourceAndIndex(filePath, chunkIndex);
            _ragIndexRepository.SaveIndex(RagServiceInstance);
        }

        public IRagService RagServiceInstance
        {
            get
            {
                string currentModel = KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName;
                string currentBaseURL = BeanFactory.GetBean<ILLMAdjustService>().Current.Default_BaseURL;

                bool needRebuild = _ragServiceInstance == null ||
                                   _lastEmbeddingModel != currentModel ||
                                   _lastBaseURL != currentBaseURL;

                if (needRebuild)
                {
                    lock (_ragServiceLock)
                    {
                        if (_ragServiceInstance == null ||
                            _lastEmbeddingModel != currentModel ||
                            _lastBaseURL != currentBaseURL)
                        {
                            BeanFactory.GetBean<IDebugService>().Info($"[RagService] 重建实例，模型: {currentModel}，BaseURL: {currentBaseURL}，旧模型: {_lastEmbeddingModel}，旧BaseURL: {_lastBaseURL}");
                            _ragServiceInstance = _ragIndexRepository.GetOrCreateRagService(currentBaseURL, currentModel);
                            _ragIndexRepository.LoadIndex(_ragServiceInstance);
                            _lastEmbeddingModel = currentModel;
                            _lastBaseURL = currentBaseURL;
                        }
                    }
                }
                return _ragServiceInstance;
            }
        }
    }
}
