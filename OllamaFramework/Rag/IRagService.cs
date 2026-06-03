using OllamaFramework.Models;
using OllamaFramework.Rag;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaFramework.Rag
{
    public interface IRagService
    {
        int IndexedChunkCount { get; }
        LLMParameters DefaultLLMParameters { get; set; }

        Task<int> IndexDocumentAsync(string filePath, string[] chunkSeparators, bool clearExisting = false);
        Task<int> IndexDocumentAsync(string filePath, string chunkSeparator, bool clearExisting = false);
        void ClearIndex();
        Task<List<(DocumentChunk Chunk, float Similarity)>> RetrieveAsync(string query, int topK = 3, float minSimilarity = 0.0f, CancellationToken cancellationToken = default);
        string BuildAugmentedPrompt(string query, List<(DocumentChunk Chunk, float Similarity)> retrievedChunks, string promptTemplate = null);
        Task<RagResponse> QueryAsync(string query, int topK = 3, float minSimilarity = 0.0f, string promptTemplate = null, LLMParameters llmParameters = null, CancellationToken cancellationToken = default);
        Task<RagResponse> QueryStreamingAsync(string query, Action<string> onChunkReceived, int topK = 3, float minSimilarity = 0.0f, string promptTemplate = null, LLMParameters llmParameters = null, CancellationToken cancellationToken = default);
        List<DocumentChunk> ExportIndexedChunks();
        void RemoveChunksBySource(string filePath);
        void AddChunks(IEnumerable<DocumentChunk> chunks);
        void SaveIndex();
        void LoadIndex();
        void RemoveChunkBySourceAndIndex(string filePath, int chunkIndex);
    }
}
