using OllamaFramework.Models;
using OllamaFramework.Rag;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services
{
    public interface IKnowledgeBaseService
    {
        IRagService RagServiceInstance { get; }
        Task<int> IndexFilesAsync(IEnumerable<string> filePaths, IProgress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)> progress = null);
        Task ReindexAllFilesAsync();
        Task ReindexSingleFileAsync(KnowledgeFileItem fileItem);
        void RefreshDisplayedChunks(KnowledgeFileItem fileItem);
        Task<List<FileChunkItem>> LoadFileChunksAsync(KnowledgeFileItem fileItem);
        void RemoveFileFromIndex(string filePath);
        void RemoveChunkAndSave(string filePath, int chunkIndex);
        string[] ParseSeparators();
    }
}
