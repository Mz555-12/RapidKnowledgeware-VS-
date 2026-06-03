using OllamaFramework.Models;
using OllamaFramework.Rag;
using System.Collections.Generic;

namespace RapidKnowledgeware.Services
{
    public interface IPromptTruncationService
    {
        string BuildTruncatedPrompt(string query, List<(DocumentChunk Chunk, float Similarity)> retrievedChunks, int maxContextLength, string promptTemplate = null);
        string BuildTruncatedContext(List<(DocumentChunk Chunk, float Similarity)> retrievedChunks, int maxContextLength);
    }
}
