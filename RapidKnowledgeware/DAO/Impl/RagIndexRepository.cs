using IOC.Annotations;
using OllamaFramework.Embedding;
using OllamaFramework.Embedding.Impl;
using OllamaFramework.Rag;
using OllamaFramework.Rag.Impl;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RapidKnowledgeware.DAO.Impl
{
    /// <summary>
    /// RAG索引数据访问对象：封装RagService和AnalysesFile的持久化操作
    /// </summary>
    [Repository]
    public class RagIndexRepository : IRagIndexRepository
    {
        public IRagService GetOrCreateRagService(string ollamaEndpoint, string embeddingModel)
        {
            var ragService = new RagService(ollamaEndpoint: ollamaEndpoint, embeddingModel: embeddingModel);
            return ragService;
        }

        public void SaveIndex(IRagService ragService)
        {
            ragService.SaveIndex();
        }

        public void LoadIndex(IRagService ragService)
        {
            ragService.LoadIndex();
        }

        public void ClearIndex(IRagService ragService)
        {
            ragService.ClearIndex();
        }

        public async Task<string> ReadFileContentAsync(string filePath, string embeddingModel)
        {
            IAnalysesFile analysis = new AnalysesFile(embeddingModel: embeddingModel);
            return await analysis.LoadFileAsync(filePath);
        }

        public List<string> SplitIntoChunks(string content, string[] separators, string embeddingModel)
        {
            IAnalysesFile analysis = new AnalysesFile(embeddingModel: embeddingModel);
            return analysis.SplitIntoChunks(content, separators);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, string embeddingModel)
        {
            IAnalysesFile analysis = new AnalysesFile(embeddingModel: embeddingModel);
            return await analysis.GenerateEmbeddingAsync(text);
        }
    }
}
