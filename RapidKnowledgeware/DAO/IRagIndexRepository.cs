using OllamaFramework.Models;
using OllamaFramework.Rag;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RapidKnowledgeware.DAO
{
    public interface IRagIndexRepository
    {
        IRagService GetOrCreateRagService(string ollamaEndpoint, string embeddingModel);
        void SaveIndex(IRagService ragService);
        void LoadIndex(IRagService ragService);
        void ClearIndex(IRagService ragService);
        Task<string> ReadFileContentAsync(string filePath, string embeddingModel);
        List<string> SplitIntoChunks(string content, string[] separators, string embeddingModel);
        Task<float[]> GenerateEmbeddingAsync(string text, string embeddingModel);
    }
}
