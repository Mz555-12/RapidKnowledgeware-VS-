using OllamaFramework.Embedding;
using OllamaFramework.Rag;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Functions.KnowledgeBaseFunc
{
    public class KnowledgeBaseService
    {
        private readonly KnowledgeBaseModel _model;

        private readonly RagService _ragService;

        public KnowledgeBaseService(KnowledgeBaseModel model)
        {
            _model = model;
            _ragService = new RagService(ollamaEndpoint: "http://localhost:11434",embeddingModel: _model.CurrentEmbeddingName
);
            _ragService.LoadIndex();
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
            var ragService = _ragService;
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
                    progress?.Report((fileName, false, 0, ex.Message));
                }
            }
            _ragService.SaveIndex();  // 索引变更后保存
            return successCount;
        }

        /// <summary>
        /// 重新索引所有文件（清空后重建）
        /// </summary>
        public async Task ReindexAllFilesAsync()
        {
            _ragService.ClearIndex();
            var filesSnapshot = _model.FileItems.ToList();
            foreach (var file in filesSnapshot)
            {
                if (!_model.FileItems.Contains(file))
                    continue;
                await ReindexSingleFileAsync(file);
            }
            _ragService.SaveIndex();
        }

        /// <summary>
        /// 重新索引单个文件（更新 RAG 索引）
        /// </summary>
        public async Task ReindexSingleFileAsync(KnowledgeFileItem fileItem)
        {
            // 移除旧索引
            _ragService.RemoveChunksBySource(fileItem.FilePath);

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
            _ragService.AddChunks(newChunks);
            _ragService.SaveIndex();

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
            _ragService.RemoveChunksBySource(filePath);
            _ragService.SaveIndex();
        }

        public void RemoveChunkAndSave(string filePath, int chunkIndex)
        {
            // 需要在 RagService 中添加 RemoveChunkBySourceAndIndex 方法
            _ragService.RemoveChunkBySourceAndIndex(filePath, chunkIndex);
            _ragService.SaveIndex();
        }
    }
}