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

        public KnowledgeBaseService(KnowledgeBaseModel model)
        {
            _model = model;
        }

        /// <summary>
        /// 解析用户输入的分隔符字符串为数组
        /// </summary>
        public string[] ParseSeparators()
        {
            var separators = _model.BlockRule
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            return separators.Length > 0 ? separators : new[] { "###" };
        }

        /// <summary>
        /// 批量索引多个文件（核心逻辑）
        /// </summary>
        public async Task<int> IndexFilesAsync(IEnumerable<string> filePaths, IProgress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)> progress = null)
        {
            int successCount = 0;
            var ragService = new RagService(
                ollamaEndpoint: "http://localhost:11434",
                embeddingModel: _model.CurrentEmbeddingName
            );
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
                        IsIndexed = true
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
            return successCount;
        }

        /// <summary>
        /// 重新索引所有文件（清空后重建）
        /// </summary>
        public async Task ReindexAllFilesAsync()
        {
            var ragService = new RagService(embeddingModel: _model.CurrentEmbeddingName);
            ragService.ClearIndex();
            var filesSnapshot = _model.FileItems.ToList();
            foreach (var file in filesSnapshot)
            {
                if (!_model.FileItems.Contains(file))
                    continue;
                await ReindexSingleFileAsync(file, ragService);
            }
        }

        /// <summary>
        /// 重新索引单个文件（更新 RAG 索引）
        /// </summary>
        public async Task ReindexSingleFileAsync(KnowledgeFileItem fileItem, RagService existingRagService = null)
        {
            var ragService = existingRagService ?? new RagService(embeddingModel: _model.CurrentEmbeddingName);
            if (existingRagService == null)
            {
                // 如果不是复用的实例，则移除该文件的旧索引
                ragService.RemoveChunksBySource(fileItem.FilePath);
            }

            var analysis = new AnalysesFile(embeddingModel: _model.CurrentEmbeddingName);
            string content = await analysis.LoadFileAsync(fileItem.FilePath);
            var allChunks = analysis.SplitIntoChunks(content, ParseSeparators());

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
            ragService.AddChunks(newChunks);
            fileItem.ChunkCount = newChunks.Count;
            fileItem.IsIndexed = true;
        }

        /// <summary>
        /// 刷新当前显示的分块列表（用于删除块后更新 UI 绑定）
        /// </summary>
        public void RefreshDisplayedChunks(KnowledgeFileItem fileItem)
        {
            var analysis = new AnalysesFile();
            // 注意：这里同步调用 LoadFileAsync，适合删除后立即刷新场景（文件已加载过）
            string content = Task.Run(() => analysis.LoadFileAsync(fileItem.FilePath)).Result;
            var allChunks = analysis.SplitIntoChunks(content, ParseSeparators());

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
            var allChunks = await Task.Run(() => analysis.SplitIntoChunks(content, ParseSeparators()));

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
    }
}